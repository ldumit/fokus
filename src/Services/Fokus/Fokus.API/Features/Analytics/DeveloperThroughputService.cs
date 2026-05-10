namespace Fokus.API.Features.Analytics;

// --- Response record hierarchy ---

public record SprintSummaryItem(int Id, string Name, DateTime StartDate, DateTime EndDate);

public record SprintBreakdown(
    int SprintId,
    decimal SpAssigned,
    decimal SpCompleted,
    decimal CompletionPercent,
    int TicketsDone,
    int TicketsCarriedOver,
    int CapacityPercent,
    decimal? RollingAverageSpCompleted,
    decimal? SpAssignedDelta,
    decimal? SpCompletedDelta,
    decimal? CompletionPercentDelta,
    int? TicketsDoneDelta,
    int? TicketsCarriedOverDelta,
    string? SpAssignedDeltaDirection,
    string? SpCompletedDeltaDirection,
    string? CompletionPercentDeltaDirection,
    string? TicketsDoneDeltaDirection,
    string? TicketsCarriedOverDeltaDirection,
    string? SpAssignedDeltaPolarity,
    string? SpCompletedDeltaPolarity,
    string? CompletionPercentDeltaPolarity,
    string? TicketsDoneDeltaPolarity,
    string? TicketsCarriedOverDeltaPolarity);

public record DeveloperThroughputEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<SprintBreakdown> SprintBreakdowns);

public record DeveloperThroughputResponse(
    List<SprintSummaryItem> Sprints,
    List<DeveloperThroughputEntry> Developers);

// --- Service ---

