using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Fokus.API.Features.Auth.Login;

public static class LoginEndpoint
{
    public static void MapLoginEndpoint(this WebApplication app)
    {
        app.MapGet("/auth/login", async (HttpContext httpContext, string? invite) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/auth/callback"
            };

            if (!string.IsNullOrEmpty(invite))
                properties.Items["invite_token"] = invite;

            await httpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
        }).AllowAnonymous();
    }
}
