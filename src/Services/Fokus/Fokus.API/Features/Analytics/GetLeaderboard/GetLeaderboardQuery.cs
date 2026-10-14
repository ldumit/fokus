using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetLeaderboard;

public class GetLeaderboardRequest
{
    public int? SprintId { get; set; }
    public int? Last { get; set; }
    public string? SubTeam { get; set; }
}

public class GetLeaderboardRequestValidator : Validator<GetLeaderboardRequest>
{
    public GetLeaderboardRequestValidator()
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

public record LeaderboardResponse(
    string Mode,
    LeaderboardMultiSprintDto? MultiSprint,
    LeaderboardSingleSprintDto? SingleSprint);

public sealed record LeaderboardMultiSprintDto(
    IReadOnlyList<SprintRefDto> Sprints,
    IReadOnlyList<LeaderboardDeveloperEntryDto> Developers);

public sealed record LeaderboardSingleSprintDto(
    SprintRefDto Sprint,
    IReadOnlyList<LeaderboardDeveloperSingleEntryDto> Developers);

public sealed record LeaderboardDeveloperEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    IReadOnlyList<LeaderboardDeveloperSprintBreakdownDto> SprintBreakdowns);

public sealed record LeaderboardDeveloperSingleEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int TotalTickets,
    int CapacityPercent,
    LeaderboardDeveloperDeltaDto? Delta);

public sealed record LeaderboardDeveloperSprintBreakdownDto(
    int SprintId,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    int FeatureTickets,
    int BugTickets,
    int CapacityPercent);

public sealed record LeaderboardDeveloperDeltaDto(
    decimal FeatureSpDelta,
    string FeatureSpDeltaDirection,
    string FeatureSpDeltaPolarity,
    decimal BugSpDelta,
    string BugSpDeltaDirection,
    string BugSpDeltaPolarity,
    decimal TotalSpDelta,
    string TotalSpDeltaDirection,
    string TotalSpDeltaPolarity,
    int FeatureTicketsDelta,
    string FeatureTicketsDeltaDirection,
    string FeatureTicketsDeltaPolarity,
    int BugTicketsDelta,
    string BugTicketsDeltaDirection,
    string BugTicketsDeltaPolarity);
