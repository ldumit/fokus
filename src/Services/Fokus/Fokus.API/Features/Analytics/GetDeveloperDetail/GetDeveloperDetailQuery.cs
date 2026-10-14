using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

public class GetDeveloperDetailRequest
{
    public string AccountId { get; set; } = string.Empty;
    public int? Last { get; set; }
}

public class GetDeveloperDetailRequestValidator : Validator<GetDeveloperDetailRequest>
{
    public GetDeveloperDetailRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("AccountId is required.");

        RuleFor(x => x.Last)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Last.HasValue)
            .WithMessage("Last must be 0 or greater.");
    }
}

public sealed record GetDeveloperDetailResponse(
    DeveloperDetailInfoDto Developer,
    IReadOnlyList<SprintTrendEntryDto> SprintTrends,
    WorkAllocationSummaryDto WorkAllocation,
    CurrentSprintDetailDto? CurrentSprint,
    decimal BugRatioTarget,
    string JiraInstanceUrl);

public sealed record DeveloperDetailInfoDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    string Role,
    int DefaultCapacityPercent);

public sealed record SprintTrendEntryDto(
    int SprintId,
    string SprintName,
    DateTime StartDate,
    DateTime EndDate,
    decimal FeatureSp,
    decimal BugSp,
    decimal TotalSp,
    decimal AssignedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal? RollingAverageSp,
    decimal BugPercent);

public sealed record WorkAllocationSummaryDto(
    decimal AverageBugPercent,
    int SprintsAboveTarget,
    int TotalSprints);

public sealed record CurrentSprintDetailDto(
    SprintRefDto Sprint,
    int CurrentDay,
    int TotalDays,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal CompletionPercent,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    IReadOnlyList<DayBreakdownEntryDto> DailyBreakdown,
    IReadOnlyList<TicketDetailEntryDto> Tickets);

public sealed record TicketDetailEntryDto(
    string Key,
    string Summary,
    string CurrentStatus,
    decimal? StoryPoints,
    string IssueType,
    int DaysInCurrentStatus,
    string State,
    bool IsStalled);
