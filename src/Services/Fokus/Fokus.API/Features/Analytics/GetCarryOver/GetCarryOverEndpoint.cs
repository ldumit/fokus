namespace Fokus.API.Features.Analytics.GetCarryOver;

[AllowAnonymous]
[HttpGet("/api/analytics/carry-over")]
[Tags("Analytics")]
public class GetCarryOverEndpoint(
    SprintRepository sprintRepository,
    AppSettingsRepository appSettingsRepository,
    CarryOverService carryOverService)
    : Endpoint<GetCarryOverRequest, CarryOverResponse>
{
    public override async Task HandleAsync(GetCarryOverRequest req, CancellationToken ct)
    {
        // 1. Load all closed sprints (lightweight, ordered ascending)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new CarryOverResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 5. Load ALL closed sprints with memberships — needed for accurate zombie sprint counting (BR7)
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var allSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        // 6. Determine mode
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

            // 7b. Identify prior sprint (next-earlier by start date)
            var targetIndex = allSprints.FindIndex(s => s.Id == target.Id);
            var priorSprint = targetIndex > 0 ? allSprints[targetIndex - 1] : null;

            // 8b. Compute single-sprint response
            var singleResult = carryOverService.ComputeSingleSprint(
                target, priorSprint, allSprints, settings, subTeam);

            await SendOkAsync(new CarryOverResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode
            // last=0 means "all sprints"; null defaults to last 5
            var last = req.Last ?? 5;

            // 7a. Take last N sprints (0 = all)
            var selectedSprints = last == 0 ? allSprints : allSprints.TakeLast(last).ToList();

            // 8a. Compute multi-sprint response
            var multiResult = carryOverService.ComputeMultiSprint(
                selectedSprints, allSprints, settings, subTeam);

            await SendOkAsync(new CarryOverResponse("multi", multiResult, null), ct);
        }
    }
}
