using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fokus.Persistence;

public static class SeedExtensions
{
    public static async Task<IHost> SeedDefaultAppSettingsAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FokusDbContext>();

        if (!await dbContext.AppSettings.AnyAsync())
        {
            dbContext.AppSettings.Add(AppSettings.CreateDefault());
            await dbContext.SaveChangesAsync();
        }

        return host;
    }
}
