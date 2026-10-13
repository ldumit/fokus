using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fokus.Persistence;

public static class MigrationExtensions
{
    public static IHost MigrateFokusDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FokusDbContext>();

        EnsureDatabaseFolderExists(dbContext.Database.GetConnectionString());
        dbContext.Database.Migrate();

        return host;
    }

    private static void EnsureDatabaseFolderExists(string? connectionString)
    {
        if (connectionString is null)
            return;

        var folder = Path.GetDirectoryName(new SqliteConnectionStringBuilder(connectionString).DataSource);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
    }
}
