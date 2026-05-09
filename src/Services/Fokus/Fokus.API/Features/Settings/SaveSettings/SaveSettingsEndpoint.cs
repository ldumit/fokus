namespace Fokus.API.Features.Settings.SaveSettings;

[HttpPut("/api/settings")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveSettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveSettingsCommand, SaveSettingsResponse>
{
    public override async Task HandleAsync(SaveSettingsCommand command, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);

        var settings = new AppSettings
        {
            Id = 1,
            BoardId = command.BoardId,
            DoneStatuses = command.DoneStatuses,
            WorkflowStages = command.WorkflowStages,
            HealthThresholds = command.HealthThresholds,
            HealthWeights = command.HealthWeights,
            BugRatioAlertThreshold = command.BugRatioAlertThreshold,
            BugRatioConsecutiveSprintCount = command.BugRatioConsecutiveSprintCount,
            SyncBackSprintCount = command.SyncBackSprintCount,
            PlanningWindowDays = command.PlanningWindowDays,
            DefaultSpPerBug = command.DefaultSpPerBug,
            ExcludedFromScopeStatuses = existing.ExcludedFromScopeStatuses
        };

        await repository.SaveAsync(settings, ct);

        await SendOkAsync(new SaveSettingsResponse { Success = true }, ct);
    }
}
