using System.Text.Json.Serialization;

namespace Jira.Contracts;

public class JiraChangelog
{
    public int MaxResults { get; set; }
    public int Total { get; set; }
    public int StartAt { get; set; }
    public List<JiraHistory> Histories { get; set; } = [];
}

public class JiraHistory
{
    public DateTime Created { get; set; }
    public JiraAuthor Author { get; set; } = new();
    public List<JiraChangeItem> Items { get; set; } = [];
}

public class JiraAuthor
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> AvatarUrls { get; set; } = [];
}

public class JiraChangeItem
{
    public string Field { get; set; } = string.Empty;
    public string? FromString { get; set; }
    [JsonPropertyName("toString")]
    public string? ToStringValue { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}
