namespace Fokus.API.Features.Settings.SaveSettings;

public class SaveSettingsCommand
{
    public int? BoardId { get; set; }
    public List<string> DoneStatuses { get; set; } = [];
    public List<string> WorkflowStages { get; set; } = [];
    public HealthThresholdConfig HealthThresholds { get; set; } = new();
    public HealthWeightConfig HealthWeights { get; set; } = new();
}

public class SaveSettingsResponse
{
    public bool Success { get; set; }
}

public class SaveSettingsCommandValidator : Validator<SaveSettingsCommand>
{
    public SaveSettingsCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .When(x => x.BoardId.HasValue)
            .WithMessage("Board ID must be greater than 0.");

        RuleFor(x => x.DoneStatuses)
            .NotEmpty()
            .WithMessage("At least one done status is required.");

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
