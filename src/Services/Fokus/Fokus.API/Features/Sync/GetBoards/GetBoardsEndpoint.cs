using System.Net;
using FastEndpoints;
using Fokus.API.Infrastructure.Jira;

namespace Fokus.API.Features.Sync.GetBoards;

public class GetBoardsEndpoint(JiraClient jiraClient)
    : EndpointWithoutRequest<GetBoardsResponse>
{
    public override void Configure()
    {
        Get("/api/boards");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var boards = await jiraClient.GetBoardsAsync(ct);

            await SendOkAsync(new GetBoardsResponse
            {
                Boards = boards.Select(b => new BoardDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Type = b.Type
                }).ToList()
            }, ct);
        }
        catch (JiraApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SendUnauthorizedAsync(ct);
        }
        catch (JiraApiException)
        {
            await SendAsync(new GetBoardsResponse(), statusCode: 502, cancellation: ct);
        }
    }
}
