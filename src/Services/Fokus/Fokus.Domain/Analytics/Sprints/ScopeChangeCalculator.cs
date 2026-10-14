using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.SubTeamFilter;
using static Fokus.Domain.Analytics.Sprints.ScopeMetricCardFactory;

namespace Fokus.Domain.Analytics.Sprints;

public class ScopeChangeCalculator
{
    public ScopeChangeMultiSprintResult ComputeMultiSprint(
        List<Sprint> sprints,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var sortedSprints = sprints.OrderBy(s => s.StartDate).ToList();
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // Resolve transition boundaries once
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        var sprintInfos = sortedSprints
            .Select(s => new SprintRef(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var perSprintData = sortedSprints
            .Select(s => ComputePerSprintData(
                s,
                FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds),
                statusTransitions, transitionIndex,
                orderedStages, startIndex, endIndex,
                excludedStatuses, defaultSpPerBug, settings.PlanningWindowDays))
            .ToList();

        var allClassificationEntries = sortedSprints
            .SelectMany(s => GetMidSprintAdditions(
                FilterMemberships(s.Memberships, subTeam, excludedDeveloperIds), s, settings.PlanningWindowDays,
                transitionIndex, orderedStages, startIndex, excludedStatuses))
            .ToList();

        var classificationBreakdown = BuildClassificationBreakdown(allClassificationEntries, defaultSpPerBug);

        var avgDisruptionRate = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.DisruptionRate), 1)
            : 0;

        var avgNetScopeChange = perSprintData.Count > 0
            ? Math.Round(perSprintData.Average(d => d.NetScopeChange), 1)
            : 0;

        var totalBugsAdded = perSprintData.Sum(d => d.BugCount);

        var summaryMetrics = new ScopeChangeSummaryMetrics(
            avgDisruptionRate,
            null,
            null,
            avgNetScopeChange,
            totalBugsAdded);

