namespace Fokus.API.Features.Sync.GetJiraSprints;

public class GetJiraSprintsResponse
{
    public List<JiraSprintDto> Sprints { get; set; } = [];
}

public class JiraSprintDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public string State { get; set; } = string.Empty;
}
