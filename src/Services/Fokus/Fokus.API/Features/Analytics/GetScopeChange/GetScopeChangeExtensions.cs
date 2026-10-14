using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetScopeChange;

public static class GetScopeChangeExtensions
{
    public static ScopeChangeMultiSprintDto ToScopeChangeMultiSprintDto(this ScopeChangeMultiSprintResult multiSprint) =>
        new(
            multiSprint.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            multiSprint.SummaryMetrics.ToScopeChangeSummaryMetricsDto(),
            multiSprint.PerSprintData.Select(ToScopeChangePerSprintDataDto).ToList(),
            multiSprint.ClassificationBreakdown.Select(ToClassificationEntryDto).ToList());

    public static ScopeChangeSingleSprintDto ToScopeChangeSingleSprintDto(this ScopeChangeSingleSprintResult singleSprint) =>
        new(
            singleSprint.Sprint.ToSprintRefDto(),
            singleSprint.Metrics.ToScopeChangeSingleSprintMetricsDto(),
            singleSprint.BurnupData.Select(ToBurnupDataPointDto).ToList(),
            singleSprint.ClassificationBreakdown.Select(ToClassificationEntryDto).ToList(),
            singleSprint.Events.Select(ToScopeChangeEventDto).ToList(),
            singleSprint.BugTimeInProgress.Select(ToBugTimeInProgressDto).ToList());

    public static ScopeChangeSummaryMetricsDto ToScopeChangeSummaryMetricsDto(this ScopeChangeSummaryMetrics summaryMetrics) =>
        new(
            AverageDisruptionRate: summaryMetrics.AverageDisruptionRate,
            AverageDisruptionRateDelta: summaryMetrics.AverageDisruptionRateDelta,
            summaryMetrics.AverageDisruptionRateDeltaDirection,
            summaryMetrics.AverageNetScopeChange,
            summaryMetrics.TotalBugsAdded);

    public static ScopeChangePerSprintDataDto ToScopeChangePerSprintDataDto(this ScopeChangePerSprintData perSprintData) =>
        new(
            perSprintData.SprintId,
            CommittedSpActive: perSprintData.CommittedSpActive,
            CommittedSpTotal: perSprintData.CommittedSpTotal,
            AddedSp: perSprintData.AddedSp,
            RemovedSp: perSprintData.RemovedSp,
            NetScopeChange: perSprintData.NetScopeChange,
            CompletedSp: perSprintData.CompletedSp,
            DisruptionRate: perSprintData.DisruptionRate,
            perSprintData.BugCount,
            perSprintData.BugSpCompleted);

    public static ClassificationEntryDto ToClassificationEntryDto(this ClassificationEntry classificationEntry) =>
        new(
            classificationEntry.Category,
            classificationEntry.TicketCount,
            SpTotal: classificationEntry.SpTotal,
            Percentage: classificationEntry.Percentage);

    public static ScopeChangeSingleSprintMetricsDto ToScopeChangeSingleSprintMetricsDto(this ScopeChangeSingleSprintMetrics singleSprintMetrics) =>
        new(
            CommittedSpActive: singleSprintMetrics.CommittedSpActive.ToScopeMetricCardDto(),
            CommittedSpTotal: singleSprintMetrics.CommittedSpTotal.ToScopeMetricCardDto(),
            AddedSp: singleSprintMetrics.AddedSp.ToScopeMetricCardDto(),
            RemovedSp: singleSprintMetrics.RemovedSp.ToScopeMetricCardDto(),
            NetScopeChange: singleSprintMetrics.NetScopeChange.ToScopeMetricCardDto(),
            DisruptionRate: singleSprintMetrics.DisruptionRate.ToScopeMetricCardDto(),
            BugCount: singleSprintMetrics.BugCount.ToScopeMetricCardDto());

    public static BurnupDataPointDto ToBurnupDataPointDto(this BurnupDataPoint burnupDataPoint) =>
        new(
            burnupDataPoint.DayNumber,
            burnupDataPoint.Date,
            TotalScopeSp: burnupDataPoint.TotalScopeSp,
            CompletedSp: burnupDataPoint.CompletedSp,
            burnupDataPoint.Phase,
            burnupDataPoint.BugSp,
            TotalScopeTickets: burnupDataPoint.TotalScopeTickets,
            CompletedTickets: burnupDataPoint.CompletedTickets,
            BugTickets: burnupDataPoint.BugTickets,
            burnupDataPoint.CommittedTotalSp,
            burnupDataPoint.CommittedTotalTickets);

    public static ScopeChangeEventDto ToScopeChangeEventDto(this ScopeChangeEvent scopeChangeEvent) =>
        new(
            scopeChangeEvent.Date,
            scopeChangeEvent.SprintDayNumber,
            TicketKey: scopeChangeEvent.TicketKey,
            TicketSummary: scopeChangeEvent.TicketSummary,
            scopeChangeEvent.StoryPoints,
            IssueType: scopeChangeEvent.IssueType,
            Action: scopeChangeEvent.Action,
            Category: scopeChangeEvent.Category,
            scopeChangeEvent.IsExcluded);

    public static BugTimeInProgressDto ToBugTimeInProgressDto(this BugTimeInProgress bugTimeInProgress) =>
        new(
            TicketKey: bugTimeInProgress.TicketKey,
            TicketSummary: bugTimeInProgress.TicketSummary,
            bugTimeInProgress.TimeInActiveDays,
            bugTimeInProgress.CurrentStatus);
}
