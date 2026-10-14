namespace Fokus.API.Features.Analytics.GetLeaderboard;

[HttpGet("/api/analytics/leaderboard")]
[Tags("Analytics")]
public class GetLeaderboardEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    TicketRepository ticketRepository,
    LeaderboardCalculator leaderboardCalculator)
    : Endpoint<GetLeaderboardRequest, LeaderboardResponse>
{
    public override async Task HandleAsync(GetLeaderboardRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints — alert baseline uses closed sprints only; multi-sprint averaging is closed-only
        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var ascending = analyticsSprints.Where(s => s.State == SprintState.Closed).OrderBy(s => s.StartDate).ToList();

        // An explicit sprint id must exist, even when there are no closed sprints
        var requestedSprint = req.SprintId.HasValue
            ? analyticsSprints.FirstOrDefault(s => s.Id == req.SprintId.Value)
                .OrThrowNotFound($"Sprint {req.SprintId.Value} was not found.")
            : null;

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await Send.OkAsync(new LeaderboardResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 5. Load ALL closed sprints with memberships (alert baseline)
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 6. Load active developers and all developers for capacity fallback
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        // 7. Load capacity records for all closed sprints
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 8. Build capacity lookup: accountId -> sprintId -> capacityPercent
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        // 9. Load status transitions for all closed sprint tickets
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        // 10. Determine mode
        if (requestedSprint is not null)
        {
            // Single-sprint mode — accept active or closed sprint.
            // Only closed sprints are loaded with memberships; an active target has none here.
            var target = requestedSprint.State == SprintState.Closed
                ? allSprints.First(s => s.Id == requestedSprint.Id)
                : requestedSprint;
            // Prior sprint is the last closed sprint before the target
            var targetIndex = allSprints.FindIndex(s => s.Id == target.Id);
            var priorSprint = targetIndex > 0
                ? allSprints[targetIndex - 1]
                : (target.State == SprintState.Active && allSprints.Count > 0 ? allSprints[^1] : null);

            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                target, allDevelopers, capacityRecords, transitionIndex, settings);
            var filteredActive = activeDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

            var singleResult = leaderboardCalculator.ComputeSingleSprint(
                target, priorSprint, filteredActive, settings, statusTransitions, subTeam, capacityLookup, allDevelopers, excludedIds);

            await Send.OkAsync(new LeaderboardResponse("single", null, singleResult.ToLeaderboardSingleSprintDto()), ct);
        }
        else
        {
            // Multi-sprint mode — default last to 5 when neither param provided
            var last = req.Last ?? 5;
            var selectedSprints = allSprints.TakeLast(last).ToList();

            // For multi-sprint: exclude developers excluded in ALL selected sprints
            var excludedInAll = activeDevelopers
                .Where(dev => selectedSprints.All(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, transitionIndex, settings)
                        .Contains(dev.Id)))
                .Select(dev => dev.Id)
                .ToHashSet();
            var filteredActive = activeDevelopers.Where(d => !excludedInAll.Contains(d.Id)).ToList();

            var multiResult = leaderboardCalculator.ComputeMultiSprint(
                selectedSprints, filteredActive, settings, statusTransitions, subTeam, capacityLookup, allDevelopers, excludedInAll);

            await Send.OkAsync(new LeaderboardResponse("multi", multiResult.ToLeaderboardMultiSprintDto(), null), ct);
        }
    }
}
