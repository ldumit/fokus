using System.Net;
using System.Text.Json;
using Blocks.Exceptions;
using Jira.Contracts;
using Refit;

namespace Jira.RestApi;

public class RestApiJiraClient(IJiraApi api) : IJiraClient
{
    private const string IssueFields = "summary,issuetype,customfield_10016,customfield_10014,customfield_10008,assignee,priority,status,created,resolutiondate";
    private const string IssueExpand = "changelog";

    public async Task<List<JiraBoard>> GetBoardsAsync(CancellationToken ct)
    {
        var result = new List<JiraBoard>();
        var startAt = 0;

        while (true)
        {
            var page = await RequestAsync(() => api.GetBoardsPageAsync(startAt, 50, ct), ct);
            result.AddRange(page.Values);
            if (page.IsLast || page.Values.Count == 0) break;
            startAt += page.Values.Count;
        }

        return result;
    }

    public async Task<List<JiraSprint>> GetSprintsAsync(int boardId, CancellationToken ct, params SprintState[] states)
    {
        var stateParam = states.Length > 0
            ? string.Join(",", states.Select(s => s.ToString().ToLowerInvariant()))
            : null;

        var result = new List<JiraSprint>();
        var startAt = 0;

        while (true)
        {
            var page = await RequestAsync(() => api.GetSprintsPageAsync(boardId, startAt, 50, stateParam, ct), ct);
            result.AddRange(page.Values);
            if (page.IsLast || page.Values.Count == 0) break;
            startAt += page.Values.Count;
        }

        return result;
    }

    public Task<List<JiraIssue>> GetSprintIssuesAsync(int sprintId, CancellationToken ct) =>
        GetAllIssuesAsync((s, m) => api.GetSprintIssuesPageAsync(sprintId, s, m, IssueExpand, IssueFields, ct), ct);

    public Task<List<JiraIssue>> GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct) =>
        GetAllIssuesAsync((s, m) => api.GetBoardBacklogIssuesPageAsync(boardId, s, m, IssueExpand, IssueFields, ct), ct);

    public Task<List<JiraIssue>> GetEpicIssuesAsync(string epicKey, CancellationToken ct) =>
        GetAllIssuesAsync((s, m) => api.GetEpicIssuesPageAsync(epicKey, s, m, IssueExpand, IssueFields, ct), ct);

    private async Task<List<JiraIssue>> GetAllIssuesAsync(
        Func<int, int, Task<IApiResponse<JiraIssuePagedResult>>> fetch,
        CancellationToken ct)
    {
        var result = new List<JiraIssue>();
        var startAt = 0;

        while (true)
        {
            var page = await RequestAsync(() => fetch(startAt, 100), ct);
            result.AddRange(page.Issues);
            if (result.Count >= page.Total || page.Issues.Count == 0) break;
            startAt += page.Issues.Count;
        }

        return result;
    }

    private async Task<T> RequestAsync<T>(Func<Task<IApiResponse<T>>> call, CancellationToken ct)
    {
        var delay = 1000;

        for (var attempt = 0; attempt <= 3; attempt++)
        {
            var response = await call();

            if (response.IsSuccessStatusCode)
                return response.Content!;

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < 3)
            {
                await Task.Delay(delay, ct);
                delay *= 2;
                continue;
            }

            var message = ParseError(response.Error?.Content) ?? $"Jira API returned {(int)response.StatusCode}";

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedException(message);

            throw new BadGatewayException(message);
        }

        throw new BadGatewayException("Jira rate limit exceeded after retries");
    }

    private static string? ParseError(string? body)
    {
        if (string.IsNullOrEmpty(body)) return null;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errorMessages", out var msgs) && msgs.GetArrayLength() > 0)
                return msgs[0].GetString();
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return null;
    }
}
