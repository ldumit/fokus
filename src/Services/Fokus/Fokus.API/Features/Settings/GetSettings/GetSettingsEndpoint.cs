namespace Fokus.API.Features.Settings.GetSettings;

[AllowAnonymous]
[HttpGet("/api/settings")]
[Tags("Settings")]
public class GetSettingsEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<GetSettingsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        await SendOkAsync(new GetSettingsResponse
        {
            BoardId = settings.BoardId,
            DoneStatuses = settings.DoneStatuses,
            WorkflowStages = settings.WorkflowStages,
            HealthThresholds = settings.HealthThresholds,
            HealthWeights = settings.HealthWeights,
            BugRatioAlertThreshold = settings.BugRatioAlertThreshold,
            BugRatioConsecutiveSprintCount = settings.BugRatioConsecutiveSprintCount
        }, ct);
    }
}
