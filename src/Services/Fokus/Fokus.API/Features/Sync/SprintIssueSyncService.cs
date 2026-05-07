using Jira.Contracts;

namespace Fokus.API.Features.Sync;

public record SprintIssueSyncResult(int TicketsUpserted, int DevelopersDiscovered);

public record SprintBatchSyncResult(
    int SprintsSynced,
    int TicketsUpserted,
    int DevelopersDiscovered,
    List<SprintSyncFailure> Failures);

public record SprintSyncFailure(int SprintId, string Error);

public record EpicDiscoveryResult(int TicketsDiscovered, int Failures);

public class SprintIssueSyncService(
    IJiraClient jiraClient,
    SprintRepository sprintRepository,
    TicketRepository ticketRepository,
    DeveloperRepository developerRepository,
    ILogger<SprintIssueSyncService> logger)
{
    public async Task<SprintBatchSyncResult> SyncSprintsFromJiraAsync(
        IReadOnlyList<JiraSprint> jiraSprints,
        string boardName,
        bool forcedNotCommitted,
        CancellationToken ct)
    {
        var sprintsSynced = 0;
        var totalTickets = 0;
        var totalDevelopers = 0;
        var failures = new List<SprintSyncFailure>();

        foreach (var jiraSprint in jiraSprints)
        {
            try
            {
                var issues = await jiraClient.GetSprintIssuesAsync(jiraSprint.Id, ct);

                var sprint = Sprint.FromJira(jiraSprint, boardName);
                await sprintRepository.UpsertAsync(sprint, ct);
                await sprintRepository.SaveChangesAsync(ct);

                var result = await SyncAsync(sprint, issues, forcedNotCommitted, ct);

                sprintsSynced++;
                totalTickets += result.TicketsUpserted;
                totalDevelopers += result.DevelopersDiscovered;
            }
            catch (Exception ex)
            {
                failures.Add(new SprintSyncFailure(jiraSprint.Id, ex.Message));
            }
        }

        return new SprintBatchSyncResult(sprintsSynced, totalTickets, totalDevelopers, failures);
    }

    public async Task<EpicDiscoveryResult> SyncEpicDiscoveryAsync(
        CancellationToken ct)
    {
        var epicKeys = await ticketRepository.GetAllKeysWithEpicAsync(ct);
        var ticketsDiscovered = 0;
        var failures = 0;

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
                    var ticket = Ticket.FromJira(issue);
                    await ticketRepository.UpsertAsync(ticket, ct);

                    var developer = Developer.FromJira(issue);
                    if (developer is not null)
                        await developerRepository.UpsertAsync(developer, ct);
                }

                await ticketRepository.SaveChangesAsync(ct);

                ticketsDiscovered += newIssues.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync epic {EpicKey}", epicKey);
                failures++;
            }
        }

        return new EpicDiscoveryResult(ticketsDiscovered, failures);
    }

    public async Task<SprintIssueSyncResult> SyncAsync(
        Sprint sprint,
        IReadOnlyList<JiraIssue> issues,
        bool forcedNotCommitted,
        CancellationToken ct)
    {
        var memberships = new List<SprintMembership>();
        var developerCount = 0;

        foreach (var issue in issues)
        {
            var ticket = Ticket.FromJira(issue);
            await ticketRepository.UpsertAsync(ticket, ct);

            var developer = Developer.FromJira(issue);
            if (developer is not null)
            {
                await developerRepository.UpsertAsync(developer, ct);
                developerCount++;
            }

            var transitions = StatusTransition.ListFromJira(issue);
            await ticketRepository.ReplaceTransitionsAsync(issue.Key, transitions, ct);

            var membership = SprintMembership.FromJira(issue, sprint, forcedNotCommitted);
            memberships.Add(membership);
        }

        await ticketRepository.SaveChangesAsync(ct);

        await sprintRepository.UpsertMembershipsAsync(sprint.Id, memberships, ct);
        await sprintRepository.SaveChangesAsync(ct);

        return new SprintIssueSyncResult(issues.Count, developerCount);
    }
}
