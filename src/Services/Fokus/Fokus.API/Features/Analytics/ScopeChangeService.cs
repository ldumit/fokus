namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record ScopeChangeSprintInfo(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record ScopeChangeSummaryMetrics(
    decimal AverageDisruptionRate,
    decimal? AverageDisruptionRateDelta,
    string? AverageDisruptionRateDeltaDirection,
    decimal AverageNetScopeChange,
    int TotalBugsAdded);

public record ScopeChangePerSprintData(
    int SprintId,
    decimal CommittedSpActive,
    decimal CommittedSpTotal,
    decimal AddedSp,
    decimal RemovedSp,
    decimal NetScopeChange,
    decimal CompletedSp,
    decimal DisruptionRate,
    int BugCount);

public record ClassificationEntry(
    string Category,
    int TicketCount,
    decimal? SpTotal,
    decimal Percentage);

public record ScopeChangeMultiSprintResponse(
    List<ScopeChangeSprintInfo> Sprints,
    ScopeChangeSummaryMetrics SummaryMetrics,
    List<ScopeChangePerSprintData> PerSprintData,
    List<ClassificationEntry> ClassificationBreakdown);

public record ScopeMetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);

public record ScopeChangeSingleSprintMetrics(
    ScopeMetricCard CommittedSpActive,
    ScopeMetricCard CommittedSpTotal,
    ScopeMetricCard AddedSp,
    ScopeMetricCard RemovedSp,
    ScopeMetricCard NetScopeChange,
    ScopeMetricCard DisruptionRate,
    ScopeMetricCard BugCount);

public record BurnupDataPoint(
    int DayNumber,
    DateTime Date,
    decimal TotalScopeSp,
    decimal CompletedSp,
    string Phase);

public record ScopeChangeEvent(
    DateTime Date,
    int SprintDayNumber,
    string TicketKey,
    string TicketSummary,
    decimal? StoryPoints,
    string IssueType,
    string Action,
    string? Category,
    bool IsExcluded);

public record BugTimeInProgress(
    string TicketKey,
    string TicketSummary,
    decimal TimeInActiveDays,
    string CurrentStatus);

public record ScopeChangeSingleSprintResponse(
    ScopeChangeSprintInfo Sprint,
    ScopeChangeSingleSprintMetrics Metrics,
    List<BurnupDataPoint> BurnupData,
    List<ClassificationEntry> ClassificationBreakdown,
    List<ScopeChangeEvent> Events,
    List<BugTimeInProgress> BugTimeInProgress);

public record ScopeChangeResponse(
    string Mode,
    ScopeChangeMultiSprintResponse? MultiSprint,
    ScopeChangeSingleSprintResponse? SingleSprint);

// --- Service ---

