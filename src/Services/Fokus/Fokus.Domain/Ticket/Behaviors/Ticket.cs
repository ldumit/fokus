namespace Fokus.Domain;

public partial class Ticket
{
    public static Ticket FromJira(JiraIssue dto) => new()
    {
        Id = dto.Key,
        Summary = dto.Fields.Summary,
        IssueType = dto.Fields.Issuetype?.Name ?? "Unknown",
        StoryPoints = dto.Fields.StoryPoints,
        EpicKey = dto.Fields.EpicKey,
        EpicName = dto.Fields.EpicName,
        AssigneeId = dto.Fields.Assignee?.AccountId,
        Priority = dto.Fields.Priority?.Name ?? "Medium",
        CurrentStatus = dto.Fields.Status?.Name ?? "Unknown",
        CreatedDate = dto.Fields.Created ?? DateTime.UtcNow,
        ResolvedDate = dto.Fields.Resolutiondate
    };
}
