using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetCarryOver;

public class GetCarryOverRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetCarryOverRequestValidator : Validator<GetCarryOverRequest>
{
    public GetCarryOverRequestValidator()
    {
        RuleFor(x => x.SprintId)
            .GreaterThan(0)
            .When(x => x.SprintId.HasValue)
            .WithMessage("Sprint ID must be greater than 0.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be at least 0 (0 means all sprints).");

        RuleFor(x => x)
            .Must(x => !(x.SprintId.HasValue && x.Last.HasValue))
            .WithMessage("SprintId and Last cannot both be provided.");

        RuleFor(x => x.SubTeam)
            .NotEmpty()
            .When(x => x.SubTeam is not null)
            .WithMessage("SubTeam must not be empty when provided.");
    }
}

public record CarryOverResponse(
    string Mode,
    CarryOverMultiSprintDto? MultiSprint,
    CarryOverSingleSprintDto? SingleSprint);

public sealed record CarryOverMultiSprintDto(
    IReadOnlyList<SprintRefDto> Sprints,
    CarryOverSummaryMetricsDto SummaryMetrics,
    IReadOnlyList<CarryOverPerSprintDataDto> PerSprintData,
    IReadOnlyList<CarryOverIssueTypeEntryDto> IssueTypeBreakdown,
    IReadOnlyList<CarryOverZombieSummaryDto> ZombieTickets);

public sealed record CarryOverSingleSprintDto(
    SprintRefDto Sprint,
    CarryOverSingleSprintMetricsDto Metrics,
    IReadOnlyList<CarryOverStatusDistributionEntryDto> StatusDistribution,
    IReadOnlyList<CarryOverIssueTypeEntryDto> IssueTypeBreakdown,
    CarryOverDestinationDto? CarryOverDestination,
    IReadOnlyList<CarryOverTicketEntryDto> Tickets,
    IReadOnlyList<ZombieTrajectoryEntryDto> ZombieTrajectories);

public sealed record CarryOverSummaryMetricsDto(
    decimal AverageCarryOverRate,
    decimal AverageCarryOverSp,
    int TotalZombieTickets);

public sealed record CarryOverPerSprintDataDto(
    int SprintId,
    decimal CarryOverSp,
    int CarryOverTicketCount,
    decimal CarryOverRate,
    decimal TotalScopeSp,
    IReadOnlyList<CarryOverStatusDistributionEntryDto> StatusDistribution);

public sealed record CarryOverIssueTypeEntryDto(
    string IssueType,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public sealed record CarryOverZombieSummaryDto(
    string TicketKey,
    string Summary,
    string IssueType,
    string CurrentStatus,
    decimal? StoryPoints,
    int SprintCount);

public sealed record CarryOverSingleSprintMetricsDto(
    ScopeMetricCardDto CarryOverRate,
    ScopeMetricCardDto CarryOverSp,
    ScopeMetricCardDto CarryOverTicketCount);

public sealed record CarryOverStatusDistributionEntryDto(
    string StageName,
    int TicketCount,
    decimal SpTotal,
    decimal Percentage);

public sealed record CarryOverDestinationDto(
    int PriorSprintId,
    string PriorSprintName,
    int PriorCarryOverCount,
    decimal PriorCarryOverSp,
    CarryOverDestinationBucketDto Completed,
    CarryOverDestinationBucketDto CarriedAgain,
    CarryOverDestinationBucketDto Removed,
    CarryOverDestinationBucketDto Dropped);

public sealed record CarryOverTicketEntryDto(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string FinalStatus,
    string WorkflowStage,
    int SprintCount,
    bool IsZombie,
    bool IsExcluded);

public sealed record ZombieTrajectoryEntryDto(
    string TicketKey,
    string Summary,
    string IssueType,
    decimal? StoryPoints,
    string CurrentStatus,
    int SprintCount,
    IReadOnlyList<ZombieTrajectorySprintEntryDto> Sprints);

public sealed record CarryOverDestinationBucketDto(
    int Count,
    decimal Sp);

public sealed record ZombieTrajectorySprintEntryDto(
    int SprintId,
    string SprintName,
    string FinalStatus);
