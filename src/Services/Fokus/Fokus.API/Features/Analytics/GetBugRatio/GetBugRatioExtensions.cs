using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetBugRatio;

public static class GetBugRatioExtensions
{
    public static BugRatioMultiSprintDto ToBugRatioMultiSprintDto(this BugRatioMultiSprintResult multiSprint) =>
        new(
            multiSprint.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            multiSprint.TeamMetrics.ToBugRatioTeamMetricsDto(),
            multiSprint.IssueTypeBreakdown.Select(ToBugRatioIssueTypeEntryDto).ToList(),
            multiSprint.Developers.Select(ToBugRatioDeveloperEntryDto).ToList());

    public static BugRatioSingleSprintDto ToBugRatioSingleSprintDto(this BugRatioSingleSprintResult singleSprint) =>
        new(
            singleSprint.Sprint.ToSprintRefDto(),
            singleSprint.TeamMetrics.ToBugRatioTeamSingleMetricsDto(),
            singleSprint.IssueTypeBreakdown.Select(ToBugRatioIssueTypeEntryDto).ToList(),
            singleSprint.Developers.Select(ToBugRatioDeveloperSingleEntryDto).ToList());

    public static BugRatioTeamMetricsDto ToBugRatioTeamMetricsDto(this BugRatioTeamMetrics teamMetrics) =>
        new(
            BugRatioPercent: teamMetrics.BugRatioPercent,
            TotalBugSp: teamMetrics.TotalBugSp,
            TotalNonBugSp: teamMetrics.TotalNonBugSp,
            TotalCompletedSp: teamMetrics.TotalCompletedSp,
            teamMetrics.PerSprintTrend.Select(ToBugRatioTeamTrendEntryDto).ToList());

    public static BugRatioIssueTypeEntryDto ToBugRatioIssueTypeEntryDto(this BugRatioIssueTypeEntry issueTypeEntry) =>
        new(
            issueTypeEntry.IssueType,
            issueTypeEntry.TicketCount,
            issueTypeEntry.SpTotal);

    public static BugRatioDeveloperEntryDto ToBugRatioDeveloperEntryDto(this BugRatioDeveloperEntry developerEntry) =>
        new(
            AccountId: developerEntry.AccountId,
            DisplayName: developerEntry.DisplayName,
            SubTeam: developerEntry.SubTeam,
            AvatarUrl: developerEntry.AvatarUrl,
            BugSp: developerEntry.BugSp,
            NonBugSp: developerEntry.NonBugSp,
            CompletedSp: developerEntry.CompletedSp,
            BugRatioPercent: developerEntry.BugRatioPercent,
            BugTicketCount: developerEntry.BugTicketCount,
            NonBugTicketCount: developerEntry.NonBugTicketCount,
            developerEntry.SprintBreakdowns.Select(ToBugRatioDeveloperSprintBreakdownDto).ToList(),
            developerEntry.Alert.ToBugRatioAlertStatusDto());

    public static BugRatioTeamSingleMetricsDto ToBugRatioTeamSingleMetricsDto(this BugRatioTeamSingleMetrics teamSingleMetrics) =>
        new(
            BugRatioPercent: teamSingleMetrics.BugRatioPercent.ToBugRatioMetricCardDto(),
            BugSp: teamSingleMetrics.BugSp.ToBugRatioMetricCardDto(),
            NonBugSp: teamSingleMetrics.NonBugSp.ToBugRatioMetricCardDto());

    public static BugRatioDeveloperSingleEntryDto ToBugRatioDeveloperSingleEntryDto(this BugRatioDeveloperSingleEntry developerSingleEntry) =>
        new(
            AccountId: developerSingleEntry.AccountId,
            DisplayName: developerSingleEntry.DisplayName,
            SubTeam: developerSingleEntry.SubTeam,
            AvatarUrl: developerSingleEntry.AvatarUrl,
            BugSp: developerSingleEntry.BugSp,
            NonBugSp: developerSingleEntry.NonBugSp,
            CompletedSp: developerSingleEntry.CompletedSp,
            BugRatioPercent: developerSingleEntry.BugRatioPercent,
            BugTicketCount: developerSingleEntry.BugTicketCount,
            NonBugTicketCount: developerSingleEntry.NonBugTicketCount,
            developerSingleEntry.Delta?.ToBugRatioDeveloperDeltaDto(),
            developerSingleEntry.Alert.ToBugRatioAlertStatusDto());

    public static BugRatioTeamTrendEntryDto ToBugRatioTeamTrendEntryDto(this BugRatioTeamTrendEntry teamTrendEntry) =>
        new(
            teamTrendEntry.SprintId,
            BugRatioPercent: teamTrendEntry.BugRatioPercent,
            BugSp: teamTrendEntry.BugSp,
            NonBugSp: teamTrendEntry.NonBugSp,
            CompletedSp: teamTrendEntry.CompletedSp);

    public static BugRatioDeveloperSprintBreakdownDto ToBugRatioDeveloperSprintBreakdownDto(this BugRatioDeveloperSprintBreakdown developerSprintBreakdown) =>
        new(
            developerSprintBreakdown.SprintId,
            BugSp: developerSprintBreakdown.BugSp,
            NonBugSp: developerSprintBreakdown.NonBugSp,
            CompletedSp: developerSprintBreakdown.CompletedSp,
            BugRatioPercent: developerSprintBreakdown.BugRatioPercent,
            BugTicketCount: developerSprintBreakdown.BugTicketCount,
            NonBugTicketCount: developerSprintBreakdown.NonBugTicketCount);

    public static BugRatioAlertStatusDto ToBugRatioAlertStatusDto(this BugRatioAlertStatus alertStatus) =>
        new(
            alertStatus.IsActive,
            ConsecutiveSprintCount: alertStatus.ConsecutiveSprintCount,
            ThresholdPercent: alertStatus.ThresholdPercent);

    public static BugRatioMetricCardDto ToBugRatioMetricCardDto(this BugRatioMetricCard metricCard) =>
        new(
            metricCard.Name,
            metricCard.Value,
            metricCard.DisplayValue,
            metricCard.Delta,
            DeltaDirection: metricCard.DeltaDirection,
            DeltaPolarity: metricCard.DeltaPolarity);

    public static BugRatioDeveloperDeltaDto ToBugRatioDeveloperDeltaDto(this BugRatioDeveloperDelta developerDelta) =>
        new(
            developerDelta.BugSpDelta,
            BugSpDeltaDirection: developerDelta.BugSpDeltaDirection,
            BugSpDeltaPolarity: developerDelta.BugSpDeltaPolarity,
            developerDelta.NonBugSpDelta,
            NonBugSpDeltaDirection: developerDelta.NonBugSpDeltaDirection,
            NonBugSpDeltaPolarity: developerDelta.NonBugSpDeltaPolarity,
            developerDelta.BugRatioPercentDelta,
            BugRatioPercentDeltaDirection: developerDelta.BugRatioPercentDeltaDirection,
            BugRatioPercentDeltaPolarity: developerDelta.BugRatioPercentDeltaPolarity,
            developerDelta.BugTicketCountDelta,
            BugTicketCountDeltaDirection: developerDelta.BugTicketCountDeltaDirection,
            BugTicketCountDeltaPolarity: developerDelta.BugTicketCountDeltaPolarity,
            developerDelta.NonBugTicketCountDelta,
            NonBugTicketCountDeltaDirection: developerDelta.NonBugTicketCountDeltaDirection,
            NonBugTicketCountDeltaPolarity: developerDelta.NonBugTicketCountDeltaPolarity);
}
