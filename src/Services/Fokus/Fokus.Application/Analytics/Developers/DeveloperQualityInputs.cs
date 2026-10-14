namespace Fokus.Application.Analytics.Developers;

public sealed record DeveloperQualitySource(
    AppSettings Settings,
    bool IsSingleSprint,
    List<Sprint> AllLoadedSprints,
    List<int> TargetSprintIds,
    List<Developer> ActiveDevelopers,
    Dictionary<int, List<TestExecution>> TesBySprintId,
    List<StatusTransition> StatusTransitions,
    Sprint? PriorSprint,
    List<TestExecution>? PriorSprintTes,
    List<Sprint> SparklineWindow,
    Dictionary<int, List<TestExecution>> SparklineTesBySprintId,
    List<Sprint> StreakWindow,
    Dictionary<int, List<TestExecution>> StreakTesBySprintId);

public class DeveloperQualityInputs(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    TestExecutionRepository testExecutionRepository)
{
    private sealed record SingleSprintWindows(
        Sprint? PriorSprint,
        List<TestExecution>? PriorSprintTes,
        List<Sprint> SparklineWindow,
        Dictionary<int, List<TestExecution>> SparklineTesBySprintId,
        List<Sprint> StreakWindow,
        Dictionary<int, List<TestExecution>> StreakTesBySprintId);

