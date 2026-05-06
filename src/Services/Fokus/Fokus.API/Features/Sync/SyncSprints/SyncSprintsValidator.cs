using FastEndpoints;
using FluentValidation;

namespace Fokus.API.Features.Sync.SyncSprints;

public class SyncSprintsValidator : Validator<SyncSprintsRequest>
{
    public SyncSprintsValidator()
    {
        RuleFor(x => x.FromSprintId)
            .GreaterThan(0)
            .WithMessage("FromSprintId must be greater than 0.");

        RuleFor(x => x.ToSprintId)
            .GreaterThan(0)
            .WithMessage("ToSprintId must be greater than 0.");
    }
}
