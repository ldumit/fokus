using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetCycleTime;

public static class GetCycleTimeExtensions
{
    public static CycleTimeSingleSprintDto ToCycleTimeSingleSprintDto(this CycleTimeSingleSprintResult singleSprint) =>
        new(
            singleSprint.Sprint?.ToSprintRefDto(),
            singleSprint.MetricCards?.Select(ToCycleTimeMetricCardDto).ToList(),
            singleSprint.ScatterPlot.Select(ToCycleTimeScatterPointDto).ToList(),
            singleSprint.StageFunnel.Select(ToStageFunnelEntryDto).ToList(),
            singleSprint.IssueTypeBreakdown.Select(ToCycleTimeIssueTypeEntryDto).ToList(),
            singleSprint.DeveloperBreakdown.Select(ToCycleTimeDeveloperEntryDto).ToList(),
            singleSprint.Outliers.Select(ToCycleTimeOutlierEntryDto).ToList(),
            singleSprint.Boundaries.ToCycleTimeBoundariesDto());

    public static CycleTimeMultiSprintDto ToCycleTimeMultiSprintDto(this CycleTimeMultiSprintResult multiSprint) =>
        new(
            multiSprint.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            multiSprint.MetricCards?.Select(ToCycleTimeMetricCardDto).ToList(),
            multiSprint.Trend.Select(ToCycleTimeTrendEntryDto).ToList(),
            multiSprint.StageFunnel.Select(ToStageFunnelEntryDto).ToList(),
            multiSprint.SprintSummaries.Select(ToCycleTimeSprintSummaryEntryDto).ToList(),
            multiSprint.Boundaries.ToCycleTimeBoundariesDto());

    public static CycleTimeMetricCardDto ToCycleTimeMetricCardDto(this CycleTimeMetricCard metricCard) =>
        new(
            metricCard.Name,
            metricCard.Value,
            metricCard.DisplayValue,
            metricCard.Delta,
            DeltaDirection: metricCard.DeltaDirection,
            DeltaPolarity: metricCard.DeltaPolarity);

    public static CycleTimeScatterPointDto ToCycleTimeScatterPointDto(this CycleTimeScatterPoint scatterPoint) =>
        new(
            TicketKey: scatterPoint.TicketKey,
            TicketSummary: scatterPoint.TicketSummary,
            IssueType: scatterPoint.IssueType,
            scatterPoint.CycleTimeDays,
            scatterPoint.CompletionDate,
            scatterPoint.StageBreakdown.Select(ToStageBreakdownEntryDto).ToList(),
            scatterPoint.ReworkCount);

    public static StageFunnelEntryDto ToStageFunnelEntryDto(this StageFunnelEntry stageFunnelEntry) =>
        new(
            stageFunnelEntry.StageName,
            AverageDurationDays: stageFunnelEntry.AverageDurationDays,
            Percentage: stageFunnelEntry.Percentage);

    public static CycleTimeIssueTypeEntryDto ToCycleTimeIssueTypeEntryDto(this CycleTimeIssueTypeEntry issueTypeEntry) =>
        new(
            issueTypeEntry.IssueType,
            issueTypeEntry.TicketCount,
            MedianCycleTime: issueTypeEntry.MedianCycleTime,
            P85CycleTime: issueTypeEntry.P85CycleTime);

    public static CycleTimeDeveloperEntryDto ToCycleTimeDeveloperEntryDto(this CycleTimeDeveloperEntry developerEntry) =>
        new(
            DisplayName: developerEntry.DisplayName,
            AvatarUrl: developerEntry.AvatarUrl,
            SubTeam: developerEntry.SubTeam,
            developerEntry.TicketsCompleted,
            MedianCycleTime: developerEntry.MedianCycleTime,
            P85CycleTime: developerEntry.P85CycleTime,
            developerEntry.DominantStage);

    public static CycleTimeOutlierEntryDto ToCycleTimeOutlierEntryDto(this CycleTimeOutlierEntry outlierEntry) =>
        new(
            TicketKey: outlierEntry.TicketKey,
            TicketSummary: outlierEntry.TicketSummary,
            IssueType: outlierEntry.IssueType,
            outlierEntry.CycleTimeDays,
            outlierEntry.StageBreakdown.Select(ToStageBreakdownEntryDto).ToList(),
            outlierEntry.ReworkCount);

    public static CycleTimeBoundariesDto ToCycleTimeBoundariesDto(this CycleTimeBoundaries boundaries) =>
        new(
            StartStage: boundaries.StartStage,
            EndStage: boundaries.EndStage);

    public static CycleTimeTrendEntryDto ToCycleTimeTrendEntryDto(this CycleTimeTrendEntry trendEntry) =>
        new(
            trendEntry.SprintId,
            trendEntry.SprintName,
            P85CycleTime: trendEntry.P85CycleTime,
            MedianCycleTime: trendEntry.MedianCycleTime,
            trendEntry.TicketsCompleted);

    public static CycleTimeSprintSummaryEntryDto ToCycleTimeSprintSummaryEntryDto(this CycleTimeSprintSummaryEntry sprintSummaryEntry) =>
        new(
            sprintSummaryEntry.SprintId,
            sprintSummaryEntry.SprintName,
            sprintSummaryEntry.StartDate,
            sprintSummaryEntry.TicketsCompleted,
            MedianCycleTime: sprintSummaryEntry.MedianCycleTime,
            P85CycleTime: sprintSummaryEntry.P85CycleTime,
            sprintSummaryEntry.OutlierCount);

    public static StageBreakdownEntryDto ToStageBreakdownEntryDto(this StageBreakdownEntry stageBreakdownEntry) =>
        new(
            stageBreakdownEntry.StageName,
            stageBreakdownEntry.DurationDays);
}
