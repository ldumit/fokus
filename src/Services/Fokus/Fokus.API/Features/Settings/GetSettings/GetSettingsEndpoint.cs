namespace Fokus.API.Features.Settings.GetSettings;

[HttpGet("/api/settings")]
[Tags("Settings")]
public class GetSettingsEndpoint(AppSettingsRepository repository)
    : EndpointWithoutRequest<GetSettingsResponse>
{
    public override async Task HandleAsync(CancellationToken ct)
    {
        var settings = await repository.GetAsync(ct);

        await Send.OkAsync(new GetSettingsResponse
        {
            BoardId = settings.BoardId,
            DoneStatuses = settings.DoneStatuses,
            WorkflowStages = settings.WorkflowStages,
            HealthThresholds = settings.HealthThresholds,
            HealthWeights = settings.HealthWeights,
            BugRatioAlertThreshold = settings.BugRatioAlertThreshold,
            BugRatioConsecutiveSprintCount = settings.BugRatioConsecutiveSprintCount,
            SyncBackSprintCount = settings.SyncBackSprintCount,
            PlanningWindowDays = settings.PlanningWindowDays,
            DefaultSpPerBug = settings.DefaultSpPerBug,
            BugRatioTarget = settings.BugRatioTarget,
            XrayEnabled = settings.XrayEnabled,
            XrayClientId = settings.XrayClientId,
            XrayClientSecret = XraySecretMask.Apply(settings.XrayClientSecret),
            QaHealthThresholds = settings.QaHealthThresholds,
            QualityHealthWeight = settings.QualityHealthWeight,
            QualitySubScoreWeights = settings.QualitySubScoreWeights
        }, ct);
    }
}
