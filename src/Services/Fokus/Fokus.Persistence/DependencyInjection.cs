using Fokus.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fokus.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FokusDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Database")));

        services.AddScoped<SprintRepository>();
        services.AddScoped<TicketRepository>();
        services.AddScoped<DeveloperRepository>();
        services.AddScoped<AppSettingsRepository>();

        return services;
    }
}
