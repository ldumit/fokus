namespace Fokus.API.Features.Sprints.GetClosedSprints;

public class ClosedSprintItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string State { get; set; } = string.Empty;
}