public class DeveloperThroughputService
{
    public DeveloperThroughputResponse ComputeThroughput(
        List<Sprint> allLoadedSprints,
        List<int> targetSprintIds,
        List<Developer> activeDevelopers,
        List<DeveloperSprintCapacity> capacityRecords,
        AppSettings settings,
        string? subTeam,
        List<Developer> allDevelopers)
    {
        var doneStatuses = settings.DoneStatuses;
        var defaultSpPerBug = settings.DefaultSpPerBug;

        // C2: sub-team filtering (active developers only for display)
        var filteredDevelopers = FilterDevelopers(activeDevelopers, subTeam);

        // Sort all loaded sprints ascending by start date
        var sortedAllSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // Target sprints in ascending order
        var targetSprints = sortedAllSprints
            .Where(s => targetSprintIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        var isSingleSprint = targetSprintIds.Count == 1;

        // Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // Find prior sprint for delta (single-sprint mode only)
        Sprint? priorSprint = null;
        if (isSingleSprint)
        {
            var targetSprint = targetSprints[0];
            var targetIndexInAll = sortedAllSprints.FindIndex(s => s.Id == targetSprint.Id);
            priorSprint = targetIndexInAll > 0 ? sortedAllSprints[targetIndexInAll - 1] : null;
        }

        var sprintSummaryItems = targetSprints
            .Select(s => new SprintSummaryItem(s.Id, s.Name, s.StartDate, s.EndDate))
            .ToList();

        var developerEntries = filteredDevelopers.Select(developer =>
        {
            var breakdowns = targetSprints.Select(sprint =>
            {
                var memberships = GetDeveloperMemberships(sprint, developer.Id, subTeam);
                var capacity = GetCapacity(capacityLookup, developer.Id, sprint.Id, allDevelopers);

                var spAssigned = memberships
                    .Where(m => m.RemovedAt == null && !IsBug(m))
                    .Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                var spCompleted = memberships
                    .Where(m => m.RemovedAt == null && doneStatuses.Contains(m.FinalStatus) && !IsBug(m))
                    .Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                var completionPercent = spAssigned > 0 ? spCompleted / spAssigned * 100 : 0;

                var ticketsDone = memberships
                    .Count(m => m.RemovedAt == null && doneStatuses.Contains(m.FinalStatus) && !IsBug(m));

                var ticketsCarriedOver = memberships
                    .Count(m => m.RemovedAt == null && !doneStatuses.Contains(m.FinalStatus) && !IsBug(m));

                var rollingAverage = ComputeRollingAverage(
                    developer.Id, sprint.Id, sortedAllSprints, capacityLookup, doneStatuses, subTeam, allDevelopers, defaultSpPerBug);

                // Deltas (single-sprint only)
                decimal? spAssignedDelta = null;
                decimal? spCompletedDelta = null;
                decimal? completionPercentDelta = null;
                int? ticketsDoneDelta = null;
                int? ticketsCarriedOverDelta = null;
                string? spAssignedDeltaDirection = null;
                string? spCompletedDeltaDirection = null;
                string? completionPercentDeltaDirection = null;
                string? ticketsDoneDeltaDirection = null;
                string? ticketsCarriedOverDeltaDirection = null;
                string? spAssignedDeltaPolarity = null;
                string? spCompletedDeltaPolarity = null;
                string? completionPercentDeltaPolarity = null;
                string? ticketsDoneDeltaPolarity = null;
                string? ticketsCarriedOverDeltaPolarity = null;

                if (isSingleSprint && priorSprint is not null)
                {
                    var priorMemberships = GetDeveloperMemberships(priorSprint, developer.Id, subTeam);

                    var priorSpAssigned = priorMemberships
                        .Where(m => m.RemovedAt == null && !IsBug(m))
                        .Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                    var priorSpCompleted = priorMemberships
                        .Where(m => m.RemovedAt == null && doneStatuses.Contains(m.FinalStatus) && !IsBug(m))
                        .Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);

                    var priorCompletionPercent = priorSpAssigned > 0 ? priorSpCompleted / priorSpAssigned * 100 : 0;

                    var priorTicketsDone = priorMemberships
                        .Count(m => m.RemovedAt == null && doneStatuses.Contains(m.FinalStatus) && !IsBug(m));

                    var priorTicketsCarriedOver = priorMemberships
                        .Count(m => m.RemovedAt == null && !doneStatuses.Contains(m.FinalStatus) && !IsBug(m));

                    spAssignedDelta = spAssigned - priorSpAssigned;
                    spCompletedDelta = spCompleted - priorSpCompleted;
                    completionPercentDelta = completionPercent - priorCompletionPercent;
                    ticketsDoneDelta = ticketsDone - priorTicketsDone;
                    ticketsCarriedOverDelta = ticketsCarriedOver - priorTicketsCarriedOver;

                    spAssignedDeltaDirection = DeltaDirection(spAssignedDelta.Value);
                    spCompletedDeltaDirection = DeltaDirection(spCompletedDelta.Value);
                    completionPercentDeltaDirection = DeltaDirection(completionPercentDelta.Value);
                    ticketsDoneDeltaDirection = DeltaDirection(ticketsDoneDelta.Value);
                    ticketsCarriedOverDeltaDirection = DeltaDirection(ticketsCarriedOverDelta.Value);

                    spAssignedDeltaPolarity = "neutral";
                    spCompletedDeltaPolarity = DeltaPolarity(spCompletedDelta.Value, positiveUp: true);
                    completionPercentDeltaPolarity = DeltaPolarity(completionPercentDelta.Value, positiveUp: true);
                    ticketsDoneDeltaPolarity = DeltaPolarity(ticketsDoneDelta.Value, positiveUp: true);
                    ticketsCarriedOverDeltaPolarity = DeltaPolarity(ticketsCarriedOverDelta.Value, positiveUp: false);
                }

                return new SprintBreakdown(
                    sprint.Id,
                    Math.Round(spAssigned, 1),
                    Math.Round(spCompleted, 1),
                    Math.Round(completionPercent, 1),
                    ticketsDone,
                    ticketsCarriedOver,
                    capacity,
                    rollingAverage.HasValue ? Math.Round(rollingAverage.Value, 1) : null,
                    spAssignedDelta.HasValue ? Math.Round(spAssignedDelta.Value, 1) : null,
                    spCompletedDelta.HasValue ? Math.Round(spCompletedDelta.Value, 1) : null,
                    completionPercentDelta.HasValue ? Math.Round(completionPercentDelta.Value, 1) : null,
                    ticketsDoneDelta,
                    ticketsCarriedOverDelta,
                    spAssignedDeltaDirection,
                    spCompletedDeltaDirection,
                    completionPercentDeltaDirection,
                    ticketsDoneDeltaDirection,
                    ticketsCarriedOverDeltaDirection,
                    spAssignedDeltaPolarity,
                    spCompletedDeltaPolarity,
                    completionPercentDeltaPolarity,
                    ticketsDoneDeltaPolarity,
                    ticketsCarriedOverDeltaPolarity);
            }).ToList();

            return new DeveloperThroughputEntry(developer.Id, developer.DisplayName, developer.SubTeam, developer.AvatarUrl, breakdowns);
        }).ToList();

        return new DeveloperThroughputResponse(sprintSummaryItems, developerEntries);
    }

