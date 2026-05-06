namespace Fokus.Domain.Entities;

public class Developer
{
    public required string AccountId { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SubTeam { get; set; }
    public bool IsActive { get; set; } = true;
}
