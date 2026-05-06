using System.Net;
using FastEndpoints;
using Fokus.API.Infrastructure.Jira;

namespace Fokus.API.Features.Sync.GetJiraSprints;

public class GetJiraSprintsEndpoint(JiraClient jiraClient)
    : Endpoint<GetJiraSprintsRequest, GetJiraSprintsResponse>
{
    public override void Configure()
    {
        Get("/api/jira/sprints");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetJiraSprintsRequest req, CancellationToken ct)
    {
        try
        {
            var sprints = await jiraClient.GetSprintsAsync(req.BoardId, state: "active,closed", ct);

            var ordered = sprints
                .OrderBy(s => s.StartDate)
                .Select(s => new JiraSprintDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartDate = s.StartDate,
                    State = s.State
                })
                .ToList();

            await SendOkAsync(new GetJiraSprintsResponse { Sprints = ordered }, ct);
        }
        catch (JiraApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SendUnauthorizedAsync(ct);
        }
        catch (JiraApiException)
        {
            await SendAsync(new GetJiraSprintsResponse(), statusCode: 502, cancellation: ct);
        }
    }
}
