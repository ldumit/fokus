using System.Net;
using FastEndpoints;
using Fokus.API.Infrastructure.Jira;
using Fokus.API.Infrastructure.Jira.Mapping;
using Fokus.Persistence.Repositories;

namespace Fokus.API.Features.Sync.SyncBacklog;

public class SyncBacklogEndpoint(
    JiraClient jiraClient,
    AppSettingsRepository settingsRepository,
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository,
    ILogger<SyncBacklogEndpoint> logger)
    : EndpointWithoutRequest<SyncBacklogResponse>
{
    public override void Configure()
    {
        Post("/api/sync/backlog");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        if (settings.BoardId is null)
        {
            AddError("BoardId is not configured in settings.");
            await SendErrorsAsync(400, ct);
            return;
        }

        var boardId = settings.BoardId.Value;
        var boardName = $"Board {boardId}";

        List<Fokus.API.Infrastructure.Jira.Dtos.JiraSprint> futureSprints;
        try
        {
            futureSprints = await jiraClient.GetSprintsAsync(boardId, state: "future", ct);
        }
        catch (JiraApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }
        catch (JiraApiException)
        {
            await SendAsync(new SyncBacklogResponse(), statusCode: 502, cancellation: ct);
            return;
        }

        var backlogSprintsSynced = 0;
        var sprintFailures = 0;

        foreach (var jiraSprint in futureSprints)
        {
            try
            {
                var issues = await jiraClient.GetSprintIssuesAsync(jiraSprint.Id, ct);
                var sprint = JiraMapper.MapSprint(jiraSprint, boardName);

                await sprintRepository.UpsertAsync(sprint, ct);
                await sprintRepository.SaveChangesAsync(ct);

                var memberships = new List<Fokus.Domain.Entities.SprintMembership>();

                foreach (var issue in issues)
                {
                    var ticket = JiraMapper.MapTicket(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = JiraMapper.MapDeveloper(issue);
                    if (developer is not null)
                        await developerRepository.UpsertAsync(developer, ct);

                    await ticketRepository.SaveChangesAsync(ct);
                    if (developer is not null)
                        await developerRepository.SaveChangesAsync(ct);

                    var transitions = JiraMapper.MapStatusTransitions(issue);
                    await ticketRepository.ReplaceTransitionsAsync(issue.Key, transitions, ct);
                    await ticketRepository.SaveChangesAsync(ct);

                    var membership = JiraMapper.MapMembership(issue, sprint, forcedNotCommitted: true);
                    memberships.Add(membership);
                }

                await sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships, ct);
                await sprintRepository.SaveChangesAsync(ct);

                backlogSprintsSynced++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync future sprint {SprintId}", jiraSprint.Id);
                sprintFailures++;
            }
        }

        // Sync epic tickets not already persisted
        var epicKeys = await ticketRepository.GetAllKeysWithEpicAsync(ct);
        var epicTicketsDiscovered = 0;
        var epicFailures = 0;

        foreach (var epicKey in epicKeys)
        {
            try
            {
                var epicIssues = await jiraClient.GetEpicIssuesAsync(epicKey, ct);
                var issueKeys = epicIssues.Select(i => i.Key).ToList();
                var existingKeys = await ticketRepository.GetExistingKeysAsync(issueKeys, ct);

                var newIssues = epicIssues.Where(i => !existingKeys.Contains(i.Key)).ToList();

                foreach (var issue in newIssues)
                {
                    var ticket = JiraMapper.MapTicket(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = JiraMapper.MapDeveloper(issue);
                    if (developer is not null)
                        await developerRepository.UpsertAsync(developer, ct);
                }

                await ticketRepository.SaveChangesAsync(ct);
                await developerRepository.SaveChangesAsync(ct);

                epicTicketsDiscovered += newIssues.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync epic {EpicKey}", epicKey);
                epicFailures++;
            }
        }

        await SendOkAsync(new SyncBacklogResponse
        {
            BacklogSprintsSynced = backlogSprintsSynced,
            EpicTicketsDiscovered = epicTicketsDiscovered,
            SprintFailures = sprintFailures,
            EpicFailures = epicFailures
        }, ct);
    }
}
