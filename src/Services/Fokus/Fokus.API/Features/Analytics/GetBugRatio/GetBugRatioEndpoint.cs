namespace Fokus.API.Features.Analytics.GetBugRatio;

[AllowAnonymous]
[HttpGet("/api/analytics/bug-ratio")]
[Tags("Analytics")]
public class GetBugRatioEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    BugRatioService bugRatioService)
    : Endpoint<GetBugRatioRequest, BugRatioResponse>
{
    public override async Task HandleAsync(GetBugRatioRequest req, CancellationToken ct)
    {
        // 1. Load all closed sprints lightweight
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new BugRatioResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 5. Load ALL closed sprints with memberships (needed for alert evaluation — BR9)
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 6. Load active developers
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);

        // 7. Determine mode
        if (req.SprintId.HasValue)
        {
            // Single-sprint mode
            var target = allSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (target is null)
            {
                AddError(r => r.SprintId, "Sprint not found or is not a closed sprint.");
                await SendErrorsAsync(400, ct);
                return;
            }

            var targetIndex = allSprints.FindIndex(s => s.Id == target.Id);
            var priorSprint = targetIndex > 0 ? allSprints[targetIndex - 1] : null;

            var singleResult = bugRatioService.ComputeSingleSprint(
                target, priorSprint, allSprints, activeDevelopers, settings, subTeam);

            await SendOkAsync(new BugRatioResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode — default last to 5 when neither param provided
            var last = req.Last ?? 5;
            var selectedSprints = allSprints.TakeLast(last).ToList();

            var multiResult = bugRatioService.ComputeMultiSprint(
                selectedSprints, allSprints, activeDevelopers, settings, subTeam);

            await SendOkAsync(new BugRatioResponse("multi", multiResult, null), ct);
        }
    }
}
