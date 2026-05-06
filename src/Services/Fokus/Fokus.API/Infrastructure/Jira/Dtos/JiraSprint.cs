namespace Fokus.API.Infrastructure.Jira.Dtos;

public class JiraSprint
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int OriginBoardId { get; set; }
}
