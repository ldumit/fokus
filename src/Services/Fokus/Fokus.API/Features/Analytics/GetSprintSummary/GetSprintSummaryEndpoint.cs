namespace Fokus.API.Features.Analytics.GetSprintSummary;

[AllowAnonymous]
[HttpGet("/api/analytics/sprint-summary")]
[Tags("Analytics")]
public class GetSprintSummaryEndpoint(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    SprintSummaryService sprintSummaryService)
    : Endpoint<GetSprintSummaryRequest, SprintSummaryResponse>
{
    public override async Task HandleAsync(GetSprintSummaryRequest req, CancellationToken ct)
    {
        // 1. Load all closed sprints (lightweight, no memberships)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);

        // 2. No closed sprints — return empty response
        if (closedSprints.Count == 0)
        {
            await SendOkAsync(new SprintSummaryResponse(null, null, null, [], [], new FlagsResult([], null, [], false)), ct);
            return;
        }

        // 3. Determine selected sprint
        Sprint selectedSprintInfo;
        if (req.SprintId.HasValue)
        {
            var match = closedSprints.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found or is not a closed sprint.");
                await SendErrorsAsync(400, ct);
                return;
            }
            selectedSprintInfo = match;
        }
        else
        {
            selectedSprintInfo = closedSprints[0]; // most recent (sorted descending)
        }

        // 4. Build sparkline window: up to 4 sprints ending at selected, from ascending list
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();
        var selectedIndex = ascending.FindIndex(s => s.Id == selectedSprintInfo.Id);
        var windowIds = ascending
            .Take(selectedIndex + 1)
            .TakeLast(4)
            .Select(s => s.Id)
            .ToList();

        // 5. Bulk load sprints with memberships for the window
        var windowSprints = await sprintRepository.GetSprintsWithMembershipsAsync(windowIds, ct);

        // 6. Load active developers and app settings
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var settings = await appSettingsRepository.GetAsync(ct);

        // 7. Normalize sub-team (empty string -> null)
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 8. Compute summary
        var selectedSprint = windowSprints.First(s => s.Id == selectedSprintInfo.Id);
        var result = sprintSummaryService.ComputeSummary(selectedSprint, windowSprints, activeDevelopers, settings, subTeam);

        await SendOkAsync(result, ct);
    }
}
