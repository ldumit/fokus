using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;

namespace Fokus.Domain.Analytics.Sprints;

public class EpicProgressCalculator
{
    public EpicProgressResult ComputeEpicProgress(
        List<Ticket> epicTickets,
        List<SprintMembership> allClosedMemberships,
        List<Ticket> unlinkedTickets,
        AppSettings settings,
        List<StatusTransition> statusTransitions,
        List<Sprint> closedSprints,
        string? subTeam,
        EpicQaData? qaData = null)
    {
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);

        var context = new EpicContext(
            CompletionChecker.ResolveCompletedStatuses(settings),
            settings.DefaultSpPerBug,
            orderedStages,
            startIndex,
            endIndex,
            // Build sprint date lookup for velocity transition checks
            closedSprints.ToDictionary(s => s.Id, s => (s.StartDate, s.EndDate)),
            // BR13: sub-team filtering
            FilterMemberships(allClosedMemberships, subTeam),
            // Activity dates match ticket ids case-insensitively; sprint completion checks match them ordinally
            statusTransitions
                .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase),
            StatusTransitionIndex.Build(statusTransitions),
            settings,
            qaData);

        // Group filtered tickets by EpicKey
        var epicEntries = FilterTickets(epicTickets, subTeam)
            .GroupBy(t => t.EpicKey!)
            .Select(group => BuildEpicEntry(group.Key, group.ToList(), context))
            .ToList();

        // BR12: sort — active epics by SP completion % asc (null last), then completed epics by name asc
        var activeEpics = epicEntries
            .Where(e => !e.IsCompleted)
            .OrderBy(e => e.SpCompletionPercentage.HasValue ? 0 : 1)
            .ThenBy(e => e.SpCompletionPercentage ?? e.TicketCompletionPercentage)
            .ToList();

        var completedEpics = epicEntries
            .Where(e => e.IsCompleted)
            .OrderBy(e => e.EpicName)
            .ToList();

        var sortedEpics = activeEpics.Concat(completedEpics).ToList();

        var summaryMetrics = new EpicProgressSummaryMetrics(
            activeEpics.Count,
            completedEpics.Count,
            ComputeAverageCompletion(activeEpics));

        // Unlinked work (BR14)
        var filteredUnlinkedTickets = FilterTickets(unlinkedTickets, subTeam);
        var unlinkedTotalSp = filteredUnlinkedTickets
            .Sum(t => t.GetEffectiveSp(settings.DefaultSpPerBug) ?? 0m);

        var unlinkedWork = new EpicProgressUnlinkedWork(
            filteredUnlinkedTickets.Count,
            Math.Round(unlinkedTotalSp, 1));

        var hasQaData = qaData is not null;
        var averageTestCoverage = hasQaData ? ComputeAverageTestCoverage(sortedEpics) : null;

        return new EpicProgressResult(summaryMetrics, sortedEpics, unlinkedWork, hasQaData, averageTestCoverage);
    }

    private static EpicProgressEntry BuildEpicEntry(string epicKey, List<Ticket> tickets, EpicContext context)
    {
        var completedStatuses = context.CompletedStatuses;

        // Epic name: first ticket's EpicName ?? EpicKey (BR22)
        var epicName = tickets.First().EpicName ?? epicKey;

        // Done vs remaining (BR2)
        var doneTickets = tickets.Where(t => completedStatuses.Contains(t.CurrentStatus)).ToList();
        var remainingTickets = tickets.Where(t => !completedStatuses.Contains(t.CurrentStatus)).ToList();

        var totalTickets = tickets.Count;
        var doneCount = doneTickets.Count;
        var ticketCompletionPct = totalTickets > 0
            ? Math.Round((decimal)doneCount / totalTickets * 100, 1)
            : 0m;

        var sp = ComputeStoryPoints(tickets, doneTickets, remainingTickets, context.DefaultSpPerBug);

        // Velocity (BR7, BR8, BR16, BR24): from closed sprint memberships for this epic
        var epicMemberships = context.FilteredMemberships
            .Where(sm => sm.Ticket?.EpicKey == epicKey)
            .ToList();

        var (startedDate, lastWorkDate) = ResolveActivityDates(tickets, context);
        var projection = ComputeProjection(epicMemberships, sp.RemainingSp + (sp.ImputedSp ?? 0m), context);

        // Active sprint count (BR17): distinct sprints where this epic had at least one ticket membership
        var activeSprintCount = epicMemberships
            .Select(sm => sm.SprintId)
            .Distinct()
            .Count();

        // Is completed (BR11): all tickets have done status
        var isCompleted = tickets.All(t => completedStatuses.Contains(t.CurrentStatus));

        // QA metrics per epic (BR1–BR12, BR20)
        var qa = ComputeEpicQaMetrics(tickets, context.QaData, context.Settings);

        // Ticket list — sort: remaining first (by current status for grouping), then done (BR spec Flow 2 step 3)
        var ticketEntries = remainingTickets
            .OrderBy(t => t.CurrentStatus)
            .Concat(doneTickets.OrderBy(t => t.CurrentStatus))
            .Select(t => BuildTicketEntry(t, completedStatuses, context.QaData))
            .ToList();

        return new EpicProgressEntry(
            epicKey, epicName,
            totalTickets, doneCount, remainingTickets.Count, ticketCompletionPct,
            Math.Round(sp.TotalSp, 1), Math.Round(sp.DoneSp, 1), Math.Round(sp.RemainingSp, 1),
            sp.ImputedSp, sp.AdjustedTotalSp, sp.SpCompletionPct, sp.UnestimatedCount,
            projection.Velocity, projection.ProjectedSprintsRemaining, projection.Confidence,
            activeSprintCount, startedDate, lastWorkDate, isCompleted, ticketEntries,
            qa.CoverageRate, qa.PassRate, qa.BugsFound, qa.FeatureTicketCount, qa.CoveredTicketCount,
            qa.CoverageRag, qa.PassRateRag);
    }

    private static EpicStoryPoints ComputeStoryPoints(
        List<Ticket> tickets,
        List<Ticket> doneTickets,
        List<Ticket> remainingTickets,
        int defaultSpPerBug)
    {
        // SP sums from estimated tickets (BR15)
        var totalSp = tickets
            .Sum(t => t.GetEffectiveSp(defaultSpPerBug) ?? 0m);

        var doneSp = doneTickets
            .Sum(t => t.GetEffectiveSp(defaultSpPerBug) ?? 0m);

        var remainingSp = remainingTickets
            .Sum(t => t.GetEffectiveSp(defaultSpPerBug) ?? 0m);

        // Unestimated ticket count (BR15)
        var unestimatedCount = tickets.Count(t => t.GetEffectiveSp(defaultSpPerBug) == null);

        // Imputation (BR4, BR5, BR6): apply only to remaining unestimated tickets
        var estimatedTickets = tickets.Where(t => t.GetEffectiveSp(defaultSpPerBug).HasValue).ToList();
        decimal? imputedSp = null;
        decimal? adjustedTotalSp = null;
        decimal? spCompletionPct = null;

        if (estimatedTickets.Count > 0)
        {
            var avgSp = estimatedTickets.Average(t => t.GetEffectiveSp(defaultSpPerBug)!.Value);
            var remainingUnestimatedCount = remainingTickets.Count(t => t.GetEffectiveSp(defaultSpPerBug) == null);
            imputedSp = remainingUnestimatedCount > 0
                ? Math.Round(avgSp * remainingUnestimatedCount, 1)
                : 0m;
            adjustedTotalSp = Math.Round(totalSp + imputedSp.Value, 1);

            // SP completion % (BR3, BR19)
            spCompletionPct = adjustedTotalSp.Value > 0
                ? Math.Round(doneSp / adjustedTotalSp.Value * 100, 1)
                : 0m;
        }

        return new EpicStoryPoints(
            totalSp, doneSp, remainingSp, unestimatedCount, imputedSp, adjustedTotalSp, spCompletionPct);
    }

    private static (DateOnly? StartedDate, DateOnly? LastWorkDate) ResolveActivityDates(
        List<Ticket> tickets,
        EpicContext context)
    {
        // Activity dates (F35 BR1, BR2): earliest and most recent qualifying transition across epic tickets
        // A transition qualifies if its ToStatus stage index >= startIndex (same boundary as velocity)
        // Guard: if no cycle time start stage is configured (startIndex < 0), dates cannot be derived —
        // GetStageIndex returns -1 for unrecognised statuses, so -1 >= -1 would match noise transitions
        DateOnly? startedDate = null;
        DateOnly? lastWorkDate = null;

        if (context.StartIndex < 0)
            return (startedDate, lastWorkDate);

        foreach (var ticket in tickets)
        {
            var ticketTransitions = context.TransitionsByTicket.GetValueOrDefault(ticket.Id, []);
            foreach (var transition in ticketTransitions)
            {
                if (TransitionAttributionChecker.GetStageIndex(transition.ToStatus, context.OrderedStages) >= context.StartIndex)
                {
                    var transitionDate = DateOnly.FromDateTime(transition.Timestamp);
                    if (startedDate is null || transitionDate < startedDate)
                        startedDate = transitionDate;
                    if (lastWorkDate is null || transitionDate > lastWorkDate)
                        lastWorkDate = transitionDate;
                }
            }
        }

        return (startedDate, lastWorkDate);
    }

    private static EpicProjection ComputeProjection(
        List<SprintMembership> epicMemberships,
        decimal remainingWork,
        EpicContext context)
    {
        // Group by SprintId, sum completed SP per sprint (transition-based — spec BR16 velocity)
        var sprintSpCompleted = epicMemberships
            .GroupBy(sm => sm.SprintId)
            .Select(g => ComputeSprintVelocity(g.Key, g.ToList(), context))
            .Where(x => x.CompletedSp > 0) // BR8: skip zero-progress sprints
            .OrderByDescending(x => x.SprintStartDate)
            .Take(3) // rolling 3-sprint window
            .ToList();

        decimal? velocity = null;
        if (sprintSpCompleted.Count > 0)
        {
            velocity = Math.Round(sprintSpCompleted.Average(x => x.CompletedSp), 1);
        }

        // Projection (BR9, BR10)
        decimal? projectedSprintsRemaining = null;
        string? projectionConfidence = null;

        if (velocity.HasValue && velocity.Value > 0)
        {
            projectedSprintsRemaining = Math.Round(remainingWork / velocity.Value, 1);

            // Confidence (BR10): low when fewer than 3 data points
            if (sprintSpCompleted.Count < 3)
            {
                projectionConfidence = "low";
            }
        }

        return new EpicProjection(velocity, projectedSprintsRemaining, projectionConfidence);
    }

    private static SprintVelocity ComputeSprintVelocity(
        int sprintId,
        List<SprintMembership> sprintMemberships,
        EpicContext context)
    {
        var sprintStartDate = sprintMemberships.First().Sprint?.StartDate ?? DateTime.MinValue;
        var (sprintStart, sprintEnd) = context.SprintDateLookup.TryGetValue(sprintId, out var dates)
            ? dates
            : (sprintStartDate, sprintStartDate);

        var completedSp = sprintMemberships
            .Where(sm =>
            {
                if (sm.RemovedAt != null) return false;
                var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                    sm.TicketId, context.TransitionIndex, sprintStart, sprintEnd, context.OrderedStages, context.EndIndex);
                return isCompleted;
            })
            .Sum(sm => sm.GetEffectiveSp(context.DefaultSpPerBug) ?? 0m);

        return new SprintVelocity(sprintStartDate, completedSp);
    }

    private static decimal ComputeAverageCompletion(List<EpicProgressEntry> activeEpics)
    {
        // Average completion: weighted by adjustedTotalSp (BR spec AC)
        var totalAdjustedSp = activeEpics
            .Where(e => e.AdjustedTotalSp.HasValue)
            .Sum(e => e.AdjustedTotalSp!.Value);

        if (totalAdjustedSp > 0)
        {
            var weightedSum = activeEpics
                .Where(e => e.AdjustedTotalSp.HasValue && e.SpCompletionPercentage.HasValue)
                .Sum(e => e.SpCompletionPercentage!.Value * e.AdjustedTotalSp!.Value);
            return Math.Round(weightedSum / totalAdjustedSp, 1);
        }

        if (activeEpics.Count > 0)
        {
            // All active epics have null adjustedTotalSp — fall back to ticket-count average
            return Math.Round(
                activeEpics.Average(e => e.TicketCompletionPercentage), 1);
        }

        return 0m;
    }

    private static decimal? ComputeAverageTestCoverage(List<EpicProgressEntry> sortedEpics)
    {
        // BR16: average test coverage = arithmetic mean of non-null coverage rates (epics with null excluded)
        var coverageRates = sortedEpics
            .Where(e => e.CoverageRate.HasValue)
            .Select(e => e.CoverageRate!.Value)
            .ToList();
        return coverageRates.Count > 0 ? Math.Round(coverageRates.Average(), 1) : null;
    }

    // --- Sub-team filtering (BR13) ---

    private static List<Ticket> FilterTickets(List<Ticket> tickets, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return tickets;

        return tickets
            .Where(t => t.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    // --- QA computation helpers ---

    // BR2, BR12: own Tests links first, fallback to the parent's
    private static List<string> GetEffectiveTeIds(Ticket ticket, EpicQaData qaData)
    {
        var ownTeIds = qaData.TestsLinksByTicket.GetValueOrDefault(ticket.Id, []);
        return ownTeIds.Count > 0
            ? ownTeIds
            : (!string.IsNullOrEmpty(ticket.ParentTicketKey)
                ? qaData.TestsLinksByTicket.GetValueOrDefault(ticket.ParentTicketKey, [])
                : []);
    }

    /// <summary>
    /// Computes per-epic QA metrics (BR1–BR4, BR12, BR13, BR20).
    /// Returns (coverageRate, passRate, bugsFound, featureTicketCount, coveredTicketCount, coverageRag, passRateRag).
    /// </summary>
    private static (decimal? CoverageRate, decimal? PassRate, int BugsFound, int FeatureTicketCount,
        int CoveredTicketCount, string? CoverageRag, string? PassRateRag)
        ComputeEpicQaMetrics(List<Ticket> epicTickets, EpicQaData? qaData, AppSettings settings)
    {
        if (qaData is null)
            return (null, null, 0, 0, 0, null, null);

        // BR1: feature ticket scope = non-bug tickets
        var featureTickets = epicTickets.Where(t => t.IssueType != "Bug").ToList();

        // BR13: zero feature tickets → null coverage and pass rate
        if (featureTickets.Count == 0)
            return (null, null, 0, 0, 0, null, null);

        var coveredCount = 0;
        var totalPassRuns = 0;
        var totalExecutedRuns = 0; // PASS + FAIL only (BR3)
        var bugKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticket in featureTickets)
        {
            var effectiveTeIds = GetEffectiveTeIds(ticket, qaData);

            if (effectiveTeIds.Count > 0)
                coveredCount++;

            // BR3: accumulate pass/fail runs across effective TEs
            foreach (var teId in effectiveTeIds)
            {
                var runs = qaData.RunsByTeId.GetValueOrDefault(teId, []);
                totalPassRuns += runs.Count(r => r.Status == TestRunStatus.Pass);
                totalExecutedRuns += runs.Count(r => r.Status == TestRunStatus.Pass || r.Status == TestRunStatus.Fail);
            }

        }

        // BR4: collect unique bug keys via Blocks links on all feature tickets' TE IDs
        // BlocksLinksByTicket: key = bug ticket key, value = list of TE IDs that block it
        // We need to invert: for each feature ticket's TE (via Tests links), find what bugs they block
        // The data structure stores: bug ticket key → list of TE IDs that have a Blocks link to that bug
        // So we need to find all bug keys where at least one of their blocking TE IDs is in our feature epic's TEs
        var epicTeIds = featureTickets
            .SelectMany(t => GetEffectiveTeIds(t, qaData))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (bugKey, teIds) in qaData.BlocksLinksByTicket)
        {
            if (teIds.Any(id => epicTeIds.Contains(id)))
                bugKeys.Add(bugKey);
        }

        var featureCount = featureTickets.Count;
        var coverageRate = Math.Round((decimal)coveredCount / featureCount * 100, 1);
        var passRate = totalExecutedRuns > 0
            ? Math.Round((decimal)totalPassRuns / totalExecutedRuns * 100, 1)
            : 0m;

        var thresholds = settings.QaHealthThresholds;
        var coverageRag = HealthScoreCalculator.MetricRag(coverageRate, thresholds.CoverageGreen, thresholds.CoverageAmber, higherIsBetter: true);
        var passRateRag = HealthScoreCalculator.MetricRag(passRate, thresholds.PassRateGreen, thresholds.PassRateAmber, higherIsBetter: true);

        return (coverageRate, passRate, bugKeys.Count, featureCount, coveredCount, coverageRag, passRateRag);
    }

    /// <summary>
    /// Builds a ticket entry with per-ticket QA fields (BR5–BR7).
    /// Bug tickets get null for all QA fields.
    /// </summary>
    private static EpicProgressTicketEntry BuildTicketEntry(
        Ticket ticket,
        HashSet<string> completedStatuses,
        EpicQaData? qaData)
    {
        var isDone = completedStatuses.Contains(ticket.CurrentStatus);

        if (qaData is null || ticket.IssueType == "Bug")
        {
            return new EpicProgressTicketEntry(
                ticket.Id, ticket.Summary, ticket.IssueType, ticket.StoryPoints,
                ticket.CurrentStatus, ticket.Assignee?.DisplayName, isDone,
                TestStatus: null,
                TestPassRate: null,
                TestBugsFound: null,
                TestRunSummary: null);
        }

        var effectiveTeIds = GetEffectiveTeIds(ticket, qaData);

        if (effectiveTeIds.Count == 0)
        {
            return new EpicProgressTicketEntry(
                ticket.Id, ticket.Summary, ticket.IssueType, ticket.StoryPoints,
                ticket.CurrentStatus, ticket.Assignee?.DisplayName, isDone,
                TestStatus: "NoTests",
                TestPassRate: null,
                TestBugsFound: 0,
                TestRunSummary: null);
        }

        var runSummary = SummarizeRuns(effectiveTeIds, qaData);

        // BR6: per-ticket pass rate
        var executedRuns = runSummary.Passed + runSummary.Failed;
        decimal? testPassRate = executedRuns > 0
            ? Math.Round((decimal)runSummary.Passed / executedRuns * 100, 1)
            : null;

        // BR7: per-ticket bugs found via Blocks links
        var teIdSet = effectiveTeIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ticketBugKeys = qaData.BlocksLinksByTicket
            .Where(kvp => kvp.Value.Any(id => teIdSet.Contains(id)))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new EpicProgressTicketEntry(
            ticket.Id, ticket.Summary, ticket.IssueType, ticket.StoryPoints,
            ticket.CurrentStatus, ticket.Assignee?.DisplayName, isDone,
            TestStatus: DeriveTestStatus(runSummary),
            TestPassRate: testPassRate,
            TestBugsFound: ticketBugKeys.Count,
            TestRunSummary: runSummary);
    }

    // Aggregate runs across all effective TEs
    private static TestRunSummary SummarizeRuns(List<string> effectiveTeIds, EpicQaData qaData)
    {
        var passCount = 0;
        var failCount = 0;
        var todoCount = 0;
        var executingCount = 0;
        var abortedCount = 0;

        foreach (var teId in effectiveTeIds)
        {
            var runs = qaData.RunsByTeId.GetValueOrDefault(teId, []);
            passCount += runs.Count(r => r.Status == TestRunStatus.Pass);
            failCount += runs.Count(r => r.Status == TestRunStatus.Fail);
            todoCount += runs.Count(r => r.Status == TestRunStatus.Todo);
            executingCount += runs.Count(r => r.Status == TestRunStatus.Executing);
            abortedCount += runs.Count(r => r.Status == TestRunStatus.Aborted);
        }

        return new TestRunSummary(passCount, failCount, todoCount, executingCount, abortedCount);
    }

    // BR5/BR8: derive test status
    private static string DeriveTestStatus(TestRunSummary runs)
    {
        if (runs.Failed > 0)
            return "Failed";
        if (runs.Passed > 0 && runs.Todo == 0 && runs.Executing == 0)
            return "Passed";
        if (runs.Todo > 0 || runs.Executing > 0)
            return "InProgress";
        return "NoTests"; // TEs exist but all runs are aborted — no effective test results
    }

    private sealed record EpicContext(
        HashSet<string> CompletedStatuses,
        int DefaultSpPerBug,
        List<string> OrderedStages,
        int StartIndex,
        int EndIndex,
        Dictionary<int, (DateTime StartDate, DateTime EndDate)> SprintDateLookup,
        List<SprintMembership> FilteredMemberships,
        Dictionary<string, List<StatusTransition>> TransitionsByTicket,
        StatusTransitionIndex TransitionIndex,
        AppSettings Settings,
        EpicQaData? QaData);

    private sealed record EpicStoryPoints(
        decimal TotalSp,
        decimal DoneSp,
        decimal RemainingSp,
        int UnestimatedCount,
        decimal? ImputedSp,
        decimal? AdjustedTotalSp,
        decimal? SpCompletionPct);

    private sealed record EpicProjection(
        decimal? Velocity,
        decimal? ProjectedSprintsRemaining,
        string? Confidence);

    private sealed record SprintVelocity(DateTime SprintStartDate, decimal CompletedSp);
}
