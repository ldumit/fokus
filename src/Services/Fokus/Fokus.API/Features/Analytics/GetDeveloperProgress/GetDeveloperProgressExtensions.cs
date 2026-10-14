using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperProgress;

public static class GetDeveloperProgressExtensions
{
    public static GetDeveloperProgressResponse ToGetDeveloperProgressResponse(this DeveloperProgressResult developerProgress) =>
        new(
            developerProgress.HasActiveSprint,
            developerProgress.Sprint?.ToSprintRefDto(),
            CurrentDay: developerProgress.CurrentDay,
            TotalDays: developerProgress.TotalDays,
            developerProgress.IsGracePeriod,
            developerProgress.Alerts.Select(ToDeveloperProgressAlertDto).ToList(),
            developerProgress.Developers.Select(ToDeveloperProgressEntryDto).ToList());

    public static DeveloperProgressAlertDto ToDeveloperProgressAlertDto(this DeveloperProgressAlert alert) =>
        new(
            AccountId: alert.AccountId,
            DisplayName: alert.DisplayName,
            AvatarUrl: alert.AvatarUrl,
            GapSp: alert.GapSp,
            GapDays: alert.GapDays,
            GapDelta: alert.GapDelta,
            alert.Direction);

    public static DeveloperProgressEntryDto ToDeveloperProgressEntryDto(this DeveloperProgressEntry entry) =>
        new(
            AccountId: entry.AccountId,
            DisplayName: entry.DisplayName,
            SubTeam: entry.SubTeam,
            AvatarUrl: entry.AvatarUrl,
            AssignedSp: entry.AssignedSp,
            CompletedSp: entry.CompletedSp,
            FeatureCompletedSp: entry.FeatureCompletedSp,
            BugCompletedSp: entry.BugCompletedSp,
            CompletionPercent: entry.CompletionPercent,
            entry.CapacityPercent,
            entry.DailyPace,
            entry.IsBehindPace,
            entry.PaceGapSp,
            entry.StalledTickets.Select(ToStalledTicketEntryDto).ToList(),
            entry.DailyBreakdown.Select(DayBreakdownEntryExtensions.ToDayBreakdownEntryDto).ToList());

    public static StalledTicketEntryDto ToStalledTicketEntryDto(this StalledTicketEntry stalledTicketEntry) =>
        new(
            Key: stalledTicketEntry.Key,
            Summary: stalledTicketEntry.Summary,
            CurrentStatus: stalledTicketEntry.CurrentStatus,
            IssueType: stalledTicketEntry.IssueType,
            stalledTicketEntry.StoryPoints,
            stalledTicketEntry.DaysSinceLastTransition);
}
