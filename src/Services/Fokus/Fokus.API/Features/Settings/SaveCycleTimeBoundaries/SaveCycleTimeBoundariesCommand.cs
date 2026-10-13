namespace Fokus.API.Features.Settings.SaveCycleTimeBoundaries;

public class SaveCycleTimeBoundariesCommand
{
    public string StartStage { get; set; } = string.Empty;
    public string EndStage { get; set; } = string.Empty;
}

public record SaveCycleTimeBoundariesResponse(string StartStage, string EndStage);

public class SaveCycleTimeBoundariesCommandValidator : Validator<SaveCycleTimeBoundariesCommand>
{
    public SaveCycleTimeBoundariesCommandValidator()
    {
        RuleFor(x => x.StartStage)
            .NotEmpty()
            .WithMessage("StartStage is required.");

        RuleFor(x => x.EndStage)
            .NotEmpty()
            .WithMessage("EndStage is required.");
    }
}
