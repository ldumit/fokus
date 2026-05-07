using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jira.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace Jira.RestApi;

public static class DependencyInjection
{
    public static IServiceCollection AddRestApiJira(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JiraOptions>()
            .Bind(configuration.GetSection("Jira"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRefitClient<IJiraApi>(new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
        })
        .ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            client.BaseAddress = new Uri(options.InstanceUrl);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Email}:{options.ApiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        });

        services.AddTransient<IJiraClient, RestApiJiraClient>();

        return services;
    }
}
