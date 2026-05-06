using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Fokus.API.Infrastructure.Jira.Dtos;
using Microsoft.Extensions.Options;

namespace Fokus.API.Infrastructure.Jira;

public class JiraClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly TokenBucketRateLimiter _rateLimiter;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JiraClient(HttpClient http, IOptions<JiraOptions> options)
    {
        var opt = options.Value;
        _http = http;

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opt.Email}:{opt.ApiToken}"));
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 10,
            AutoReplenishment = true,
            QueueLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    }

    public async Task<List<JiraBoard>> GetBoardsAsync(CancellationToken ct = default)
    {
        var result = new List<JiraBoard>();
        var startAt = 0;
        const int maxResults = 50;

        while (true)
        {
            var page = await GetAsync<JiraPagedResult<JiraBoard>>(
                $"/rest/agile/1.0/board?startAt={startAt}&maxResults={maxResults}", ct);
            result.AddRange(page.Values);
            if (page.IsLast || page.Values.Count == 0) break;
            startAt += page.Values.Count;
        }

        return result;
    }

    public async Task<List<JiraSprint>> GetSprintsAsync(int boardId, string? state = null, CancellationToken ct = default)
    {
        var result = new List<JiraSprint>();
        var startAt = 0;
        const int maxResults = 50;

        var stateParam = state is not null ? $"&state={state}" : string.Empty;

        while (true)
        {
            var page = await GetAsync<JiraPagedResult<JiraSprint>>(
                $"/rest/agile/1.0/board/{boardId}/sprint?startAt={startAt}&maxResults={maxResults}{stateParam}", ct);
            result.AddRange(page.Values);
            if (page.IsLast || page.Values.Count == 0) break;
            startAt += page.Values.Count;
        }

        return result;
    }

    public async Task<List<JiraIssue>> GetSprintIssuesAsync(int sprintId, CancellationToken ct = default)
    {
        const string fields = "summary,issuetype,story_points,customfield_10016,customfield_10014,customfield_10008,assignee,priority,status,created,resolutiondate";
        return await GetAllIssuesAsync(
            $"/rest/agile/1.0/sprint/{sprintId}/issue?expand=changelog&fields={fields}", ct);
    }

    public async Task<List<JiraIssue>> GetBoardBacklogIssuesAsync(int boardId, CancellationToken ct = default)
    {
        const string fields = "summary,issuetype,story_points,customfield_10016,customfield_10014,customfield_10008,assignee,priority,status,created,resolutiondate";
        return await GetAllIssuesAsync(
            $"/rest/agile/1.0/board/{boardId}/backlog?expand=changelog&fields={fields}", ct);
    }

    public async Task<List<JiraIssue>> GetEpicIssuesAsync(string epicKey, CancellationToken ct = default)
    {
        const string fields = "summary,issuetype,story_points,customfield_10016,customfield_10014,customfield_10008,assignee,priority,status,created,resolutiondate";
        return await GetAllIssuesAsync(
            $"/rest/agile/1.0/epic/{epicKey}/issue?expand=changelog&fields={fields}", ct);
    }

    private async Task<List<JiraIssue>> GetAllIssuesAsync(string baseUrl, CancellationToken ct)
    {
        var result = new List<JiraIssue>();
        var startAt = 0;
        const int maxResults = 100;

        var separator = baseUrl.Contains('?') ? "&" : "?";

        while (true)
        {
            var page = await GetAsync<JiraIssuePagedResult>(
                $"{baseUrl}{separator}startAt={startAt}&maxResults={maxResults}", ct);
            result.AddRange(page.Issues);
            if (result.Count >= page.Total || page.Issues.Count == 0) break;
            startAt += page.Issues.Count;
        }

        return result;
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var retryDelayMs = 1000;
        for (var attempt = 0; attempt <= 3; attempt++)
        {
            using var lease = await _rateLimiter.AcquireAsync(1, ct);
            if (!lease.IsAcquired)
                throw new JiraApiException(HttpStatusCode.TooManyRequests, "Rate limit queue full");

            var response = await _http.GetAsync(path, ct);

            if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < 3)
            {
                await Task.Delay(retryDelayMs, ct);
                retryDelayMs *= 2;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                string? jiraMessage = null;
                try
                {
                    var err = JsonDocument.Parse(body);
                    if (err.RootElement.TryGetProperty("errorMessages", out var msgs) &&
                        msgs.GetArrayLength() > 0)
                        jiraMessage = msgs[0].GetString();
                    else if (err.RootElement.TryGetProperty("message", out var msg))
                        jiraMessage = msg.GetString();
                }
                catch { }

                throw new JiraApiException(response.StatusCode, jiraMessage);
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return (await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct))!;
        }

        throw new JiraApiException(HttpStatusCode.TooManyRequests, "Rate limit exceeded after retries");
    }

    public void Dispose() => _rateLimiter.Dispose();
}
