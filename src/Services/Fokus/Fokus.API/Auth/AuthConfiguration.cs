using System.Security.Claims;
using Fokus.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;

namespace Fokus.API.Auth;

public static class AuthConfiguration
{
    public static IServiceCollection AddFokusAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.LoginPath = "/auth/login";
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            })
            .AddGoogle(options =>
            {
                configuration.GetSection("Google").Bind(options);
                options.CallbackPath = "/auth/callback";
                options.SaveTokens = false;
                options.Events.OnTicketReceived = async context =>
                {
                    await HandleTicketReceivedAsync(context);
                };
            });

        return services;
    }

    private static async Task HandleTicketReceivedAsync(TicketReceivedContext context)
    {
        var sp = context.HttpContext.RequestServices;
        var appUserRepository = sp.GetRequiredService<AppUserRepository>();
        var invitationRepository = sp.GetRequiredService<InvitationRepository>();
        var appSettingsRepository = sp.GetRequiredService<AppSettingsRepository>();

        var principal = context.Principal!;
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var avatarUrl = principal.FindFirstValue("urn:google:picture");

        // Read invite token from auth properties
        var inviteToken = context.Properties?.Items.TryGetValue("invite_token", out var token) == true ? token : null;

        AppUser? user = null;

        if (!string.IsNullOrEmpty(inviteToken))
        {
            // Invitation path
            var invitation = await invitationRepository.GetByTokenAsync(inviteToken);

            if (invitation is null || invitation.Status != "Pending" || invitation.IsExpired)
            {
                var reason = invitation is null ? "invalid"
                    : invitation.Status == "Accepted" ? "used"
                    : invitation.Status == "Revoked" ? "revoked"
                    : "expired";
                context.Response.Redirect($"/invite/error?reason={reason}");
                context.HandleResponse();
                return;
            }

            if (!string.Equals(invitation.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/invite/error?reason=email-mismatch");
                context.HandleResponse();
                return;
            }

            user = AppUser.CreateFromInvitation(googleId, email, displayName, avatarUrl, invitation.Role);
            await appUserRepository.AddAsync(user);
            invitation.Accept(DateTime.UtcNow);
            await appUserRepository.SaveChangesAsync();
        }
        else
        {
            var userCount = await appUserRepository.CountAsync();

            if (userCount == 0)
            {
                // Bootstrap path
                user = AppUser.CreateBootstrapAdmin(googleId, email, displayName, avatarUrl);
                await appUserRepository.AddAsync(user);
                await appUserRepository.SaveChangesAsync();

                var settings = await appSettingsRepository.GetAsync();
                var domain = email.Contains('@') ? email.Split('@')[1] : null;
                if (domain is not null && settings.CompanyDomain is null)
                {
                    settings.CompanyDomain = domain;
                    await appSettingsRepository.SaveAsync(settings);
                }
            }
            else
            {
                // Regular login path
                user = await appUserRepository.GetByGoogleIdAsync(googleId);

                if (user is null)
                {
                    context.Response.Redirect("/login?error=no-account");
                    context.HandleResponse();
                    return;
                }

                if (!user.IsActive)
                {
                    context.Response.Redirect("/login?error=deactivated");
                    context.HandleResponse();
                    return;
                }

                user.RecordLogin(displayName, avatarUrl);
                await appUserRepository.SaveChangesAsync();
            }
        }

        // Build claims principal
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.AvatarUrl is not null)
            claims.Add(new Claim("avatar_url", user.AvatarUrl));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var userPrincipal = new ClaimsPrincipal(identity);

        await context.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            userPrincipal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });

        context.Response.Redirect("/");
        context.HandleResponse();
    }
}
