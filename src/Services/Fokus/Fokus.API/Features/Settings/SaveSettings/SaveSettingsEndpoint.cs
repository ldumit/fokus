namespace Fokus.API.Features.Settings.SaveSettings;

[AllowAnonymous]
[HttpPut("/api/settings")]
[Tags("Settings")]
public class SaveSettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveSettingsCommand, SaveSettingsResponse>
{
    public override async Task HandleAsync(SaveSettingsCommand command, CancellationToken ct)
    {
        var settings = new AppSettings
        {
            Id = 1,
            BoardId = command.BoardId,
            DoneStatuses = command.DoneStatuses,
            WorkflowStages = command.WorkflowStages,
            HealthThresholds = command.HealthThresholds,
            HealthWeights = command.HealthWeights
        };

        await repository.SaveAsync(settings, ct);

        await SendOkAsync(new SaveSettingsResponse { Success = true }, ct);
    }
}
