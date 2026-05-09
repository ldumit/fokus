namespace Fokus.API.Features.Analytics.GetEpicProgress;

[HttpGet("/api/analytics/epic-progress")]
[Tags("Analytics")]
public class GetEpicProgressEndpoint(
    AppSettingsRepository appSettingsRepository,
    TicketRepository ticketRepository,
    SprintRepository sprintRepository,
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
                new EpicProgressUnlinkedWork(0, 0m)), ct);
            return;
        }

        // 5. Load all closed sprint memberships
        var closedMemberships = await sprintRepository.GetAllClosedSprintMembershipsAsync(ct);

        // 6. Load unlinked tickets (in at least one sprint, no epic key)
        var unlinkedTickets = await ticketRepository.GetTicketsWithoutEpicInSprintsAsync(ct);

        // 7. Compute epic progress
        var result = epicProgressService.ComputeEpicProgress(
            epicTickets, closedMemberships, unlinkedTickets, settings, subTeam);

        await SendOkAsync(result, ct);
    }
}
