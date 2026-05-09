namespace Fokus.Domain.Auth;

public partial class AppUser : Entity<int>
{
    public required string GoogleId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Manager";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}
