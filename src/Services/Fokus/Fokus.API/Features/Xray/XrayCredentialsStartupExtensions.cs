using Fokus.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xray.GraphQL;

namespace Fokus.API.Features.Xray;

public static class XrayCredentialsStartupExtensions
{
    public static async Task<IHost> ApplyConfiguredXrayCredentialsAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var xrayOptions = scope.ServiceProvider.GetRequiredService<IOptions<XrayOptions>>().Value;
        if (string.IsNullOrEmpty(xrayOptions.ClientId) || string.IsNullOrEmpty(xrayOptions.ClientSecret))
            return host;

        var dbContext = scope.ServiceProvider.GetRequiredService<FokusDbContext>();
        var settings = await dbContext.AppSettings.FirstAsync();
        settings.XrayEnabled = true;
        settings.XrayClientId = xrayOptions.ClientId;
        settings.XrayClientSecret = xrayOptions.ClientSecret;
        await dbContext.SaveChangesAsync();

        return host;
    }
}