        return new ScopeChangeMultiSprintResult(sprintInfos, summaryMetrics, perSprintData, classificationBreakdown);
    }

    public ScopeChangeSingleSprintResult ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam,
        HashSet<string>? excludedDeveloperIds = null)
    {
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var workflowStages = settings.WorkflowStages;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // Resolve transition boundaries once
        var (orderedStages, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        var memberships = FilterMemberships(targetSprint.Memberships, subTeam, excludedDeveloperIds);
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam, excludedDeveloperIds)
            : null;

        var sprintInfo = new SprintRef(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        // Core metrics for target sprint
        var current = ComputeSprintMetrics(
            memberships, statusTransitions, transitionIndex, targetSprint,
            orderedStages, startIndex, endIndex,
            excludedStatuses, defaultSpPerBug, settings.PlanningWindowDays);
        ScopeSprintMetrics? prior = priorMemberships is not null && priorSprint is not null
            ? ComputeSprintMetrics(
                priorMemberships, statusTransitions, transitionIndex, priorSprint,
                orderedStages, startIndex, endIndex,
                excludedStatuses, defaultSpPerBug, settings.PlanningWindowDays)
            : null;

        // Build metric cards with deltas
        var metrics = new ScopeChangeSingleSprintMetrics(
            CommittedSpActive: BuildMetricCard("Committed SP (Active)", current.CommittedSpActive,
                $"{current.CommittedSpActive:0.#}", prior is not null ? current.CommittedSpActive - prior.CommittedSpActive : null, "neutral"),
            CommittedSpTotal: BuildMetricCard("Committed SP (Total)", current.CommittedSpTotal,
                $"Active: {current.CommittedSpActive:0.#} | Total: {current.CommittedSpTotal:0.#}", null, "neutral"),
            AddedSp: BuildMetricCard("Added SP", current.AddedSp,
                $"{current.AddedSp:0.#}", prior is not null ? current.AddedSp - prior.AddedSp : null, "positive-down"),
            RemovedSp: BuildMetricCard("Removed SP", current.RemovedSp,
                $"{current.RemovedSp:0.#}", prior is not null ? current.RemovedSp - prior.RemovedSp : null, "neutral"),
            NetScopeChange: BuildMetricCard("Net Scope Change", current.NetScopeChange,
                $"{current.NetScopeChange:+0.#;-0.#;0}", prior is not null ? current.NetScopeChange - prior.NetScopeChange : null, "neutral"),
            DisruptionRate: BuildMetricCard("Disruption Rate", current.DisruptionRate,
                $"{current.DisruptionRate:0.#}%", prior is not null ? current.DisruptionRate - prior.DisruptionRate : null, "positive-down"),
            BugCount: BuildMetricCard("Bug Count", current.BugCount,
                $"{current.BugCount}", prior is not null ? (decimal)(current.BugCount - prior.BugCount) : null, "positive-down"));

        // Classification breakdown for this sprint
        var midSprintAdditions = GetMidSprintAdditions(memberships, targetSprint, settings.PlanningWindowDays,
            transitionIndex, orderedStages, startIndex, excludedStatuses);
        var classificationBreakdown = BuildClassificationBreakdown(midSprintAdditions, defaultSpPerBug);

        // Event table
        var events = BuildEventTable(memberships, targetSprint, excludedStatuses, settings.PlanningWindowDays, defaultSpPerBug,
            transitionIndex, orderedStages, startIndex);

        // Burnup chart (transition-based cumulative scope and completed lines — spec BR11)
        var burnupData = BuildBurnupData(
            memberships, targetSprint, transitionIndex,
            orderedStages, startIndex, endIndex,
            excludedStatuses, settings.PlanningWindowDays, defaultSpPerBug);

        // Bug time-in-progress
        var bugTimeInProgress = ComputeBugTimeInProgress(memberships, transitionIndex, targetSprint, workflowStages);

        return new ScopeChangeSingleSprintResult(
            sprintInfo, metrics, burnupData, classificationBreakdown, events, bugTimeInProgress);
    }

    // --- Per-sprint data computation ---

    private static ScopeChangePerSprintData ComputePerSprintData(
        Sprint sprint,
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        int planningWindowDays)
    {
        var metrics = ComputeSprintMetrics(
            memberships, statusTransitions, transitionIndex, sprint,
            orderedStages, startIndex, endIndex,
            excludedStatuses, defaultSpPerBug, planningWindowDays);
        return new ScopeChangePerSprintData(
            sprint.Id,
            Math.Round(metrics.CommittedSpActive, 1),
            Math.Round(metrics.CommittedSpTotal, 1),
            Math.Round(metrics.AddedSp, 1),
            Math.Round(metrics.RemovedSp, 1),
            Math.Round(metrics.NetScopeChange, 1),
            Math.Round(metrics.CompletedSp, 1),
            Math.Round(metrics.DisruptionRate, 1),
            metrics.BugCount,
            Math.Round(metrics.BugSpCompleted, 1));
    }

    private record ScopeSprintMetrics(
        decimal CommittedSpActive,
        decimal CommittedSpTotal,
        decimal AddedSp,
        decimal RemovedSp,
        decimal CompletedSp,
        decimal NetScopeChange,
        decimal DisruptionRate,
        int BugCount,
        decimal BugSpCompleted);

    private static ScopeSprintMetrics ComputeSprintMetrics(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug,
        int planningWindowDays)
    {
        var sprintStart = sprint.StartDate;
        var sprintEnd = sprint.EndDate;
        var planningCutoff = sprintStart.AddDays(planningWindowDays);

        var activeSp = 0m;      // feature tickets that transitioned to startStage (active/committed)
        var addedSp = 0m;       // feature tickets added post-planning that entered the cycle (spec BR3)
        var completedSp = 0m;   // feature tickets that transitioned to endStage
        var bugSpCompleted = 0m; // bug tickets that transitioned to endStage
        var bugCount = 0;        // bug tickets with any transition to endStage during sprint

        foreach (var m in memberships)
        {
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;
            if (m.RemovedAt != null) continue;
            if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, startIndex);
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, endIndex);
            var isAdded = TransitionAttributionChecker.IsAddedInSprint(m, isStarted, planningCutoff);

            if (m.Ticket?.IssueType == "Bug")
            {
                if (isCompleted)
                {
                    bugSpCompleted += sp.Value;
                    bugCount++;
                }
            }
            else
            {
                if (isStarted) activeSp += sp.Value;
                if (isAdded) addedSp += sp.Value;
                if (isCompleted) completedSp += sp.Value;
            }
        }

        // feature tickets removed post-planning that had entered the cycle (spec BR4)
        var removedSp = ComputeRemovedSp(
            memberships, statusTransitions, sprintStart, planningCutoff,
            orderedStages, startIndex, excludedStatuses, defaultSpPerBug);
        var committedSpTotal = ComputeCommittedSpTotal(memberships, planningCutoff, defaultSpPerBug);

        var netScopeChange = addedSp - removedSp;
        var disruptionRate = activeSp > 0 ? addedSp / activeSp * 100 : 0;

        return new ScopeSprintMetrics(
            activeSp, committedSpTotal, addedSp, removedSp, completedSp,
            netScopeChange, disruptionRate, bugCount, bugSpCompleted);
    }

    // Removed tickets: only count in removedSp if post-planning AND cycle-entered AND non-excluded (spec BR4)
    private static decimal ComputeRemovedSp(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        DateTime sprintStart,
        DateTime planningCutoff,
        List<string> orderedStages,
        int startIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var transitionsByTicket = statusTransitions
            .GroupBy(t => t.TicketId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var removedSp = 0m;
        foreach (var m in memberships)
        {
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue) continue;
            if (m.RemovedAt == null) continue;
            if (m.Ticket?.IssueType == "Bug") continue;
            if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;
            var ticketTransitions = transitionsByTicket.GetValueOrDefault(m.TicketId, []);
            if (TransitionAttributionChecker.IsRemovedPostPlanning(
                    m, ticketTransitions, sprintStart, planningCutoff, orderedStages, startIndex))
                removedSp += sp.Value;
        }

        return removedSp;
    }

    // committedSpTotal = membership at planning cutoff, feature-only, no excluded-status filter (spec BR1)
    private static decimal ComputeCommittedSpTotal(
        List<SprintMembership> memberships,
        DateTime planningCutoff,
        int defaultSpPerBug) =>
        memberships
            .Where(m => m.Ticket?.IssueType != "Bug"
                        && m.GetEffectiveSp(defaultSpPerBug).HasValue
                        && (m.WasCommitted || m.AddedAt <= planningCutoff)
                        && (m.RemovedAt == null || m.RemovedAt > planningCutoff))
            .Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value);

    // --- Excluded status check (case-insensitive) ---

    private static bool IsExcluded(string status, List<string> excludedStatuses) =>
        excludedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    // --- Classification ---

    private record MidSprintAddition(SprintMembership Membership, string Category);

    private static List<MidSprintAddition> GetMidSprintAdditions(
        List<SprintMembership> memberships,
        Sprint sprint,
        int planningWindowDays,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int startIndex,
        List<string> excludedStatuses)
    {
        var planningCutoff = sprint.StartDate.AddDays(planningWindowDays);
        var sprintStart = sprint.StartDate;
        var sprintEnd = sprint.EndDate;

        var result = new List<MidSprintAddition>();
        foreach (var m in memberships)
        {
            // Classification only applies to post-planning, cycle-entered, non-bug, non-excluded additions (spec BR3)
            if (m.AddedAt <= planningCutoff) continue;
            if (m.RemovedAt != null) continue;
            if (m.Ticket?.IssueType == "Bug") continue;
            if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;

            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, startIndex);
            if (!isStarted) continue;

            result.Add(new MidSprintAddition(m, ClassifyAddition(m, sprint)));
        }
        return result;
    }

    private static string ClassifyAddition(SprintMembership m, Sprint sprint)
    {
        // All tickets reaching this point are already post-planning — Planning Overflow removed (spec BR8)
        if (m.Ticket?.IssueType == "Bug")
            return "Unplanned Bug";

        if (m.Ticket?.CreatedDate < sprint.StartDate)
            return "Priority Escalation";

        return "Scope Injection";
    }

    private static List<ClassificationEntry> BuildClassificationBreakdown(
        List<MidSprintAddition> additions,
        int defaultSpPerBug)
    {
        var total = additions.Count;

        var categories = new[] { "Unplanned Bug", "Priority Escalation", "Scope Injection" };

        return categories.Select(category =>
        {
            var items = additions.Where(a => a.Category == category).ToList();
            var count = items.Count;
            var spItems = items.Where(a => a.Membership.GetEffectiveSp(defaultSpPerBug).HasValue).ToList();
            decimal? spTotal = spItems.Count > 0 ? spItems.Sum(a => a.Membership.GetEffectiveSp(defaultSpPerBug)!.Value) : null;
            var percentage = total > 0 ? Math.Round((decimal)count / total * 100, 1) : 0;
            return new ClassificationEntry(category, count, spTotal.HasValue ? Math.Round(spTotal.Value, 1) : null, percentage);
        }).ToList();
    }

    // --- Event table ---

    private static List<ScopeChangeEvent> BuildEventTable(
        List<SprintMembership> memberships,
        Sprint sprint,
        List<string> excludedStatuses,
        int planningWindowDays,
        int defaultSpPerBug,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int startIndex)
    {
        var planningCutoff = sprint.StartDate.AddDays(planningWindowDays);
        var sprintStart = sprint.StartDate;
        var sprintEnd = sprint.EndDate;

        var events = new List<ScopeChangeEvent>();

        foreach (var m in memberships)
        {
            var isExcluded = IsExcluded(m.FinalStatus, excludedStatuses);
            var ticketKey = m.TicketId;
            var ticketSummary = m.Ticket?.Summary ?? m.TicketId;
            var issueType = m.Ticket?.IssueType ?? "Unknown";
            var effectiveSp = m.GetEffectiveSp(defaultSpPerBug);

            // Added event (all non-committed memberships appear in the event table, spec BR16)
            if (!m.WasCommitted)
            {
                var dayNumber = (int)(m.AddedAt - sprint.StartDate).TotalDays + 1;

                // Classification category only for tickets that qualify as Added SP:
                // post-planning AND cycle-entered AND non-bug AND non-excluded AND not removed (spec BR16)
                string? category = null;
                if (m.AddedAt > planningCutoff
                    && m.RemovedAt == null
                    && m.Ticket?.IssueType != "Bug"
                    && !isExcluded)
                {
                    var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                        m.TicketId, transitionIndex, sprintStart, sprintEnd, orderedStages, startIndex);
                    if (isStarted)
                        category = ClassifyAddition(m, sprint);
                }

                events.Add(new ScopeChangeEvent(
                    m.AddedAt, dayNumber, ticketKey, ticketSummary,
                    effectiveSp, issueType, "added", category, isExcluded));
            }

            // Removed event
            if (m.RemovedAt.HasValue)
            {
                var removeDayNumber = (int)(m.RemovedAt.Value - sprint.StartDate).TotalDays + 1;
                events.Add(new ScopeChangeEvent(
                    m.RemovedAt.Value, removeDayNumber, ticketKey, ticketSummary,
                    effectiveSp, issueType, "removed", null, isExcluded));
            }
        }

        return events.OrderBy(e => e.Date).ToList();
    }

    // --- Burnup chart data (transition-based cumulative lines — spec BR11) ---

    private static List<BurnupDataPoint> BuildBurnupData(
        List<SprintMembership> memberships,
        Sprint sprint,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        List<string> excludedStatuses,
        int planningWindowDays,
        int defaultSpPerBug)
    {
        var sprintDays = (int)(sprint.EndDate - sprint.StartDate).TotalDays + 1;
        if (sprintDays <= 0) sprintDays = 1;

        var (startTransitionByFeatureTicket, endTransitionByFeatureTicket) = FindFeatureBoundaryTransitions(
            memberships, sprint, transitionIndex, orderedStages, startIndex, endIndex, excludedStatuses, defaultSpPerBug);

        var bugAreaSpans = BuildBugAreaSpans(
            memberships, sprint, transitionIndex, orderedStages, endIndex, excludedStatuses, defaultSpPerBug);

        var result = new List<BurnupDataPoint>();
        var cumulativeTotalScope = 0m;    // cumulative feature SP started (scope line starts near 0)
        var cumulativeCompleted = 0m;     // cumulative feature SP completed
        var cumulativeTotalScopeTickets = 0;
        var cumulativeCompletedTickets = 0;

        for (var i = 0; i < sprintDays; i++)
        {
            var day = sprint.StartDate.AddDays(i);
            var dayNumber = i + 1;

            // Feature tickets that first transitioned to startStage on this day (scope line grows)
            var (startedTodaySp, startedTodayTickets) = SumFeaturesFirstOnDay(
                memberships, startTransitionByFeatureTicket, day, excludedStatuses, defaultSpPerBug);

            cumulativeTotalScope += startedTodaySp;
            cumulativeTotalScopeTickets += startedTodayTickets;

            // Feature tickets that first transitioned to endStage on this day (completed line grows)
            var (completedTodaySp, completedTodayTickets) = SumFeaturesFirstOnDay(
                memberships, endTransitionByFeatureTicket, day, excludedStatuses, defaultSpPerBug);

            cumulativeCompleted += completedTodaySp;
            cumulativeCompletedTickets += completedTodayTickets;

            var bugsInArea = bugAreaSpans
                .Where(b => b.EntryDay <= day.Date && (b.ExitDay is null || b.ExitDay > day.Date))
                .ToList();
            var bugSpDay = bugsInArea.Sum(b => b.Sp);

            var (committedTotalSpDay, committedTotalTicketsDay) = SumCommittedOnDay(memberships, day, defaultSpPerBug);

            var phase = dayNumber <= planningWindowDays ? "planning" : "execution";

            result.Add(new BurnupDataPoint(
                dayNumber,
                day,
                Math.Round(cumulativeTotalScope, 1),
                Math.Round(cumulativeCompleted, 1),
                phase,
                Math.Round(bugSpDay, 1),
                cumulativeTotalScopeTickets,
                cumulativeCompletedTickets,
                bugsInArea.Count,
                Math.Round(committedTotalSpDay, 1),
                committedTotalTicketsDay));
        }

        return result;
    }

    private static (Dictionary<string, DateTime> FirstStart, Dictionary<string, DateTime> FirstEnd)
        FindFeatureBoundaryTransitions(
            List<SprintMembership> memberships,
            Sprint sprint,
            StatusTransitionIndex transitionIndex,
            List<string> orderedStages,
            int startIndex,
            int endIndex,
            List<string> excludedStatuses,
            int defaultSpPerBug)
    {
        // Build lookup: ticketId -> first qualifying start transition timestamp (feature-only)
        var startTransitionByFeatureTicket = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        // Build lookup: ticketId -> first qualifying end transition timestamp (feature-only)
        var endTransitionByFeatureTicket = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        foreach (var m in memberships)
        {
            if (m.RemovedAt != null) continue;
            if (!m.GetEffectiveSp(defaultSpPerBug).HasValue) continue;
            if (IsExcluded(m.FinalStatus, excludedStatuses)) continue;
            if (m.Ticket?.IssueType == "Bug") continue;

            // Feature: record first start transition within sprint
            var firstStart = transitionIndex.ForTicket(m.TicketId)
                .Where(t => t.Timestamp >= sprint.StartDate
                            && t.Timestamp <= sprint.EndDate
                            && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= startIndex)
                .OrderBy(t => t.Timestamp)
                .FirstOrDefault();
            if (firstStart is not null)
                startTransitionByFeatureTicket[m.TicketId] = firstStart.Timestamp;

            // Feature: record first end transition within sprint
            if (endIndex >= 0)
            {
                var firstEnd = transitionIndex.ForTicket(m.TicketId)
                    .Where(t => t.Timestamp >= sprint.StartDate
                                && t.Timestamp <= sprint.EndDate
                                && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex)
                    .OrderBy(t => t.Timestamp)
                    .FirstOrDefault();
                if (firstEnd is not null)
                    endTransitionByFeatureTicket[m.TicketId] = firstEnd.Timestamp;
            }
        }

        return (startTransitionByFeatureTicket, endTransitionByFeatureTicket);
    }

    private static (decimal Sp, int Tickets) SumFeaturesFirstOnDay(
        List<SprintMembership> memberships,
        Dictionary<string, DateTime> firstTransitionByTicket,
        DateTime day,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var matching = memberships
            .Where(m => m.RemovedAt == null
                        && m.Ticket?.IssueType != "Bug"
                        && m.GetEffectiveSp(defaultSpPerBug).HasValue
                        && !IsExcluded(m.FinalStatus, excludedStatuses)
                        && firstTransitionByTicket.TryGetValue(m.TicketId, out var ts)
                        && ts.Date == day.Date)
            .ToList();
        return (matching.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value), matching.Count);
    }

    // Committed Total: feature SP in sprint on this day (WasCommitted from day 1, mid-sprint by AddedAt)
    private static (decimal Sp, int Tickets) SumCommittedOnDay(
        List<SprintMembership> memberships,
        DateTime day,
        int defaultSpPerBug)
    {
        var matching = memberships
            .Where(m => m.Ticket?.IssueType != "Bug"
                        && m.GetEffectiveSp(defaultSpPerBug).HasValue
                        && (m.WasCommitted || m.AddedAt.Date <= day.Date)
                        && (m.RemovedAt == null || m.RemovedAt.Value.Date > day.Date))
            .ToList();
        return (matching.Sum(m => m.GetEffectiveSp(defaultSpPerBug)!.Value), matching.Count);
    }

    // --- Burnup bug area ---

    private record BugAreaSpan(decimal Sp, DateTime EntryDay, DateTime? ExitDay);

    private static List<BugAreaSpan> BuildBugAreaSpans(
        List<SprintMembership> memberships,
        Sprint sprint,
        StatusTransitionIndex transitionIndex,
        List<string> orderedStages,
        int endIndex,
        List<string> excludedStatuses,
        int defaultSpPerBug)
    {
        var spans = new List<BugAreaSpan>();
        foreach (var m in memberships)
        {
            if (m.Ticket?.IssueType != "Bug") continue;
            var sp = m.GetEffectiveSp(defaultSpPerBug);
            if (!sp.HasValue || IsExcluded(m.FinalStatus, excludedStatuses)) continue;
            if (m.RemovedAt <= sprint.StartDate) continue;

            var entryDay = m.AddedAt <= sprint.StartDate ? sprint.StartDate.Date : m.AddedAt.Date;
            var completedAt = endIndex >= 0
                ? transitionIndex.ForTicket(m.TicketId)
                    .Where(t => t.Timestamp >= sprint.StartDate
                                && t.Timestamp <= sprint.EndDate
                                && TransitionAttributionChecker.GetStageIndex(t.ToStatus, orderedStages) >= endIndex)
                    .OrderBy(t => t.Timestamp)
                    .FirstOrDefault()?.Timestamp
                : null;

            spans.Add(new BugAreaSpan(sp.Value, entryDay, ResolveExitDay(entryDay, completedAt, m.RemovedAt)));
        }

        return spans;
    }

    private static DateTime? ResolveExitDay(DateTime entryDay, DateTime? completedAt, DateTime? removedAt)
    {
        DateTime? leftAt = (completedAt, removedAt) switch
        {
            (null, null) => null,
            (null, var removed) => removed,
            (var completed, null) => completed,
            var (completed, removed) => completed < removed ? completed : removed
        };

        if (leftAt is null) return null;
        return leftAt.Value.Date > entryDay ? leftAt.Value.Date : entryDay;
    }

    // --- Bug time-in-progress ---

    private static List<BugTimeInProgress> ComputeBugTimeInProgress(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        List<string> workflowStages)
    {
        var midSprintBugs = memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null && m.Ticket?.IssueType == "Bug")
            .ToList();

        if (midSprintBugs.Count == 0)
            return [];

        var activeStatuses = GetActiveStatuses(workflowStages);

        var result = new List<BugTimeInProgress>();

        foreach (var m in midSprintBugs)
        {
            var ticketTransitions = transitionIndex.ForTicket(m.TicketId)
                .OrderBy(t => t.Timestamp)
                .ToList();

            var timeInActive = ComputeTimeInActiveStatuses(ticketTransitions, activeStatuses, sprint.EndDate);

            result.Add(new BugTimeInProgress(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                Math.Round(timeInActive, 2),
                m.Ticket?.CurrentStatus ?? m.FinalStatus));
        }

        return result;
    }

    private static HashSet<string> GetActiveStatuses(List<string> workflowStages)
    {
        if (workflowStages.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "In Progress" };

        if (workflowStages.Count <= 2)
            return new HashSet<string>(workflowStages, StringComparer.OrdinalIgnoreCase);

        var middle = workflowStages.Skip(1).Take(workflowStages.Count - 2).ToList();
        return middle.Count > 0
            ? new HashSet<string>(middle, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "In Progress" };
    }

    private static decimal ComputeTimeInActiveStatuses(
        List<StatusTransition> transitions,
        HashSet<string> activeStatuses,
        DateTime sprintEnd)
    {
        var totalTime = 0m;

        for (var i = 0; i < transitions.Count; i++)
        {
            var t = transitions[i];
            if (!activeStatuses.Contains(t.ToStatus)) continue;

            var exitTime = sprintEnd;
            for (var j = i + 1; j < transitions.Count; j++)
            {
                if (!activeStatuses.Contains(transitions[j].ToStatus))
                {
                    exitTime = transitions[j].Timestamp;
                    break;
                }
            }

            var duration = (exitTime - t.Timestamp).TotalDays;
            if (duration > 0)
                totalTime += (decimal)duration;
        }

        return totalTime;
    }
}
