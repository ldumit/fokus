namespace Fokus.API.Features.Settings.SaveXraySettings;

[HttpPut("/api/settings/xray")]
[Tags("Settings")]
[Authorize(Policy = AccessPolicies.Admins)]
public class SaveXraySettingsEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveXraySettingsCommand, SaveXraySettingsResponse>
{
    public override async Task HandleAsync(SaveXraySettingsCommand req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);

        existing.XrayEnabled = req.XrayEnabled;
        existing.XrayClientId = req.XrayClientId;

        if (!XraySecretMask.IsKeepExisting(req.XrayClientSecret))
            existing.XrayClientSecret = req.XrayClientSecret;

        await repository.SaveAsync(existing, ct);

        await Send.OkAsync(new SaveXraySettingsResponse { Success = true }, ct);
    }
}
