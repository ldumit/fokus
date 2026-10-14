using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperProgress;

public class GetDeveloperProgressRequest
{
    public string? SubTeam { get; set; }
}

public sealed record GetDeveloperProgressResponse(
    bool HasActiveSprint,
    SprintRefDto? Sprint,
    int CurrentDay,
    int TotalDays,
    bool IsGracePeriod,
    IReadOnlyList<DeveloperProgressAlertDto> Alerts,
    IReadOnlyList<DeveloperProgressEntryDto> Developers);

public sealed record DeveloperProgressAlertDto(
    string AccountId,
    string DisplayName,
    string? AvatarUrl,
    decimal GapSp,
    decimal GapDays,
    decimal? GapDelta,
    string Direction);

public sealed record DeveloperProgressEntryDto(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    decimal AssignedSp,
    decimal CompletedSp,
    decimal FeatureCompletedSp,
    decimal BugCompletedSp,
    decimal CompletionPercent,
    int CapacityPercent,
    decimal DailyPace,
    bool IsBehindPace,
    decimal? PaceGapSp,
    IReadOnlyList<StalledTicketEntryDto> StalledTickets,
    IReadOnlyList<DayBreakdownEntryDto> DailyBreakdown);

public sealed record StalledTicketEntryDto(
    string Key,
    string Summary,
    string CurrentStatus,
    string IssueType,
    decimal? StoryPoints,
    int DaysSinceLastTransition);