    // --- Bug classification (mirrors SprintSummaryService) ---

    private static bool IsBug(SprintMembership m) =>
        m.Ticket?.IssueType == "Bug";

    // --- Sub-team filtering (C2) ---

    private static List<Developer> FilterDevelopers(List<Developer> developers, string? subTeam)
    {
        if (string.IsNullOrWhiteSpace(subTeam))
            return developers;

        return developers.Where(d => d.SubTeam == subTeam).ToList();
    }

    private static List<SprintMembership> GetDeveloperMemberships(Sprint sprint, string developerId, string? subTeam)
    {
        return sprint.Memberships
            .Where(m => m.Ticket?.AssigneeId == developerId &&
                        (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
            .ToList();
    }

    private static int GetCapacity(
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        string developerId,
        int sprintId,
        List<Developer> developers)
    {
        if (capacityLookup.TryGetValue(developerId, out var sprintMap) &&
            sprintMap.TryGetValue(sprintId, out var percent))
        {
            return percent;
        }
        var developer = developers.FirstOrDefault(d => d.Id == developerId);
        return developer?.DefaultCapacityPercent ?? 100;
    }

    // --- Rolling average (capacity-aware, 3-sprint window) ---

    private static decimal? ComputeRollingAverage(
        string developerId,
        int currentSprintId,
        List<Sprint> sortedAllSprints,
        Dictionary<string, Dictionary<int, int>> capacityLookup,
        List<string> doneStatuses,
        string? subTeam,
        List<Developer> developers,
        int defaultSpPerBug = 0)
    {
        var currentIndex = sortedAllSprints.FindIndex(s => s.Id == currentSprintId);
        if (currentIndex < 0) return null;

        // Look backward from current sprint (inclusive) collecting up to 3 qualifying sprints
        var qualifyingSpCompleted = new List<decimal>();
        for (var i = currentIndex; i >= 0 && qualifyingSpCompleted.Count < 3; i--)
        {
            var sprint = sortedAllSprints[i];
            var capacity = GetCapacity(capacityLookup, developerId, sprint.Id, developers);
            if (capacity == 0) continue;

            var spCompleted = sprint.Memberships
                .Where(m => m.Ticket?.AssigneeId == developerId &&
                            m.RemovedAt == null &&
                            doneStatuses.Contains(m.FinalStatus) &&
                            m.Ticket?.IssueType != "Bug" &&
                            (string.IsNullOrWhiteSpace(subTeam) || m.Ticket?.Assignee?.SubTeam == subTeam))
                .Sum(m => m.GetEffectiveSp(defaultSpPerBug) ?? 0m);

            qualifyingSpCompleted.Add(spCompleted);
        }

        if (qualifyingSpCompleted.Count < 3) return null;

        return qualifyingSpCompleted.Average();
    }

    // --- Delta helpers ---

    private static string DeltaDirection(decimal delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaDirection(int delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    private static string DeltaPolarity(decimal delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    private static string DeltaPolarity(int delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");
}
