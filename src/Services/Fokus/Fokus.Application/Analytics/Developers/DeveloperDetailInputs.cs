namespace Fokus.Application.Analytics.Developers;

public sealed record DeveloperDetailSource(
    Developer Developer,
    List<Sprint> AnalyticsSprints,
    AppSettings Settings,
    List<Sprint> ClosedSprints,
    Dictionary<string, Dictionary<int, int>> CapacityLookup,
    List<StatusTransition> StatusTransitions);

public sealed record DeveloperActiveSprintSource(
    Sprint? ActiveSprint,
    List<DeveloperSprintCapacity> CapacityRecords,
    List<StatusTransition> StatusTransitions);

public class DeveloperDetailInputs(
    SprintRepository sprintRepository,
    DeveloperRepository developerRepository,
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository)
{
    public async Task<DeveloperDetailSource> LoadAsync(string accountId, CancellationToken ct)
    {
        var allDevelopers = await developerRepository.GetAllAsync(ct);
        var developer = allDevelopers.FirstOrDefault(d => d.Id == accountId);
        if (developer is null || !developer.IsActive)
            throw new NotFoundException($"Developer {accountId} was not found or is inactive.");

        var analyticsSprints = await sprintRepository.GetAnalyticsSprintsAsync(ct);
        var closedSprintInfos = analyticsSprints
            .Where(s => s.State == SprintState.Closed)
            .OrderBy(s => s.StartDate)
            .ToList();

        var settings = await appSettingsRepository.GetAsync(ct);

        var allClosedIds = closedSprintInfos.Select(s => s.Id).ToList();
        var allLoadedSprints = await sprintRepository.GetSprintsWithMembershipsAsync(allClosedIds, ct);
        var closedSprints = allLoadedSprints.OrderBy(s => s.StartDate).ToList();

        var capacityRecords = await developerRepository.GetCapacitiesForSprintsAsync(allClosedIds, ct);
        var capacityLookup = capacityRecords
            .GroupBy(c => c.DeveloperAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(c => c.SprintId, c => c.CapacityPercent));

        var statusTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(allClosedIds, ct);

        return new DeveloperDetailSource(developer, analyticsSprints, settings, closedSprints, capacityLookup, statusTransitions);
    }

    public async Task<DeveloperActiveSprintSource> LoadActiveSprintAsync(List<Sprint> analyticsSprints, CancellationToken ct)
    {
        var activeSprintInfo = analyticsSprints.FirstOrDefault(s => s.State == SprintState.Active);
        if (activeSprintInfo is null)
            return new DeveloperActiveSprintSource(null, [], []);

        var activeSprint = await sprintRepository.GetSprintWithMembershipsAsync(activeSprintInfo.Id, ct);
        if (activeSprint is null)
            return new DeveloperActiveSprintSource(null, [], []);

        var activeCapacityRecords = await developerRepository.GetCapacitiesForSprintsAsync([activeSprintInfo.Id], ct);
        var activeTransitions = await ticketRepository.GetStatusTransitionsForSprintTicketsAsync([activeSprintInfo.Id], ct);

        return new DeveloperActiveSprintSource(activeSprint, activeCapacityRecords, activeTransitions);
    }
}
