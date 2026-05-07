using System.Text.Json.Serialization;

namespace Jira.Contracts;

public class JiraIssue
{
    public string Key { get; set; } = string.Empty;
    public JiraIssueFields Fields { get; set; } = new();
    public JiraChangelog Changelog { get; set; } = new();
}

public class JiraIssueFields
{
    public string Summary { get; set; } = string.Empty;
    public JiraIssueType? Issuetype { get; set; }
    [JsonPropertyName("customfield_10016")]
    public decimal? StoryPoints { get; set; }
    [JsonPropertyName("customfield_10008")]
    public string? EpicKey { get; set; }
    [JsonPropertyName("customfield_10014")]
    public string? EpicName { get; set; }
    public JiraUser? Assignee { get; set; }
    public JiraPriority? Priority { get; set; }
    public JiraStatus? Status { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Resolutiondate { get; set; }
}

public class JiraIssueType
{
    public string Name { get; set; } = string.Empty;
}

public class JiraUser
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> AvatarUrls { get; set; } = [];
}

public class JiraPriority
{
    public string Name { get; set; } = string.Empty;
}

public class JiraStatus
{
    public string Name { get; set; } = string.Empty;
}
