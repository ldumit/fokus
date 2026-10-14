using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetDeveloperDetail;

public static class GetDeveloperDetailExtensions
{
    public static GetDeveloperDetailResponse ToGetDeveloperDetailResponse(this DeveloperDetailResult developerDetail) =>
        new(
            developerDetail.Developer.ToDeveloperDetailInfoDto(),
            developerDetail.SprintTrends.Select(ToSprintTrendEntryDto).ToList(),
            developerDetail.WorkAllocation.ToWorkAllocationSummaryDto(),
            developerDetail.CurrentSprint?.ToCurrentSprintDetailDto(),
            developerDetail.BugRatioTarget,
            developerDetail.JiraInstanceUrl);

    public static DeveloperDetailInfoDto ToDeveloperDetailInfoDto(this DeveloperDetailInfo developerDetailInfo) =>
        new(
            AccountId: developerDetailInfo.AccountId,
            DisplayName: developerDetailInfo.DisplayName,
            SubTeam: developerDetailInfo.SubTeam,
            AvatarUrl: developerDetailInfo.AvatarUrl,
            Role: developerDetailInfo.Role,
            developerDetailInfo.DefaultCapacityPercent);

    public static SprintTrendEntryDto ToSprintTrendEntryDto(this SprintTrendEntry sprintTrendEntry) =>
        new(
            sprintTrendEntry.SprintId,
            sprintTrendEntry.SprintName,
            StartDate: sprintTrendEntry.StartDate,
            EndDate: sprintTrendEntry.EndDate,
            FeatureSp: sprintTrendEntry.FeatureSp,
            BugSp: sprintTrendEntry.BugSp,
            TotalSp: sprintTrendEntry.TotalSp,
            AssignedSp: sprintTrendEntry.AssignedSp,
            CompletionPercent: sprintTrendEntry.CompletionPercent,
            sprintTrendEntry.CapacityPercent,
            RollingAverageSp: sprintTrendEntry.RollingAverageSp,
            BugPercent: sprintTrendEntry.BugPercent);

    public static WorkAllocationSummaryDto ToWorkAllocationSummaryDto(this WorkAllocationSummary workAllocationSummary) =>
        new(
            workAllocationSummary.AverageBugPercent,
            SprintsAboveTarget: workAllocationSummary.SprintsAboveTarget,
            TotalSprints: workAllocationSummary.TotalSprints);

    public static CurrentSprintDetailDto ToCurrentSprintDetailDto(this CurrentSprintDetail currentSprintDetail) =>
        new(
            currentSprintDetail.Sprint.ToSprintRefDto(),
            CurrentDay: currentSprintDetail.CurrentDay,
            TotalDays: currentSprintDetail.TotalDays,
            AssignedSp: currentSprintDetail.AssignedSp,
            CompletedSp: currentSprintDetail.CompletedSp,
            CompletionPercent: currentSprintDetail.CompletionPercent,
            FeatureCompletedSp: currentSprintDetail.FeatureCompletedSp,
            BugCompletedSp: currentSprintDetail.BugCompletedSp,
            DailyPace: currentSprintDetail.DailyPace,
            currentSprintDetail.IsBehindPace,
            currentSprintDetail.PaceGapSp,
            currentSprintDetail.DailyBreakdown.Select(DayBreakdownEntryExtensions.ToDayBreakdownEntryDto).ToList(),
            currentSprintDetail.Tickets.Select(ToTicketDetailEntryDto).ToList());

    public static TicketDetailEntryDto ToTicketDetailEntryDto(this TicketDetailEntry ticketDetailEntry) =>
        new(
            Key: ticketDetailEntry.Key,
            Summary: ticketDetailEntry.Summary,
            CurrentStatus: ticketDetailEntry.CurrentStatus,
            ticketDetailEntry.StoryPoints,
            ticketDetailEntry.IssueType,
            ticketDetailEntry.DaysInCurrentStatus,
            ticketDetailEntry.State,
            ticketDetailEntry.IsStalled);
}
