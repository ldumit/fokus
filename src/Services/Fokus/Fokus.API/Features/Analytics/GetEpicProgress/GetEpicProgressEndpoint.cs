namespace Fokus.API.Features.Analytics.GetEpicProgress;

[HttpGet("/api/analytics/epic-progress")]
[Tags("Analytics")]
public class GetEpicProgressEndpoint(
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    SprintRepository sprintRepository,
    TestExecutionRepository testExecutionRepository,
    EpicProgressService epicProgressService)
    : Endpoint<GetEpicProgressRequest, EpicProgressResponse>
{
    public override async Task HandleAsync(GetEpicProgressRequest req, CancellationToken ct)
    {
        // 1. Normalize subTeam (empty string to null)
        var subTeam = string.IsNullOrWhiteSpace(req.SubTeam) ? null : req.SubTeam;

        // 2. Load app settings
        var settings = await appSettingsRepository.GetAsync(ct);

        // 3. Load all tickets with epic key
        var epicTickets = await ticketRepository.GetTicketsWithEpicAsync(ct);

        // 4. If no epic tickets, return empty response
        if (epicTickets.Count == 0)
        {
            await SendOkAsync(new EpicProgressResponse(
                new EpicProgressSummaryMetrics(0, 0, 0m),
                [],
                new EpicProgressUnlinkedWork(0, 0m),
                HasQaData: false,
                AverageTestCoverage: null), ct);
            return;
        }

        // 5. Load all closed sprints (lightweight, for velocity transition date ranges)
        var closedSprints = await sprintRepository.GetClosedSprintsAsync(ct);
        var closedSprintIds = closedSprints.Select(s => s.Id).ToList();

        // 6. Load all closed sprint memberships (for velocity grouping by sprint)
        var closedMemberships = await sprintRepository.GetAllClosedSprintMembershipsAsync(ct);

        // 7. Load status transitions for all closed sprint tickets (for transition-based velocity)
        var statusTransitions = closedSprintIds.Count > 0
            ? await ticketRepository.GetStatusTransitionsForSprintTicketsAsync(closedSprintIds, ct)
            : new List<StatusTransition>();

        // 8. Load unlinked tickets (in at least one sprint, no epic key)
        var unlinkedTickets = await ticketRepository.GetTicketsWithoutEpicInSprintsAsync(ct);

        // 9. Load QA data when Xray is enabled (no extra DB round-trips when disabled)
        EpicQaData? qaData = null;
        if (settings.XrayEnabled)
        {
            var ticketKeys = epicTickets.Select(t => t.Id).ToList();
            var (testsLinks, blocksLinks, runsByTeId) =
                await testExecutionRepository.GetTestExecutionDataForTicketsAsync(ticketKeys, ct);
            qaData = new EpicQaData(testsLinks, blocksLinks, runsByTeId);
        }

        // 10. Compute epic progress
        var result = epicProgressService.ComputeEpicProgress(
            epicTickets, closedMemberships, unlinkedTickets, settings, statusTransitions, closedSprints, subTeam, qaData);

        await SendOkAsync(result, ct);
    }
}
