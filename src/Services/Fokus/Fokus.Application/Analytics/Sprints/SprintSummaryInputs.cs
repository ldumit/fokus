namespace Fokus.Application.Analytics.Sprints;

public sealed record SprintSummarySource(
    AppSettings Settings,
    Sprint SelectedSprint,
    List<Sprint> WindowSprints,
    List<Developer> ActiveDevelopers,
    List<Developer> AllDevelopers,
    List<DeveloperSprintCapacity> CapacityRecords,
    Dictionary<string, Dictionary<int, int>> CapacityLookup,
    List<StatusTransition> StatusTransitions,
    List<Ticket> AllEpicTickets,
    List<TestExecution>? SprintTes,
    IReadOnlyDictionary<string, int> ZombieSprintCounts);

public class SprintSummaryInputs(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    TestExecutionRepository testExecutionRepository)
{
    private const int ZombieMinSprintCount = 3;

    public async Task<SprintSummarySource?> LoadAsync(int? sprintId, CancellationToken ct)
    {
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // An explicit sprint id must exist, even when there are no sprints at all
        var requestedSprint = sprintId.HasValue
            ? sprints.FirstOrDefault(s => s.Id == sprintId.Value)
                .OrThrowNotFound($"Sprint {sprintId.Value} was not found.")
            : null;

        if (sprints.Count == 0)
            return null;

        var selectedSprintInfo = requestedSprint ?? sprints[0]; // most recent (sorted descending)
        var windowIds = SelectWindowIds(sprints, selectedSprintInfo);

        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var settings = await appSettingsRepository.GetAsync(ct);

        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(windowIds, ct);
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(windowIds, ct);
        var selectedSprint = windowSprints.First(s => s.Id == selectedSprintInfo.Id);

        // All epic tickets, for F8/F14 alignment (BR20)
        var allEpicTickets = await ticketRepository.GetTicketsWithEpicAsync(ct);

        // QA data only when Xray is enabled (BR19)
        var sprintTes = settings.XrayEnabled
            ? await testExecutionRepository.GetTestExecutionsForSprintAsync(selectedSprint.Id, ct)
            : null;

        var zombieSprintCounts = await sprintRepository.GetZombieSprintCountsAsync(ZombieMinSprintCount, ct);

        return new SprintSummarySource(
            settings,
            selectedSprint,
            windowSprints,
            activeDevelopers,
            allDevelopers,
            capacityRecords,
            capacityLookup,
            statusTransitions,
            allEpicTickets,
            sprintTes,
            zombieSprintCounts);
    }

    // Sparkline window: up to 4 closed sprints ending at selected, from the ascending list.
    // Sparklines use closed sprints only — active sprint is not a data point
    private static List<int> SelectWindowIds(List<Sprint> sprints, Sprint selectedSprintInfo)
    {
        var ascending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == selectedSprintInfo.Id);
        // If selected is active (not in closed list), use last closed sprint as anchor for sparkline
        var sparklineAnchorIndex = selectedIndex >= 0 ? selectedIndex : ascending.Count - 1;
        var windowIds = ascending
            .Take(sparklineAnchorIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

        // Always include the selected sprint even when it's active (not in the closed list)
        if (!windowIds.Contains(selectedSprintInfo.Id))
            windowIds.Add(selectedSprintInfo.Id);

        return windowIds;
    }
}
