namespace Fokus.Application.Analytics.Qa;

public sealed record QaWorkloadScope(
    AppSettings Settings,
    Sprint? RequestedSprint,
    List<Sprint> ClosedAscending,
    List<Developer> AllDevelopers);

public sealed record QaWorkloadMultiSprintSource(
    List<Sprint> TargetSprints,
    Dictionary<int, List<TestExecution>> TesBySprintId,
    List<Sprint> AllClosedSprints,
    Dictionary<int, List<TestExecution>> AllClosedTesBySprintId);

public sealed record QaWorkloadSingleSprintSource(
    Sprint TargetSprint,
    List<TestExecution> TargetTes,
    Sprint? PriorSprint,
    List<TestExecution>? PriorTes,
    List<Sprint> SparklineWindow,
    Dictionary<int, List<TestExecution>> SparklineTesBySprintId,
    List<Sprint> AllClosedSprints,
    Dictionary<int, List<TestExecution>> AllClosedTesBySprintId);

public class QaWorkloadInputs(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TestExecutionRepository testExecutionRepository)
{
    public async Task<QaWorkloadScope?> LoadScopeAsync(int? sprintId, CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // An explicit sprint id must exist (active or closed), even when Xray is disabled or there are no closed sprints
        var requestedSprint = sprintId.HasValue
            ? allSprints.FirstOrDefault(s => s.Id == sprintId.Value)
                .OrThrowNotFound($"Sprint {sprintId.Value} was not found.")
            : null;

        if (!settings.XrayEnabled)
            return null;

        // Workload alert baseline and sparkline use closed sprints only
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        if (ascending.Count == 0)
            return null;

        // All developers, not only active ones — BR20: exclusion filter not applied
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        return new QaWorkloadScope(settings, requestedSprint, ascending, allDevelopers);
    }

    public async Task<QaWorkloadMultiSprintSource?> LoadMultiSprintAsync(QaWorkloadScope scope, int last, CancellationToken ct)
    {
        var candidateIds = scope.ClosedAscending.TakeLast(last).Select(s => s.Id).ToList();

        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct);
        var targetIds = candidateIds.Where(id => withQaData.Contains(id)).ToList();
        if (targetIds.Count == 0)
            return null;

        var targetSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);

        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in targetIds)
        {
            tesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        // Workload alert: all closed sprints and their TEs
        var allClosedIds = scope.ClosedAscending.Select(s => s.Id).ToList();
        var allClosedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allClosedTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in allClosedIds)
        {
            allClosedTesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        return new QaWorkloadMultiSprintSource(targetSprints, tesBySprintId, allClosedSprints, allClosedTesBySprintId);
    }

    public async Task<QaWorkloadSingleSprintSource?> LoadSingleSprintAsync(QaWorkloadScope scope, Sprint targetSprintInfo, CancellationToken ct)
    {
        var ascending = scope.ClosedAscending;
        var targetLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([targetSprintInfo.Id], ct);
        var targetSprint = targetLoaded[0];

        var targetTes = await testExecutionRepository.GetTestExecutionsForSprintAsync(targetSprint.Id, ct);

        var withQaData = await testExecutionRepository.GetSprintIdsWithQaDataAsync([targetSprint.Id], ct);
        if (!withQaData.Contains(targetSprint.Id))
            return null;

        // targetIndex is -1 when the selected sprint is active (not in closed ascending list)
        var targetIndex = ascending.FindIndex(s => s.Id == targetSprint.Id);
        var (priorSprint, priorTes) = await LoadPriorSprintAsync(ascending, targetIndex, ct);
        var (sparklineWindow, sparklineTesBySprintId) = await LoadSparklineWindowAsync(ascending, targetIndex, ct);

        // Workload alert: all closed sprints and their TEs
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allClosedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allClosedTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in allClosedIds)
        {
            if (sparklineTesBySprintId.TryGetValue(sprintId, out var existing))
                allClosedTesBySprintId[sprintId] = existing;
            else
                allClosedTesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        return new QaWorkloadSingleSprintSource(
            targetSprint,
            targetTes,
            priorSprint,
            priorTes,
            sparklineWindow,
            sparklineTesBySprintId,
            allClosedSprints.OrderBy(s => s.StartDate).ToList(),
            allClosedTesBySprintId);
    }

    // Prior sprint: last closed sprint before target (or last closed sprint when active), only when it has QA data
    private async Task<(Sprint? Sprint, List<TestExecution>? Tes)> LoadPriorSprintAsync(
        List<Sprint> ascending, int targetIndex, CancellationToken ct)
    {
        var priorIndex = targetIndex > 0 ? targetIndex - 1
            : targetIndex < 0 && ascending.Count > 0 ? ascending.Count - 1
            : -1;
        if (priorIndex < 0)
            return (null, null);

        var priorInfo = ascending[priorIndex];
        var priorWithQa = await testExecutionRepository.GetSprintIdsWithQaDataAsync([priorInfo.Id], ct);
        if (!priorWithQa.Contains(priorInfo.Id))
            return (null, null);

        var priorLoaded = await sprintRepository.GetSprintsWithMembershipsAsync([priorInfo.Id], ct);
        var priorTes = await testExecutionRepository.GetTestExecutionsForSprintAsync(priorInfo.Id, ct);
        return (priorLoaded[0], priorTes);
    }

    // Sparkline window: up to 4 trailing closed sprints with QA data.
    // When active (targetIndex == -1), anchor at last closed sprint so window is non-empty
    private async Task<(List<Sprint> Window, Dictionary<int, List<TestExecution>> TesBySprintId)> LoadSparklineWindowAsync(
        List<Sprint> ascending, int targetIndex, CancellationToken ct)
    {
        var sparklineAnchorIndex = targetIndex >= 0 ? targetIndex : ascending.Count - 1;
        var candidateIds = ascending.Take(sparklineAnchorIndex + 1).Select(s => s.Id).ToList();
        var qaFilteredIds = (await testExecutionRepository.GetSprintIdsWithQaDataAsync(candidateIds, ct))
            .OrderBy(id => ascending.FindIndex(s => s.Id == id))
            .ToList();
        var sparklineIds = qaFilteredIds.TakeLast(4).ToList();
        var sparklineSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sparklineIds, ct);
        var sparklineWindow = sparklineSprints.OrderBy(s => s.StartDate).ToList();

        var sparklineTesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprint in sparklineWindow)
        {
            sparklineTesBySprintId[sprint.Id] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprint.Id, ct);
        }

        return (sparklineWindow, sparklineTesBySprintId);
    }
}
