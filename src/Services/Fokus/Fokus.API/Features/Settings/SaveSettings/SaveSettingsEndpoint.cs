using FastEndpoints;
using Fokus.Domain.Entities;
using Fokus.Persistence.Repositories;

namespace Fokus.API.Features.Settings.SaveSettings;

public class SaveSettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveSettingsRequest, SaveSettingsResponse>
{
    public override void Configure()
    {
        Put("/api/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SaveSettingsRequest req, CancellationToken ct)
    {
        var settings = new AppSettings
        {
            Id = 1,
            BoardId = req.BoardId,
            DoneStatuses = req.DoneStatuses,
            WorkflowStages = req.WorkflowStages,
            HealthThresholds = req.HealthThresholds,
            HealthWeights = req.HealthWeights
        };

        await repository.SaveAsync(settings, ct);

        await SendOkAsync(new SaveSettingsResponse { Success = true }, ct);
    }
}
