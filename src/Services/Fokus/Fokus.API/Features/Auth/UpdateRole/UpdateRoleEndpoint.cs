using Fokus.API.Features.Auth.GetUsers;

namespace Fokus.API.Features.Auth.UpdateRole;

[HttpPut("/api/users/{id}/role")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class UpdateRoleEndpoint(AppUserRepository appUserRepository)
    : Endpoint<UpdateRoleRequest, UserResponse>
{
    public override async Task HandleAsync(UpdateRoleRequest req, CancellationToken ct)
    {
        var user = await appUserRepository.GetByIdAsync(req.Id, ct);
        if (user is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Last-Admin protection: prevent demoting the last active Admin
        if (user.Role == "Admin" && req.Role == "Manager")
        {
            var adminCount = await appUserRepository.CountActiveAdminsAsync(ct);
            if (adminCount <= 1)
                throw new BadRequestException("Cannot demote the last active Admin.");
        }

        user.Role = req.Role;
        await appUserRepository.SaveChangesAsync(ct);

        await SendOkAsync(new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive
        }, ct);
    }
}
