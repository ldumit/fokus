using FastEndpoints;
using FastEndpoints.Swagger;
using Fokus.Persistence;
using Scalar.AspNetCore;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusServices(this IServiceCollection services)
    {
        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddOpenApi();

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
