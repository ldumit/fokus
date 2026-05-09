using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Fokus.API.Features.Auth.Logout;

[HttpPost("/api/auth/logout")]
[Tags("Auth")]
public class LogoutEndpoint : EndpointWithoutRequest
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SendNoContentAsync(ct);
    }
}
