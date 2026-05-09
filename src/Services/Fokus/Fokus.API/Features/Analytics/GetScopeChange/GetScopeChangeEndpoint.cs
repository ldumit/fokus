namespace Fokus.API.Features.Analytics.GetScopeChange;

[HttpGet("/api/analytics/scope-change")]
[Tags("Analytics")]
public class GetScopeChangeEndpoint(
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    AppSettingsRepository appSettingsRepository,
    DeveloperRepository developerRepository,
    ScopeChangeService scopeChangeService)
    : Endpoint<GetScopeChangeRequest, ScopeChangeResponse>
{
    public override async Task HandleAsync(GetScopeChangeRequest req, CancellationToken ct)
    {
        // 1. Load all closed sprints (lightweight, ordered ascending)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);
        var ascending = closedSprints.OrderBy(s => s.StartDate).ToList();

        // 2. No closed sprints — return empty multi response
        if (ascending.Count == 0)
        {
            await SendOkAsync(new ScopeChangeResponse("multi", null, null), ct);
            return;
        }

        // 3. Normalize sub-team
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 4. Load app settings, all developers, and capacity records
        var settings = await appSettingsRepository.GetAsync(ct);
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var allClosedIds = ascending.Select(s => s.Id).ToList();
        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);

        // 5. Determine mode
        if (req.SprintId.HasValue)
        {
            // Single-sprint mode
            var match = ascending.FirstOrDefault(s => s.Id == req.SprintId.Value);
            if (match is null)
            {
                AddError(r => r.SprintId, "Sprint not found or is not a closed sprint.");
                await SendErrorsAsync(400, ct);
                return;
            }

            // 6b. Identify prior sprint (next-earlier by start date)
            var targetIndex = ascending.FindIndex(s => s.Id == match.Id);
            var priorSprintLightweight = targetIndex > 0 ? ascending[targetIndex - 1] : null;

            // 7b. Load target and prior sprints with memberships
            var sprintIdsToLoad = new List<int> { match.Id };
            if (priorSprintLightweight is not null)
                sprintIdsToLoad.Add(priorSprintLightweight.Id);

            var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(sprintIdsToLoad, ct);
            var targetSprint = loadedSprints.First(s => s.Id == match.Id);
            var priorSprint = priorSprintLightweight is not null
                ? loadedSprints.FirstOrDefault(s => s.Id == priorSprintLightweight.Id)
                : null;

            // 8b. Compute excluded developer IDs for the target sprint
            var excludedIds = ExcludedDeveloperFilter.GetExcludedDeveloperIds(
                targetSprint, allDevelopers, capacityRecords, settings.DoneStatuses);

            // 9b. Identify all non-removed ticket IDs (needed for burnup completion tracking)
            var allActiveTicketIds = targetSprint.Memberships
                .Where(m => m.RemovedAt == null)
                .Select(m => m.TicketId)
                .ToList();

            // 10b. Load status transitions for all active tickets.
            // BuildBurnupData uses these to place each ticket's completion on the correct day.
            // ComputeBugTimeInProgress filters internally to mid-sprint bugs only.
            var statusTransitions = allActiveTicketIds.Count > 0
                ? await ticketRepository.GetStatusTransitionsForTicketsAsync(allActiveTicketIds, ct)
                : new List<StatusTransition>();

            // 11b. Compute single-sprint response
            var singleResult = scopeChangeService.ComputeSingleSprint(
                targetSprint, priorSprint, statusTransitions, settings, subTeam, excludedIds);

            await SendOkAsync(new ScopeChangeResponse("single", null, singleResult), ct);
        }
        else
        {
            // Multi-sprint mode
            // last=0 means "all sprints"; null defaults to last 5
            var last = req.Last ?? 5;

            // 6a. Take last N sprints ascending (0 = all)
            var targetSprints = last == 0 ? ascending : ascending.TakeLast(last).ToList();
            var targetIds = targetSprints.Select(s => s.Id).ToList();

            // 7a. Bulk load sprints with memberships
            var loadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(targetIds, ct);

            // 8a. Compute excluded developer IDs across all target sprints
            var excludedInAll = loadedSprints
                .SelectMany(sprint =>
                    ExcludedDeveloperFilter.GetExcludedDeveloperIds(sprint, allDevelopers, capacityRecords, settings.DoneStatuses))
                .GroupBy(id => id)
                .Where(g => g.Count() == loadedSprints.Count)
                .Select(g => g.Key)
                .ToHashSet();

            // 9a. Compute multi-sprint response
            var multiResult = scopeChangeService.ComputeMultiSprint(loadedSprints, settings, subTeam, excludedInAll);

            await SendOkAsync(new ScopeChangeResponse("multi", multiResult, null), ct);
        }
    }
}
