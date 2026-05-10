namespace Fokus.API.Features.Settings.SaveHealthConfig;

public class SaveHealthConfigRequest
{
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
}

public class SaveHealthConfigRequestValidator : Validator<SaveHealthConfigRequest>
{
    public SaveHealthConfigRequestValidator()
    {
        RuleFor(x => x.HealthWeights)
            .Must(w => w.Completion + w.Disruption + w.CarryOver == 100)
            .WithMessage("Health weights must sum to 100.");

        RuleFor(x => x.HealthThresholds.CompletionGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.CompletionAmber)
            .InclusiveBetween(0, 100)
            .LessThan(x => x.HealthThresholds.CompletionGreen)
            .WithMessage("Completion amber threshold must be less than green.");

        RuleFor(x => x.HealthThresholds.DisruptionGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.DisruptionAmber)
            .InclusiveBetween(0, 100)
            .GreaterThan(x => x.HealthThresholds.DisruptionGreen)
            .WithMessage("Disruption amber threshold must be greater than green.");

        RuleFor(x => x.HealthThresholds.CarryOverGreen)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.HealthThresholds.CarryOverAmber)
            .InclusiveBetween(0, 100)
            .GreaterThan(x => x.HealthThresholds.CarryOverGreen)
            .WithMessage("Carry-over amber threshold must be greater than green.");
    }
}

[HttpPut("/api/settings/health-config")]
[Tags("Settings")]
[Authorize(Roles = "Admin")]
public class SaveHealthConfigEndpoint(AppSettingsRepository repository)
    : Endpoint<SaveHealthConfigRequest, SaveHealthConfigResponse>
{
    public override async Task HandleAsync(SaveHealthConfigRequest req, CancellationToken ct)
    {
        var existing = await repository.GetAsync(ct);
        existing.HealthThresholds = req.HealthThresholds;
        existing.HealthWeights = req.HealthWeights;
        await repository.SaveAsync(existing, ct);
        await SendOkAsync(new SaveHealthConfigResponse { Success = true }, ct);
    }
}

public class SaveHealthConfigResponse
{
    public bool Success { get; set; }
}
