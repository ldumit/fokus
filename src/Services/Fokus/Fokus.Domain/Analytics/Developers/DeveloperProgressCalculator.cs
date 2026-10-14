using Fokus.Domain.Analytics.Shared;
using static Fokus.Domain.Analytics.Shared.BusinessDayCalculator;

namespace Fokus.Domain.Analytics.Developers;

public class DeveloperProgressCalculator
{
    public DeveloperProgressResult ComputeProgress(
        Sprint? activeSprint,
        List<Developer> activeDevelopers,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        string? subTeam,
        List<Developer> allDevelopers,
        List<StatusTransition> statusTransitions,
        HashSet<string> excludedDeveloperIds,
        DateTime today)
    {
        if (activeSprint is null)
            return new DeveloperProgressResult(false, null, 0, 0, false, [], []);

        // BR8: totalDays inclusive, min 1
        var totalDays = Math.Max(1, (activeSprint.EndDate.Date - activeSprint.StartDate.Date).Days + 1);

        // BR9: currentDay clamped to [1, totalDays]
        var currentDay = Math.Min(totalDays, Math.Max(1, (today - activeSprint.StartDate.Date).Days + 1));

        // BR11: grace period = first 2 days
        var isGracePeriod = currentDay <= 2;

        // Resolve end stage for completion check
        var (orderedStages, _) = TransitionAttributionChecker.ResolveStartIndex(settings);
        var endIndex = TransitionAttributionChecker.ResolveEndIndex(settings, orderedStages);
        var (_, startIndex) = TransitionAttributionChecker.ResolveStartIndex(settings);

        var context = new ProgressContext(
            activeSprint, settings, totalDays, currentDay, isGracePeriod,
            orderedStages, startIndex, endIndex, StatusTransitionIndex.Build(statusTransitions), today);

        // Capacity lookup: accountId -> capacityPercent (sprint-specific first, then developer default)
        var capacityLookup = capacityRecords
            .Where(c => c.SprintId == activeSprint.Id)
            .ToDictionary(c => c.DeveloperAccountId, c => c.CapacityPercent);

        // Filter developers: sub-team + active + not excluded
        var filteredDevelopers = activeDevelopers
            .Where(d => !excludedDeveloperIds.Contains(d.Id))
            .Where(d => d.IsActive)
            .Where(d => string.IsNullOrWhiteSpace(subTeam) || d.SubTeam == subTeam)
            .ToList();

        // Build developer entries alongside yesterday's stall set for direction derivation
        var developerData = filteredDevelopers
            .Select(developer => BuildDeveloperProgress(
                developer, GetCapacity(capacityLookup, developer.Id, allDevelopers), context))
            .ToList();

        var developerEntries = developerData.Select(d => d.Entry).ToList();

        // Build alerts (pace-change developers, suppressed during grace period)
        var alerts = isGracePeriod ? new List<DeveloperProgressAlert>() : BuildAlerts(developerData);

        var sprintInfo = new SprintRef(
            activeSprint.Id, activeSprint.Name, activeSprint.StartDate, activeSprint.EndDate);

        return new DeveloperProgressResult(
            true, sprintInfo, currentDay, totalDays, isGracePeriod, alerts, developerEntries);
    }

    private DeveloperProgressData BuildDeveloperProgress(Developer developer, int capacity, ProgressContext context)
    {
        // BR1-2: all non-removed memberships, all ticket types
        var memberships = context.Sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developer.Id && m.RemovedAt == null)
            .ToList();

        // Assigned SP: sum of effective SP (null excluded from sum per plan)
        var assignedSp = memberships
            .Select(m => m.GetEffectiveSp(context.Settings.DefaultSpPerBug))
            .Where(sp => sp.HasValue)
            .Sum(sp => sp!.Value);

        // BR10: dailyPace = assignedSp * (capacity / 100) / totalDays
        var dailyPace = assignedSp > 0
            ? assignedSp * (capacity / 100m) / context.TotalDays
            : 0m;

        var completions = TallyCompletions(memberships, context);
        var dailyBreakdown = BuildDailyBreakdown(completions.ByDay, dailyPace, context);

        // BR12-14: behind-pace detection (suppressed during grace period)
        var completionPercent = assignedSp > 0 ? completions.CompletedSp / assignedSp * 100m : 0m;
        var (isBehindPace, paceGapSp) = ResolvePaceGap(dailyPace, completions.CompletedSp, context);

        // BR15-17: stall detection for today and yesterday (direction derivation)
        var stalledTickets = BuildStalledTickets(
            memberships, context.TransitionIndex, context.Sprint,
            context.OrderedStages, context.StartIndex, context.EndIndex, context.Today);
        var yesterdayStalledKeys = BuildStalledTickets(
            memberships, context.TransitionIndex, context.Sprint,
            context.OrderedStages, context.StartIndex, context.EndIndex, context.Today.AddDays(-1))
            .Select(t => t.Key)
            .ToHashSet();

