namespace Fokus.Application.Analytics.Qa;

public sealed record QaTrendsSource(
    AppSettings Settings,
    List<Sprint> TargetSprints,
    Dictionary<int, List<TestExecution>> TesBySprintId,
    Dictionary<int, List<SprintMembership>> MembershipsBySprintId,
    List<StatusTransition> StatusTransitions,
    List<Developer> ActiveDevelopers,
    List<Sprint> BugRatioTargetSprints,
    List<Sprint> AllClosedSprints,
    List<StatusTransition> BugRatioTransitions);

public class QaTrendsInputs(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    TestExecutionRepository testExecutionRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository)
{
    private sealed record BugRatioInputs(
        List<Sprint> TargetSprints,
        List<Sprint> AllClosedSprints,
        List<StatusTransition> Transitions);

    public async Task<QaTrendsSource?> LoadAsync(int? last, CancellationToken ct)
    {
        var settings = await appSettingsRepository.GetAsync(ct);
        if (!settings.XrayEnabled)
            return null;

        // QA trends use closed sprints only for aggregates and N+1 correlation
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = allSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();
        if (ascending.Count < 2)
            return null;

        var allSprintIds = ascending.Select(s => s.Id).ToList();
        var qaSyncedIds = await testExecutionRepository.GetSprintIdsWithQaDataAsync(allSprintIds, ct);
        var qaSyncedAscending = ascending.Where(s => qaSyncedIds.Contains(s.Id)).ToList();
        if (qaSyncedAscending.Count < 2)
            return null;

        // Last N QA-synced sprints: 0 or null means all, fewer than 2 is treated as 2
        var targetQaSprints = last is null or 0
            ? qaSyncedAscending
            : qaSyncedAscending.TakeLast(Math.Max(last.Value, 2)).ToList();

        var targetIds = targetQaSprints.Select(s => s.Id).ToList();
        var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);
        var sortedLoaded = loadedSprints.OrderBy(s => s.StartDate).ToList();
        var membershipsBySprintId = sortedLoaded.ToDictionary(s => s.Id, s => s.Memberships.ToList());

        var tesBySprintId = new Dictionary<int, List<TestExecution>>();
        foreach (var sprintId in targetIds)
        {
            tesBySprintId[sprintId] = await testExecutionRepository.GetTestExecutionsForSprintAsync(sprintId, ct);
        }

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(targetIds, ct);
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);

        var bugRatioInputs = await LoadBugRatioInputsAsync(ascending, targetQaSprints, statusTransitions, ct);

        return new QaTrendsSource(
            settings,
            sortedLoaded,
            tesBySprintId,
            membershipsBySprintId,
            statusTransitions,
            activeDevelopers,
            bugRatioInputs.TargetSprints,
            bugRatioInputs.AllClosedSprints,
            bugRatioInputs.Transitions);
    }

    // The target sprints plus one sprint beyond the range, for the next-sprint bug ratio
    private async Task<BugRatioInputs> LoadBugRatioInputsAsync(
        List<Sprint> ascending, List<Sprint> targetQaSprints, List<StatusTransition> targetTransitions, CancellationToken ct)
    {
        var extendedIds = targetQaSprints.Select(s => s.Id).ToList();
        var lastTargetIndex = ascending.FindIndex(s => s.Id == targetQaSprints.Last().Id);
        Sprint? nextSprint = null;
        if (lastTargetIndex >= 0 && lastTargetIndex + 1 < ascending.Count)
        {
            nextSprint = ascending[lastTargetIndex + 1];
            extendedIds.Add(nextSprint.Id);
        }

        // All closed sprints with memberships — the bug ratio's alert evaluation needs all
        var allSprintIds = ascending.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allSprintIds, ct);
        var allSortedSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        var bugRatioTransitions = nextSprint is not null
            ? await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(extendedIds, ct)
            : targetTransitions;

        var extendedTargetSprints = allSortedSprints
            .Where(s => extendedIds.Contains(s.Id))
            .OrderBy(s => s.StartDate)
            .ToList();

        return new BugRatioInputs(extendedTargetSprints, allSortedSprints, bugRatioTransitions);
    }
}
