using FastEndpoints.Swagger;
using Fokus.API.Auth;
using Fokus.API.Features.Analytics;
using Fokus.API.Features.Auth.Login;
using Fokus.API.Features.Settings;
using Fokus.API.Features.Sync;
using Fokus.Persistence;
using Jira.RestApi;
using Scalar.AspNetCore;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFokusAuth(configuration);

        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddOpenApi();

        services.AddRestApiJira(configuration);

        services.AddScoped<SprintIssueSyncService>();
        services.AddScoped<WorkflowDetectionService>();
        services.AddScoped<SprintSummaryService>();
        services.AddScoped<DeveloperThroughputService>();
        services.AddScoped<ScopeChangeService>();
        services.AddScoped<CarryOverService>();
        services.AddScoped<BugRatioService>();
        services.AddScoped<EpicProgressService>();
        services.AddScoped<CycleTimeService>();
        services.AddScoped<LeaderboardService>();

        return services;
    }

    public static WebApplication UseFokusMiddleware(this WebApplication app)
    {
        app.MapLoginEndpoint();

        app.UseFastEndpoints(c =>
        {
            c.Endpoints.Configurator = ep =>
            {
                ep.PreProcessor<ActiveUserPreProcessor>(Order.Before);
            };
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerGen();
            app.MapScalarApiReference();
        }

        return app;
    }
}
