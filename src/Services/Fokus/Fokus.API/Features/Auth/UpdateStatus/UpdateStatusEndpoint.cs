using Fokus.API.Features.Auth.GetUsers;

namespace Fokus.API.Features.Auth.UpdateStatus;

[HttpPut("/api/users/{id}/status")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class UpdateStatusEndpoint(AppUserRepository appUserRepository)
    : Endpoint<UpdateStatusRequest, UserResponse>
{
    public override async Task HandleAsync(UpdateStatusRequest req, CancellationToken ct)
    {
        var user = await appUserRepository.GetByIdAsync(req.Id, ct);
        if (user is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Last-Admin protection: prevent deactivating the last active Admin
        if (user.Role == "Admin" && !req.IsActive)
        {
            var adminCount = await appUserRepository.CountActiveAdminsAsync(ct);
            if (adminCount <= 1)
                throw new BadRequestException("Cannot deactivate the last active Admin.");
        }

        user.IsActive = req.IsActive;
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
