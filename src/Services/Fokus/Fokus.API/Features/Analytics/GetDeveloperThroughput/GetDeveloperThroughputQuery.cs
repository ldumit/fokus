using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperThroughput;

public class GetDeveloperThroughputRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetDeveloperThroughputRequestValidator : Validator<GetDeveloperThroughputRequest>
{
    public GetDeveloperThroughputRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(1)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be at least 1.");

        RuleFor(x => x)
            .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
            .WithMessage("SprintId and Last cannot both be provided.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public sealed record GetDeveloperThroughputResponse(
    IReadOnlyList<SprintRefDto> Sprints,
    IReadOnlyList<DeveloperThroughputEntryDto> Developers);

public sealed record DeveloperThroughputEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    IReadOnlyList<SprintBreakdownDto> SprintBreakdowns);

public sealed record SprintBreakdownDto(
    int SprintId,
    decimal SpAssigned,
    decimal SpCompleted,
    decimal CompletionPercent,
    int TicketsDone,
    int TicketsCarriedOver,
    int CapacityPercent,
    decimal? RollingAverageSpCompleted,
    decimal? SpAssignedDelta,
    decimal? SpCompletedDelta,
    decimal? CompletionPercentDelta,
    int? TicketsDoneDelta,
    int? TicketsCarriedOverDelta,
    string? SpAssignedDeltaDirection,
    string? SpCompletedDeltaDirection,
    string? CompletionPercentDeltaDirection,
    string? TicketsDoneDeltaDirection,
    string? TicketsCarriedOverDeltaDirection,
    string? SpAssignedDeltaPolarity,
    string? SpCompletedDeltaPolarity,
    string? CompletionPercentDeltaPolarity,
    string? TicketsDoneDeltaPolarity,
    string? TicketsCarriedOverDeltaPolarity);