public class ScopeChangeService
{
    public ScopeChangeMultiSprintResponse ComputeMultiSprint(
        List<Sprint> sprints,
        AppSettings settings,
        string? subTeam)
    {
        var sortedSprints = sprints.OrderBy(s => s.StartDate).ToList();
        var doneStatuses = settings.DoneStatuses;
        var excludedStatuses = settings.ExcludedFromScopeStatuses;

        var sprintInfos = sortedSprints
            .Select(s => new ScopeChangeSprintInfo(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var perSprintData = sortedSprints
            .Select(s => ComputePerSprintData(s, FilterMemberships(s.Memberships, subTeam), doneStatuses, excludedStatuses))
            .ToList();

        var allClassificationEntries = sortedSprints
            .SelectMany(s => GetMidSprintAdditions(FilterMemberships(s.Memberships, subTeam), s))
            .ToList();

        var classificationBreakdown = BuildClassificationBreakdown(allClassificationEntries);

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

        return new ScopeChangeMultiSprintResponse(sprintInfos, summaryMetrics, perSprintData, classificationBreakdown);
    }

    public ScopeChangeSingleSprintResponse ComputeSingleSprint(
        Sprint targetSprint,
        Sprint? priorSprint,
        List<StatusTransition> statusTransitions,
        AppSettings settings,
        string? subTeam)
    {
        var doneStatuses = settings.DoneStatuses;
        var excludedStatuses = settings.ExcludedFromScopeStatuses;
        var workflowStages = settings.WorkflowStages;

        var memberships = FilterMemberships(targetSprint.Memberships, subTeam);
        var priorMemberships = priorSprint is not null
            ? FilterMemberships(priorSprint.Memberships, subTeam)
            : null;

        var sprintInfo = new ScopeChangeSprintInfo(
            targetSprint.Id, targetSprint.Name, targetSprint.StartDate, targetSprint.EndDate);

        // Core metrics for target sprint
        var current = ComputeSprintMetrics(memberships, doneStatuses, excludedStatuses);
        ScopeSprintMetrics? prior = priorMemberships is not null
            ? ComputeSprintMetrics(priorMemberships, doneStatuses, excludedStatuses)
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
        var midSprintAdditions = GetMidSprintAdditions(memberships, targetSprint);
        var classificationBreakdown = BuildClassificationBreakdown(midSprintAdditions);

        // Event table
        var events = BuildEventTable(memberships, targetSprint, excludedStatuses);

        // Burnup chart
        var burnupData = BuildBurnupData(memberships, targetSprint, doneStatuses, excludedStatuses, statusTransitions);

        // Bug time-in-progress
        var bugTimeInProgress = ComputeBugTimeInProgress(memberships, statusTransitions, targetSprint, workflowStages);

        return new ScopeChangeSingleSprintResponse(
            sprintInfo, metrics, burnupData, classificationBreakdown, events, bugTimeInProgress);
    }

    // --- Sub-team filtering (C2) ---

    private static List<SprintMembership> FilterMemberships(
        IReadOnlyList<SprintMembership> memberships,
        string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return memberships.ToList();

        return memberships
            .Where(m => m.Ticket?.Assignee?.SubTeam == subTeam)
            .ToList();
    }

    // --- Per-sprint data computation ---

    private static ScopeChangePerSprintData ComputePerSprintData(
        Sprint sprint,
        List<SprintMembership> memberships,
        List<string> doneStatuses,
        List<string> excludedStatuses)
    {
        var metrics = ComputeSprintMetrics(memberships, doneStatuses, excludedStatuses);
        return new ScopeChangePerSprintData(
            sprint.Id,
            Math.Round(metrics.CommittedSpActive, 1),
            Math.Round(metrics.CommittedSpTotal, 1),
            Math.Round(metrics.AddedSp, 1),
            Math.Round(metrics.RemovedSp, 1),
            Math.Round(metrics.NetScopeChange, 1),
            Math.Round(metrics.CompletedSp, 1),
            Math.Round(metrics.DisruptionRate, 1),
            metrics.BugCount);
    }

    private record ScopeSprintMetrics(
        decimal CommittedSpActive,
        decimal CommittedSpTotal,
        decimal AddedSp,
        decimal RemovedSp,
        decimal CompletedSp,
        decimal NetScopeChange,
        decimal DisruptionRate,
        int BugCount);

    private static ScopeSprintMetrics ComputeSprintMetrics(
        List<SprintMembership> memberships,
        List<string> doneStatuses,
        List<string> excludedStatuses)
    {
        var committedSpActive = memberships
            .Where(m => m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue
                        && !IsExcluded(m.FinalStatus, excludedStatuses))
            .Sum(m => m.StoryPoints!.Value);

        var committedSpTotal = memberships
            .Where(m => m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);

        var addedSp = memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue
                        && !IsExcluded(m.FinalStatus, excludedStatuses))
            .Sum(m => m.StoryPoints!.Value);

        var removedSp = memberships
            .Where(m => m.RemovedAt != null && m.StoryPoints.HasValue)
            .Sum(m => m.StoryPoints!.Value);

        var completedSp = memberships
            .Where(m => doneStatuses.Contains(m.FinalStatus) && m.RemovedAt == null && m.StoryPoints.HasValue
                        && !IsExcluded(m.FinalStatus, excludedStatuses))
            .Sum(m => m.StoryPoints!.Value);

        var netScopeChange = addedSp - removedSp;
        var disruptionRate = committedSpActive > 0 ? addedSp / committedSpActive * 100 : 0;

        var bugCount = memberships
            .Count(m => !m.WasCommitted && m.RemovedAt == null && m.Ticket?.IssueType == "Bug");

        return new ScopeSprintMetrics(
            committedSpActive, committedSpTotal, addedSp, removedSp, completedSp,
            netScopeChange, disruptionRate, bugCount);
    }

    // --- Excluded status check (case-insensitive) ---

    private static bool IsExcluded(string status, List<string> excludedStatuses) =>
        excludedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);

