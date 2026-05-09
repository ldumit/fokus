using System.Security.Claims;
using System.Text.Json;

namespace Fokus.API.Auth;

public class ActiveUserPreProcessor : IGlobalPreProcessor
{
    public async Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return;

        var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return;

        var appUserRepository = httpContext.RequestServices.GetRequiredService<AppUserRepository>();
        var user = await appUserRepository.GetByIdAsync(userId, ct);
        if (user is not null && user.IsActive)
            return;

        var message = user is null ? "Account not found." : "Your account has been deactivated.";

        httpContext.Response.StatusCode = 401;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            statusCode = 401,
            message
        }), ct);

        await httpContext.Response.CompleteAsync();
    }
}
