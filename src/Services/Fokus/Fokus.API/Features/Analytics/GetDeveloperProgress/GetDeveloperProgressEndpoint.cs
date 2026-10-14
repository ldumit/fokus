namespace Fokus.API.Features.Analytics.GetDeveloperProgress;

[HttpGet("/api/analytics/developer-progress")]
[Tags("Analytics")]
public class GetDeveloperProgressEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    DeveloperProgressCalculator developerProgressCalculator,
    TimeProvider timeProvider)
    : Endpoint<GetDeveloperProgressRequest, GetDeveloperProgressResponse>
{
    public override async Task HandleAsync(GetDeveloperProgressRequest req, CancellationToken ct)
    {
        // 1. Load analytics sprints (active + closed)
        var allSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);

        // 2. Find the active sprint
        var activeSprint = allSprints.FirstOrDefault(s => s.State == SprintState.Active);

        // 3. No active sprint — return early with empty response
        if (activeSprint is null)
        {
            await Send.OkAsync(new GetDeveloperProgressResponse(false, null, 0, 0, false, [], []), ct);
            return;
        }

        // 4. Load the active sprint with memberships
        var sprintWithMemberships = await sprintRepository.GetSprintWithMembershipsAsync(activeSprint.Id, ct);
        if (sprintWithMemberships is null)
        {
            await Send.OkAsync(new GetDeveloperProgressResponse(false, null, 0, 0, false, [], []), ct);
            return;
        }

        // 5. Bulk load supporting data
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync([activeSprint.Id], ct);
        var settings = await appSettingsRepository.GetAsync(ct);
        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync([activeSprint.Id], ct);
        var transitionIndex = StatusTransitionIndex.Build(statusTransitions);

        // 6. Compute excluded developer IDs
        var excludedDeveloperIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
            sprintWithMemberships, allDevelopers, capacityRecords, transitionIndex, settings);

        // 7. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Compute and return
        var result = developerProgressCalculator.ComputeProgress(
            sprintWithMemberships,
            activeDevelopers,
            capacityRecords,
            settings,
            subTeam,
            allDevelopers,
            statusTransitions,
            excludedDeveloperIds,
            timeProvider.GetUtcNow().UtcDateTime.Date);

        await Send.OkAsync(result.ToGetDeveloperProgressResponse(), ct);
    }
}
