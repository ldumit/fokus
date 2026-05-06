using FastEndpoints;
using FluentValidation;

namespace Fokus.API.Features.Sync.GetJiraSprints;

public class GetJiraSprintsValidator : Validator<GetJiraSprintsRequest>
{
    public GetJiraSprintsValidator()
    {
        RuleFor(x => x.BoardId)
            .GreaterThan(0)
            .WithMessage("Board ID must be greater than 0.");
    }
}
