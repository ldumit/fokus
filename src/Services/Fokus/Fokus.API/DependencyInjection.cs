using FastEndpoints;
using FastEndpoints.Swagger;
using Fokus.API.Infrastructure.Jira;
using Fokus.Persistence;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Fokus.API;

public static class DependencyInjection
{
    public static IServiceCollection AddFokusServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddFastEndpoints();
        services.SwaggerDocument();
        services.AddOpenApi();

        services.AddOptions<JiraOptions>()
            .Bind(configuration.GetSection("Jira"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<JiraClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            client.BaseAddress = new Uri(options.InstanceUrl);
        });

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
