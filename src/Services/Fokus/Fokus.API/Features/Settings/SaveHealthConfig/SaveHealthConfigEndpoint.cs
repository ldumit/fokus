namespace Fokus.API.Features.Settings.SaveHealthConfig;

[HttpPut("/api/settings/health-config")]
[Tags("Settings")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SaveHealthConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveHealthConfigCommand, SaveHealthConfigResponse>
{
    public override async Task HandleAsync(SaveHealthConfigCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.HealthThresholds = req.HealthThresholds;
        existing.HealthWeights = req.HealthWeights;
        existing.QaHealthThresholds = req.QaHealthThresholds;
        existing.QualityHealthWeight = req.QualityHealthWeight;
        existing.QualitySubScoreWeights = req.QualitySubScoreWeights;
        await repository.SaveAsync(existing, ct);
        await Send.OkAsync(new SaveHealthConfigResponse { Success = true }, ct);
    }
}