    // --- Classification ---

    private record MidSprintAddition(SprintMembership Membership, string Category);

    private static List<MidSprintAddition> GetMidSprintAdditions(
        List<SprintMembership> memberships,
        Sprint sprint)
    {
        var planningCutoff = sprint.StartDate.AddDays(2);
        return memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null)
            .Select(m => new MidSprintAddition(m, ClassifyAddition(m, sprint, planningCutoff)))
            .ToList();
    }

    private static string ClassifyAddition(SprintMembership m, Sprint sprint, DateTime planningCutoff)
    {
        if (m.AddedAt <= planningCutoff)
            return "Planning Overflow";

        if (m.Ticket?.IssueType == "Bug")
            return "Unplanned Bug";

        if (m.Ticket?.CreatedDate < sprint.StartDate)
            return "Priority Escalation";

        return "Scope Injection";
    }

    private static List<ClassificationEntry> BuildClassificationBreakdown(
        List<MidSprintAddition> additions)
    {
        var total = additions.Count;

        var categories = new[] { "Planning Overflow", "Unplanned Bug", "Priority Escalation", "Scope Injection" };

        return categories.Select(category =>
        {
            var items = additions.Where(a => a.Category == category).ToList();
            var count = items.Count;
            var spItems = items.Where(a => a.Membership.StoryPoints.HasValue).ToList();
            decimal? spTotal = spItems.Count > 0 ? spItems.Sum(a => a.Membership.StoryPoints!.Value) : null;
            var percentage = total > 0 ? Math.Round((decimal)count / total * 100, 1) : 0;
            return new ClassificationEntry(category, count, spTotal.HasValue ? Math.Round(spTotal.Value, 1) : null, percentage);
        }).ToList();
    }

    // --- Event table ---

    private static List<ScopeChangeEvent> BuildEventTable(
        List<SprintMembership> memberships,
        Sprint sprint,
        List<string> excludedStatuses)
    {
        var planningCutoff = sprint.StartDate.AddDays(2);
        var events = new List<ScopeChangeEvent>();

        foreach (var m in memberships)
        {
            var isExcluded = IsExcluded(m.FinalStatus, excludedStatuses);
            var ticketKey = m.TicketId;
            var ticketSummary = m.Ticket?.Summary ?? m.TicketId;
            var issueType = m.Ticket?.IssueType ?? "Unknown";

            // Added event (mid-sprint additions)
            if (!m.WasCommitted)
            {
                var dayNumber = (int)(m.AddedAt - sprint.StartDate).TotalDays + 1;
                var category = ClassifyAddition(m, sprint, planningCutoff);
                events.Add(new ScopeChangeEvent(
                    m.AddedAt, dayNumber, ticketKey, ticketSummary,
                    m.StoryPoints, issueType, "added", category, isExcluded));
            }

            // Removed event
            if (m.RemovedAt.HasValue)
            {
                var removeDayNumber = (int)(m.RemovedAt.Value - sprint.StartDate).TotalDays + 1;
                events.Add(new ScopeChangeEvent(
                    m.RemovedAt.Value, removeDayNumber, ticketKey, ticketSummary,
                    m.StoryPoints, issueType, "removed", null, isExcluded));
            }
        }

        return events.OrderBy(e => e.Date).ToList();
    }

    // --- Burnup chart data ---

    private static List<BurnupDataPoint> BuildBurnupData(
        List<SprintMembership> memberships,
        Sprint sprint,
        List<string> doneStatuses,
        List<string> excludedStatuses,
        List<StatusTransition> statusTransitions)
    {
        var sprintDays = (int)(sprint.EndDate - sprint.StartDate).TotalDays + 1;
        if (sprintDays <= 0) sprintDays = 1;

        // Build a lookup: ticketId -> first transition to done status within sprint
        var doneTransitionByTicket = statusTransitions
            .Where(t => doneStatuses.Contains(t.ToStatus) && t.Timestamp >= sprint.StartDate && t.Timestamp <= sprint.EndDate)
            .GroupBy(t => t.TicketId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Timestamp).First().Timestamp);

        // Starting committed SP (active, not excluded)
        var startingCommitted = memberships
            .Where(m => m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue
                        && !IsExcluded(m.FinalStatus, excludedStatuses))
            .Sum(m => m.StoryPoints!.Value);

        var result = new List<BurnupDataPoint>();
        var cumulativeTotalScope = startingCommitted;
        var cumulativeCompleted = 0m;

        for (var i = 0; i < sprintDays; i++)
        {
            var day = sprint.StartDate.AddDays(i);
            var dayNumber = i + 1;

            // Scope additions on this day
            var addedToday = memberships
                .Where(m => !m.WasCommitted && m.RemovedAt == null && m.StoryPoints.HasValue
                            && !IsExcluded(m.FinalStatus, excludedStatuses)
                            && m.AddedAt.Date == day.Date)
                .Sum(m => m.StoryPoints!.Value);

            // Scope removals on this day
            var removedToday = memberships
                .Where(m => m.RemovedAt.HasValue && m.StoryPoints.HasValue
                            && m.RemovedAt.Value.Date == day.Date)
                .Sum(m => m.StoryPoints!.Value);

            cumulativeTotalScope += addedToday - removedToday;

            // Completions on this day (first done transition on this day)
            var completedToday = memberships
                .Where(m => m.RemovedAt == null && m.StoryPoints.HasValue
                            && !IsExcluded(m.FinalStatus, excludedStatuses)
                            && doneTransitionByTicket.TryGetValue(m.TicketId, out var ts)
                            && ts.Date == day.Date)
                .Sum(m => m.StoryPoints!.Value);

            cumulativeCompleted += completedToday;

            var phase = dayNumber <= 2 ? "planning" : "execution";

            result.Add(new BurnupDataPoint(
                dayNumber,
                day,
                Math.Round(cumulativeTotalScope, 1),
                Math.Round(cumulativeCompleted, 1),
                phase));
        }

        return result;
    }

    // --- Bug time-in-progress ---

    private static List<BugTimeInProgress> ComputeBugTimeInProgress(
        List<SprintMembership> memberships,
        List<StatusTransition> statusTransitions,
        Sprint sprint,
        List<string> workflowStages)
    {
        var midSprintBugs = memberships
            .Where(m => !m.WasCommitted && m.RemovedAt == null && m.Ticket?.IssueType == "Bug")
            .ToList();

        if (midSprintBugs.Count == 0)
            return [];

        // Determine active statuses from workflow stages
        var activeStatuses = GetActiveStatuses(workflowStages);

        var result = new List<BugTimeInProgress>();

        foreach (var m in midSprintBugs)
        {
            var ticketTransitions = statusTransitions
                .Where(t => t.TicketId == m.TicketId)
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

        // Active statuses are those between the first and last in the workflow
        // (excluding start and end boundary statuses)
        if (workflowStages.Count <= 2)
            return new HashSet<string>(workflowStages, StringComparer.OrdinalIgnoreCase);

        // Middle statuses (between first and last) are active work stages
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

            // Find next transition out of active status
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

    // --- Metric card builder ---

    private static ScopeMetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity)
    {
        string? direction = null;
        string? deltaPolarity = null;

        if (delta.HasValue)
        {
            direction = delta.Value > 0 ? "up" : delta.Value < 0 ? "down" : "flat";
            deltaPolarity = polarity switch
            {
                "positive-up" => delta.Value > 0 ? "positive" : delta.Value < 0 ? "negative" : "neutral",
                "positive-down" => delta.Value < 0 ? "positive" : delta.Value > 0 ? "negative" : "neutral",
                _ => "neutral"
            };
        }

        return new ScopeMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }
}
