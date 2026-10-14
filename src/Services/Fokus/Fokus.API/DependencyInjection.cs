using FastEndpoints.Swagger;
using Fokus.API.Features.Auth.Login;
using Fokus.API.Hubs;
using Fokus.Persistence;
using Jira.RestApi;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;
using Xray.GraphQL;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddFokusAuth(configuration, environment);

        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddOpenApi();
        services.AddSignalR();
        services.TryAddSingleton(TimeProvider.System);

        services.AddRestApiJira(configuration);
        services.AddGraphQLXray(configuration);

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
