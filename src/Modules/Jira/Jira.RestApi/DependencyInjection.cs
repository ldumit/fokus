using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Jira.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace Jira.RestApi;

// TODO: Remove after debugging
class JiraDebugHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);
        if (request.RequestUri?.PathAndQuery.Contains("search") == true)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[JIRA RAW] {request.RequestUri.PathAndQuery[..Math.Min(80, request.RequestUri.PathAndQuery.Length)]}");
            Console.WriteLine($"[JIRA RAW] Status={response.StatusCode} ContentType={response.Content.Headers.ContentType} BodyLen={body.Length}");
            Console.WriteLine($"[JIRA RAW] Body={body[..Math.Min(300, body.Length)]}");
            response.Content = new StringContent(body, System.Text.Encoding.UTF8, response.Content.Headers.ContentType?.MediaType ?? "application/json");
        }
        return response;
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddRestApiJira(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JiraOptions>()
            .Bind(configuration.GetSection("Jira"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<JiraDebugHandler>();
        services.AddRefitClient<IJiraApi>(new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JiraDateTimeConverter(), new JiraNullableDateTimeConverter() }
            })
        })
        .ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            client.BaseAddress = new Uri(options.InstanceUrl);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Email}:{options.ApiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        })
        .AddHttpMessageHandler<JiraDebugHandler>();

        services.AddTransient<IJiraClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<JiraOptions>>().Value;
            var api = sp.GetRequiredService<IJiraApi>();

            if (options.IsTeamManaged)
            {
                var logger = sp.GetRequiredService<ILogger<TeamManagedJiraClient>>();
                return new TeamManagedJiraClient(api, options.ProjectKey, logger);
            }

            return new RestApiJiraClient(api);
        });

        return services;
    }
}