        var entry = new DeveloperProgressEntry(
            developer.Id,
            developer.DisplayName,
            developer.SubTeam,
            developer.AvatarUrl,
            Math.Round(assignedSp, 1),
            Math.Round(completions.CompletedSp, 1),
            Math.Round(completions.FeatureCompletedSp, 1),
            Math.Round(completions.BugCompletedSp, 1),
            Math.Round(completionPercent, 1),
            capacity,
            Math.Round(dailyPace, 10), // preserve precision; rounded at usage
            isBehindPace,
            paceGapSp,
            stalledTickets,
            dailyBreakdown);

        return new DeveloperProgressData(entry, yesterdayStalledKeys, developer);
    }

    // Completion: tickets completed in sprint via IsCompletedInSprint
    // Build per-ticket completion info for daily breakdown
    private static CompletionTally TallyCompletions(List<SprintMembership> memberships, ProgressContext context)
    {
        var sprint = context.Sprint;
        var completionsByDay = new Dictionary<int, List<CompletedTicketEntry>>();
        var completedSp = 0m;
        var featureCompletedSp = 0m;
        var bugCompletedSp = 0m;

        foreach (var m in memberships)
        {
            var (isCompleted, completedAt) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, context.TransitionIndex,
                sprint.StartDate, sprint.EndDate,
                context.OrderedStages, context.EndIndex);

            if (!isCompleted || completedAt is null) continue;

            var effectiveSp = m.GetEffectiveSp(context.Settings.DefaultSpPerBug);
            if (effectiveSp.HasValue)
            {
                completedSp += effectiveSp.Value;
                if (m.Ticket?.IssueType == "Bug")
                    bugCompletedSp += effectiveSp.Value;
                else
                    featureCompletedSp += effectiveSp.Value;
            }

            // BR3: completion day = (completedAt.Date - sprintStart.Date).Days + 1
            var completionDay = (completedAt.Value.Date - sprint.StartDate.Date).Days + 1;
            completionDay = Math.Min(completionDay, context.CurrentDay); // clamp to currentDay

            if (!completionsByDay.ContainsKey(completionDay))
                completionsByDay[completionDay] = [];

            completionsByDay[completionDay].Add(new CompletedTicketEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                effectiveSp,
                m.Ticket?.IssueType ?? "Unknown"));
        }

        return new CompletionTally(completionsByDay, completedSp, featureCompletedSp, bugCompletedSp);
    }

    // Build daily breakdown 1..currentDay with running cumulative
    private static List<DayBreakdownEntry> BuildDailyBreakdown(
        Dictionary<int, List<CompletedTicketEntry>> completionsByDay,
        decimal dailyPace,
        ProgressContext context)
    {
        var dailyBreakdown = new List<DayBreakdownEntry>();
        var cumulativeSp = 0m;
        for (var day = 1; day <= context.CurrentDay; day++)
        {
            var dayTickets = completionsByDay.TryGetValue(day, out var tickets) ? tickets : [];
            var daySp = dayTickets
                .Where(t => t.StoryPoints.HasValue)
                .Sum(t => t.StoryPoints!.Value);
            cumulativeSp += daySp;

            var expectedCumulative = Math.Round(dailyPace * day, 10); // keep precision for comparison
            var date = context.Sprint.StartDate.Date.AddDays(day - 1);
            dailyBreakdown.Add(new DayBreakdownEntry(day, date, dayTickets, cumulativeSp, expectedCumulative));
        }

        return dailyBreakdown;
    }

    private static (bool IsBehindPace, decimal? PaceGapSp) ResolvePaceGap(
        decimal dailyPace,
        decimal completedSp,
        ProgressContext context)
    {
        if (context.IsGracePeriod || dailyPace <= 0)
            return (false, null);

        var expectedAtCurrentDay = dailyPace * context.CurrentDay;
        var gap = expectedAtCurrentDay - completedSp;
        return (gap > dailyPace, Math.Round(gap, 1));
    }

    private static List<DeveloperProgressAlert> BuildAlerts(List<DeveloperProgressData> developerData)
    {
        var alerts = developerData
            .Select(BuildAlert)
            .OfType<DeveloperProgressAlert>()
            .ToList();

        // Sort: worsening/new-stall group first, then improving/stall-resolved
        // Within worsening: new-stall at top, then by gapDelta desc (largest gap increase first)
        // Within improving: stall-resolved at top, then by gapDelta asc (largest improvement first)
        return alerts
            .OrderBy(a => a.Direction is "improving" or "stall-resolved" ? 1 : 0)
            .ThenBy(a => a.Direction is "new-stall" or "stall-resolved" ? 0 : 1)
            .ThenBy(a => a.Direction is "improving" or "stall-resolved"
                ? a.GapDelta ?? 0m
                : -(a.GapDelta ?? 0m))
            .ToList();
    }

    private static DeveloperProgressAlert? BuildAlert(DeveloperProgressData data)
    {
        var (entry, yesterdayStalledKeys, devRecord) = data;

        // Gap delta: gap(currentDay) - gap(previousDay); null when breakdown has < 2 days
        decimal? gapDelta = null;
        if (entry.DailyBreakdown.Count >= 2)
        {
            var last = entry.DailyBreakdown[^1];
            var prev = entry.DailyBreakdown[^2];
            var gapCurrent = last.ExpectedCumulativeSp - last.CumulativeSp;
            var gapPrev = prev.ExpectedCumulativeSp - prev.CumulativeSp;
            gapDelta = Math.Round(gapCurrent - gapPrev, 1);
        }

        var direction = DeriveDirection(entry, yesterdayStalledKeys, gapDelta);
        if (direction == "stable") return null;

        var gapSp = entry.PaceGapSp ?? 0m;
        var gapDays = entry.DailyPace > 0 && entry.PaceGapSp.HasValue
            ? Math.Round(entry.PaceGapSp.Value / entry.DailyPace, 1)
            : 0m;
        return new DeveloperProgressAlert(
            entry.AccountId,
            entry.DisplayName,
            devRecord.AvatarUrl,
            gapSp,
            gapDays,
            gapDelta,
            direction);
    }

    private static string DeriveDirection(
        DeveloperProgressEntry entry,
        HashSet<string> yesterdayStalledKeys,
        decimal? gapDelta)
    {
        // Stall signals (spec BR6 — stall takes priority over gap direction)
        var todayStalledKeys = entry.StalledTickets.Select(t => t.Key).ToHashSet();
        var hasNewStall = todayStalledKeys.Any(k => !yesterdayStalledKeys.Contains(k));
        var hadStallYesterday = yesterdayStalledKeys.Count > 0;
        var hasNoStallToday = todayStalledKeys.Count == 0;
        var isStallResolved = hadStallYesterday && hasNoStallToday;

        // Direction derivation (stall takes priority)
        if (hasNewStall)
            return "new-stall";
        if (isStallResolved)
            return "stall-resolved";
        if (gapDelta.HasValue && gapDelta.Value > 0)
            return "worsening";
        if (gapDelta.HasValue && gapDelta.Value < 0)
            return "improving";
        return "stable";
    }

    // --- Stall detection (BR15-17) ---

    private List<StalledTicketEntry> BuildStalledTickets(
        List<SprintMembership> memberships,
        StatusTransitionIndex transitionIndex,
        Sprint sprint,
        List<string> orderedStages,
        int startIndex,
        int endIndex,
        DateTime referenceDate)
    {
        var stalled = new List<StalledTicketEntry>();

        foreach (var m in memberships)
        {
            // BR15a: non-removed (already filtered by caller)
            // BR15b: started in sprint
            var (isStarted, _) = TransitionAttributionChecker.IsStartedInSprint(
                m.TicketId, transitionIndex,
                sprint.StartDate, sprint.EndDate,
                orderedStages, startIndex);

            if (!isStarted) continue;

            // BR15c: NOT completed
            var (isCompleted, _) = TransitionAttributionChecker.IsCompletedInSprint(
                m.TicketId, transitionIndex,
                sprint.StartDate, sprint.EndDate,
                orderedStages, endIndex);

            if (isCompleted) continue;

            // BR15d: most recent transition > 2 business days ago
            var ticketTransitions = transitionIndex.ForTicket(m.TicketId)
                .OrderByDescending(t => t.Timestamp)
                .ToList();

            if (ticketTransitions.Count == 0) continue;

            var lastTransition = ticketTransitions[0].Timestamp;
            var businessDays = CountBusinessDays(lastTransition, referenceDate);

            if (businessDays <= 2) continue;

            stalled.Add(new StalledTicketEntry(
                m.TicketId,
                m.Ticket?.Summary ?? m.TicketId,
                m.Ticket?.CurrentStatus ?? m.FinalStatus,
                m.Ticket?.IssueType ?? "Unknown",
                m.GetEffectiveSp(0),
                businessDays));
        }

        return stalled;
    }

    // --- Capacity resolution ---

    private static int GetCapacity(
        Dictionary<string, int> capacityLookup,
        string developerId,
        List<Developer> allDevelopers)
    {
        if (capacityLookup.TryGetValue(developerId, out var cap))
            return cap;
        var dev = allDevelopers.FirstOrDefault(d => d.Id == developerId);
        return dev?.DefaultCapacityPercent ?? 100;
    }

    private sealed record ProgressContext(
        Sprint Sprint,
        AppSettings Settings,
        int TotalDays,
        int CurrentDay,
        bool IsGracePeriod,
        List<string> OrderedStages,
        int StartIndex,
        int EndIndex,
        StatusTransitionIndex TransitionIndex,
        DateTime Today);

    private sealed record DeveloperProgressData(
        DeveloperProgressEntry Entry,
        HashSet<string> YesterdayStalledKeys,
        Developer Developer);

    private sealed record CompletionTally(
        Dictionary<int, List<CompletedTicketEntry>> ByDay,
        decimal CompletedSp,
        decimal FeatureCompletedSp,
        decimal BugCompletedSp);
}
