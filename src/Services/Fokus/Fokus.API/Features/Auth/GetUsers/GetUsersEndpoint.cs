namespace Fokus.API.Features.Auth.GetUsers;

[HttpGet("/api/users")]
[Tags("Users")]
[Authorize(Roles = "Admin")]
public class GetUsersEndpoint(AppUserRepository appUserRepository)
    : EndpointWithoutRequest<List<UserResponse>>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await appUserRepository.GetAllAsync(ct);

        var result = users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUrl,
            Role = u.Role,
            LastLoginAt = u.LastLoginAt,
            IsActive = u.IsActive
        }).ToList();

        await SendOkAsync(result, ct);
    }
}
