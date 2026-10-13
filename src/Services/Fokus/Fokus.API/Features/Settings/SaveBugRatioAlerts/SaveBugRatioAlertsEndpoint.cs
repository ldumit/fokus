namespace Fokus.API.Features.Settings.SaveBugRatioAlerts;

[HttpPut("/api/settings/bug-ratio-alerts")]
[Tags("Settings")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SaveBugRatioAlertsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveBugRatioAlertsCommand, SaveBugRatioAlertsResponse>
{
    public override async Task HandleAsync(SaveBugRatioAlertsCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.BugRatioAlertThreshold = req.AlertThreshold;
        existing.BugRatioConsecutiveSprintCount = req.ConsecutiveSprintCount;
        existing.DefaultSpPerBug = req.DefaultSpPerBug;
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveBugRatioAlertsResponse { Success = true }, ct);
    }
}
