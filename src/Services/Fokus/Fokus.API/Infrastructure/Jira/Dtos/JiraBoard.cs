namespace Fokus.API.Infrastructure.Jira.Dtos;

public class JiraBoard
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
