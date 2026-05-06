using System.Net;

namespace Fokus.API.Infrastructure.Jira;

public class JiraApiException(HttpStatusCode statusCode, string? jiraMessage)
    : Exception(jiraMessage ?? $"Jira API returned {(int)statusCode}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? JiraMessage { get; } = jiraMessage;
}
