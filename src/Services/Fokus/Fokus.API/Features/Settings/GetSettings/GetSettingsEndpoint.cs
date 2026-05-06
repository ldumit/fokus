using FastEndpoints;
using Fokus.Persistence.Repositories;

namespace Fokus.API.Features.Settings.GetSettings;

public class GetSettingsEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<GetSettingsResponse>
{
    public override void Configure()
    {
        Get("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        await SendOkAsync(new GetSettingsResponse
        {
            BoardId = settings.BoardId,
            DoneStatuses = settings.DoneStatuses,
            WorkflowStages = settings.WorkflowStages,
            HealthThresholds = settings.HealthThresholds,
            HealthWeights = settings.HealthWeights
        }, ct);
    }
}
