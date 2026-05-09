using FastEndpoints.Swagger;
using Fokus.API.Features.Analytics;
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
        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddOpenApi();

        services.AddRestApiJira(configuration);

        services.AddScoped<SprintIssueSyncService>();
        services.AddScoped<WorkflowDetectionService>();
        services.AddScoped<SprintSummaryService>();
        services.AddScoped<DeveloperThroughputService>();
        services.AddScoped<ScopeChangeService>();

        return services;
    }

    public static WebApplication UseFokusMiddleware(this WebApplication app)
    {
        app.UseFastEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerGen();
            app.MapScalarApiReference();
        }

        return app;
    }
}
