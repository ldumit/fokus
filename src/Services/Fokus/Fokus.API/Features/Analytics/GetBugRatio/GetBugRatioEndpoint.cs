namespace Fokus.API.Features.Analytics.GetBugRatio;

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

        // 6. Load active developers and all developers for capacity fallback
        var activeDevelopers = await developerRepository.GetActiveDevelopersAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);

        // 7. Load capacity records for all closed sprints
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 8. Determine mode
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

            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                target, allDevelopers, capacityRecords, settings.DoneStatuses);
            var filteredActive = activeDevelopers.Where(d => !excludedIds.Contains(d.Id)).ToList();

            var singleResult = bugRatioService.ComputeSingleSprint(
                target, priorSprint, allSprints, filteredActive, settings, subTeam, excludedIds);

            await SendOkAsync(new BugRatioResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode — default last to 5 when neither param provided
            var last = req.Last ?? 5;
            var selectedSprints = allSprints.TakeLast(last).ToList();

            // For multi-sprint: exclude developers excluded in ALL selected sprints
            var excludedInAll = activeDevelopers
                .Where(dev => selectedSprints.All(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, settings.DoneStatuses)
                        .Contains(dev.Id)))
                .Select(dev => dev.Id)
                .ToHashSet();
            var filteredActive = activeDevelopers.Where(d => !excludedInAll.Contains(d.Id)).ToList();

            var multiResult = bugRatioService.ComputeMultiSprint(
                selectedSprints, allSprints, filteredActive, settings, subTeam, excludedInAll);

            await SendOkAsync(new BugRatioResponse("multi", multiResult, null), ct);
        }
    }
}
