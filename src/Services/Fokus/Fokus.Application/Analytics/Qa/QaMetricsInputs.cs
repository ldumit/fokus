namespace Fokus.Application.Analytics.Qa;

public sealed record QaMetricsSource(
    AppSettings Settings,
    Sprint SelectedSprint,
    List<TestExecution> SprintTes,
    List<StatusTransition> StatusTransitions,
    Sprint? PriorSprint,
    List<TestExecution>? PriorTes,
    List<SprintMembership>? PriorMemberships,
    List<Sprint> SparklineWindow,
    Dictionary<int, List<TestExecution>> SparklineTesBySprintId,
    Dictionary<int, List<SprintMembership>> SparklineMembershipsBySprintId);

public class QaMetricsInputs(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository)
{
    public async Task<QaMetricsSource?> LoadAsync(int sprintId, CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        var sprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        sprints.FirstOrDefault(s => s.Id == sprintId)
            .OrThrowNotFound($"Sprint {sprintId} was not found.");

        if (!settings.XrayEnabled)
            return null;

        // Sparklines use closed sprints only — active sprint is not a data point
        var closedAscending = sprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        var closedIndex = closedAscending.FindIndex(s => s.Id == sprintId);
        var windowIds = SelectWindowIds(closedAscending, closedIndex);

        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);

        // Active sprint is not in the closed window — load it separately
        var selectedSprint = closedIndex >= 0
            ? windowSprints.First(s => s.Id == sprintId)
            : (await sprintRepository.GetSprintsWithMembershipsAsync([sprintId], ct))[0];

        // Active sprint ticket transitions are loaded with the window's
        var transitionIds = closedIndex >= 0 ? windowIds : [.. windowIds, sprintId];
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(transitionIds, ct);

        var sprintTes = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);

        var sortedWindow = windowSprints.OrderBy(s => s.StartDate).ToList();
        var selectedWindowIndex = sortedWindow.FindIndex(s => s.Id == sprintId);
        // Active sprint is not in sortedWindow — prior = last closed sprint in window
        var priorSprint = selectedWindowIndex > 0
            ? sortedWindow[selectedWindowIndex - 1]
            : (closedIndex < 0 && sortedWindow.Count > 0 ? sortedWindow[^1] : null);

        var priorTes = priorSprint is not null
            ? await testExecutionRepository.GetTestExecutionsForSprintAsync(priorSprint.Id, ct)
            : null;

        // For an active sprint selectedWindowIndex == -1, so Take(-1 + 1) = Take(0) would be empty.
        // Use the full sortedWindow (all closed window sprints) as the sparkline base instead.
        var sparklineWindow = closedIndex >= 0
            ? sortedWindow.Take(selectedWindowIndex + 1).TakeLast(4).ToList()
            : sortedWindow.TakeLast(4).ToList();

        var sparklineTesBySprintId = await LoadTesBySprintIdAsync(sparklineWindow, ct);

        return new QaMetricsSource(
            settings,
            selectedSprint,
            sprintTes,
            statusTransitions,
            priorSprint,
            priorTes,
            priorSprint?.Memberships.ToList(),
            sparklineWindow,
            sparklineTesBySprintId,
            sparklineWindow.ToDictionary(s => s.Id, s => s.Memberships.ToList()));
    }

    // Up to 4 closed sprints ending at the selected one (ascending);
    // if selected is active (not in closed list), anchor at the last closed sprint
    private static List<int> SelectWindowIds(List<Sprint> closedAscending, int closedIndex)
    {
        var sparklineAnchorIndex = closedIndex >= 0 ? closedIndex : closedAscending.Count - 1;
        return closedAscending
            .Take(sparklineAnchorIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();
    }

    private async Task<Dictionary<int, List<TestExecution>>> LoadTesBySprintIdAsync(List<Sprint> sprints, CancellationToken ct)
    {
        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sprints)
        {
            tesBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        return tesBySprintId;
    }
}