    public async Task<DeveloperQualitySource?> LoadAsync(int? sprintId, int? last, CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // An explicit sprint id must exist, even when Xray is disabled or there are no closed sprints
        var requestedSprint = sprintId.HasValue
            ? allSprints.FirstOrDefault(s => s.Id == sprintId.Value)
                .OrThrowNotFound($"Sprint {sprintId.Value} was not found.")
            : null;

        if (!settings.XrayEnabled)
            return null;

        // Multi-sprint averaging, sparkline, and streak use closed sprints only
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        if (ascending.Count == 0)
            return null;

        var isSingleSprint = sprintId.HasValue;
        var targetSprintIds = await ResolveTargetSprintIdsAsync(requestedSprint, last, ascending, ct);
        if (targetSprintIds.Count == 0)
            return null;

        var windows = isSingleSprint
            ? await LoadSingleSprintWindowsAsync(targetSprintIds[0], ascending, ct)
            : new SingleSprintWindows(null, null, [], [], [], []);

        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetSprintIds.ToList(), ct);
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(
            CollectTransitionSprintIds(targetSprintIds, windows), ct);

        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var targetSprintId in targetSprintIds)
        {
            tesBySprintId[targetSprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(targetSprintId, ct);
        }

        return new DeveloperQualitySource(
            settings,
            isSingleSprint,
            MergeLoadedSprints(loadedSprints, windows),
            targetSprintIds,
            activeDevelopers,
            new Dictionary<int, List<TestExecution>>(tesBySprintId),
            statusTransitions,
            windows.PriorSprint,
            windows.PriorSprintTes,
            windows.SparklineWindow,
            windows.SparklineTesBySprintId,
            windows.StreakWindow,
            windows.StreakTesBySprintId);
    }

    private async Task<List<int>> ResolveTargetSprintIdsAsync(
        Sprint? requestedSprint, int? last, List<Sprint> ascending, CancellationToken ct)
    {
        // Single sprint — accept active or closed
        if (requestedSprint is Sprint match)
            return [match.Id];

        // Multi-sprint: last N (or all) closed sprints, filtered to only those with QA data
        var candidateIds = last.HasValue
            ? ascending.TakeLast(last.Value).Select(s => s.Id).ToList()
            : ascending.Select(s => s.Id).ToList();
        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct);
        return candidateIds.Where(id => withQaData.Contains(id)).ToList();
    }

    private async Task<SingleSprintWindows> LoadSingleSprintWindowsAsync(
        int targetSprintId, List<Sprint> ascending, CancellationToken ct)
    {
        var targetIndex = ascending.FindIndex(s => s.Id == targetSprintId);
        // When active sprint is selected, it is not in the closed ascending list (targetIndex == -1).
        // Use ascending.Count as the anchor so all closed sprints are candidates for sparkline/streak.
        var closedAnchorIndex = targetIndex >= 0 ? targetIndex : ascending.Count - 1;

        var (priorSprint, priorSprintTes) = await LoadPriorSprintAsync(ascending, targetIndex, closedAnchorIndex, ct);

        var candidateIds = ascending.Take(closedAnchorIndex + 1).Select(s => s.Id).ToList();

        // Filter to sprints with QA data (BR8 semantics) — used for both sparkline and streak
        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct);
        var qaFilteredIds = candidateIds.Where(id => withQaData.Contains(id)).ToList();

        // Sparkline: up to 4 trailing sprints with QA data ending at target
        var sparklineIds = qaFilteredIds.TakeLast(4).ToList();
        var sparklineSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sparklineIds, ct);
        var sparklineWindow = sparklineSprints.OrderBy(s => s.StartDate).ToList();

        var sparklineTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sparklineWindow)
        {
            sparklineTesBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        // Streak window: up to 12 trailing sprints with QA data ending at target (BR15).
        // Capped at 12 to bound DB load while covering realistic streak lengths.
        var streakIds = qaFilteredIds.TakeLast(12).ToList();
        var streakSprints = await sprintRepository.GetSprintsWithMembershipsAsync(streakIds, ct);
        var streakWindow = streakSprints.OrderBy(s => s.StartDate).ToList();

        var streakTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in streakWindow)
        {
            if (sparklineTesBySprintId.TryGetValue(sprint.Id, out var existingTes))
                streakTesBySprintId[sprint.Id] = existingTes;
            else
                streakTesBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        return new SingleSprintWindows(
            priorSprint, priorSprintTes, sparklineWindow, sparklineTesBySprintId, streakWindow, streakTesBySprintId);
    }

    // Prior sprint: last closed sprint before target, provided only when it has QA data
    private async Task<(Sprint? Sprint, List<TestExecution>? Tes)> LoadPriorSprintAsync(
        List<Sprint> ascending, int targetIndex, int closedAnchorIndex, CancellationToken ct)
    {
        if (closedAnchorIndex <= 0 && (targetIndex >= 0 || ascending.Count == 0))
            return (null, null);

        var priorSprintInfo = targetIndex >= 0
            ? ascending[targetIndex - 1]
            : ascending[ascending.Count - 1];
        var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorSprintInfo.Id], ct);
        if (priorLoaded.Count == 0)
            return (null, null);

        var priorSprintTes = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorLoaded[0].Id, ct);
        return priorSprintTes.Count == 0 ? (null, null) : (priorLoaded[0], priorSprintTes);
    }

    // Transitions for target, sparkline, prior and streak sprints
    private static List<int> CollectTransitionSprintIds(List<int> targetSprintIds, SingleSprintWindows windows) =>
        targetSprintIds
            .Concat(windows.SparklineWindow.Select(s => s.Id))
            .Concat(windows.PriorSprint is not null ? [windows.PriorSprint.Id] : Array.Empty<int>())
            .Concat(windows.StreakWindow.Select(s => s.Id))
            .Distinct()
            .ToList();

    // Every sprint whose memberships the calculator may read: target, sparkline, prior and streak sprints
    private static List<Sprint> MergeLoadedSprints(List<Sprint> loadedSprints, SingleSprintWindows windows) =>
        loadedSprints
            .Concat(windows.SparklineWindow.Where(s => !loadedSprints.Any(ls => ls.Id == s.Id)))
            .Concat(windows.PriorSprint is not null && !loadedSprints.Any(ls => ls.Id == windows.PriorSprint.Id) ? [windows.PriorSprint] : [])
            .Concat(windows.StreakWindow.Where(s => !loadedSprints.Any(ls => ls.Id == s.Id)))
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .ToList();
}
