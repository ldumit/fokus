namespace Fokus.Domain.Auth;

public partial class AppUser
{
    public static AppUser CreateBootstrapAdmin(string googleId, string email, string displayName, string? avatarUrl)
    {
        return new AppUser
        {
            GoogleId = googleId,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public static AppUser CreateFromInvitation(string googleId, string email, string displayName, string? avatarUrl, string role)
    {
        return new AppUser
        {
            GoogleId = googleId,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void RecordLogin(string displayName, string? avatarUrl)
    {
        LastLoginAt = DateTime.UtcNow;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}
