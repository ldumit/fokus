using System.Net;
using FastEndpoints;
using Fokus.API.Infrastructure.Jira;
using Fokus.API.Infrastructure.Jira.Mapping;
using Fokus.Persistence.Repositories;

namespace Fokus.API.Features.Sync.SyncSprints;

public class SyncSprintsEndpoint(
    JiraClient jiraClient,
    AppSettingsRepository settingsRepository,
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository)
    : Endpoint<SyncSprintsRequest, SyncSprintsResponse>
{
    public override void Configure()
    {
        Post("/api/sync/sprints");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SyncSprintsRequest req, CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (settings.BoardId is null)
        {
            AddError("BoardId is not configured in settings.");
            await SendErrorsAsync(400, ct);
            return;
        }

        var boardId = settings.BoardId.Value;

        List<Fokus.API.Infrastructure.Jira.Dtos.JiraSprint> allSprints;
        try
        {
            allSprints = await jiraClient.GetSprintsAsync(boardId, state: "active,closed", ct);
        }
        catch (JiraApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }
        catch (JiraApiException)
        {
            await SendAsync(new SyncSprintsResponse(), statusCode: 502, cancellation: ct);
            return;
        }

        var ordered = allSprints.OrderBy(s => s.StartDate).ToList();

        var fromSprint = ordered.FirstOrDefault(s => s.Id == req.FromSprintId);
        var toSprint = ordered.FirstOrDefault(s => s.Id == req.ToSprintId);

        if (fromSprint is null || toSprint is null)
        {
            AddError("One or both sprint IDs were not found in the Jira board's started sprints.");
            await SendErrorsAsync(400, ct);
            return;
        }

        if (fromSprint.StartDate > toSprint.StartDate)
        {
            AddError("FromSprintId must have an earlier or equal start date than ToSprintId.");
            await SendErrorsAsync(400, ct);
            return;
        }

        var sprintsInRange = ordered
            .Where(s => s.StartDate >= fromSprint.StartDate && s.StartDate <= toSprint.StartDate)
            .ToList();

        // Fetch board name from the first sprint's board (use boardId as fallback name)
        var boardName = $"Board {boardId}";

        var response = new SyncSprintsResponse
        {
            SprintsAttempted = sprintsInRange.Count
        };

        foreach (var jiraSprint in sprintsInRange)
        {
            try
            {
                var issues = await jiraClient.GetSprintIssuesAsync(jiraSprint.Id, ct);

                var sprint = JiraMapper.MapSprint(jiraSprint, boardName);
                await sprintRepository.UpsertAsync(sprint, ct);
                await sprintRepository.SaveChangesAsync(ct);

                var memberships = new List<Fokus.Domain.Entities.SprintMembership>();
                var newDeveloperCount = 0;

                foreach (var issue in issues)
                {
                    var ticket = JiraMapper.MapTicket(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = JiraMapper.MapDeveloper(issue);
                    if (developer is not null)
                    {
                        await developerRepository.UpsertAsync(developer, ct);
                        newDeveloperCount++;
                    }

                    await ticketRepository.SaveChangesAsync(ct);
                    await developerRepository.SaveChangesAsync(ct);

                    var transitions = JiraMapper.MapStatusTransitions(issue);
                    await ticketRepository.ReplaceTransitionsAsync(issue.Key, transitions, ct);
                    await ticketRepository.SaveChangesAsync(ct);

                    var membership = JiraMapper.MapMembership(issue, sprint);
                    memberships.Add(membership);
                }

                await sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships, ct);
                await sprintRepository.SaveChangesAsync(ct);

                response.SprintsSynced++;
                response.TicketsUpserted += issues.Count;
                response.DevelopersDiscovered += newDeveloperCount;
            }
            catch (Exception ex)
            {
                response.Failures.Add(new SprintSyncFailure
                {
                    SprintId = jiraSprint.Id,
                    Error = ex.Message
                });
            }
        }

        await SendOkAsync(response, ct);
    }
}
