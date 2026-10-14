namespace Fokus.API.Features.Analytics.GetEpicProgress;

public static class GetEpicProgressExtensions
{
    public static GetEpicProgressResponse ToGetEpicProgressResponse(this EpicProgressResult epicProgress) =>
        new(
            epicProgress.SummaryMetrics.ToEpicProgressSummaryMetricsDto(),
            epicProgress.Epics.Select(ToEpicProgressEntryDto).ToList(),
            epicProgress.UnlinkedWork.ToEpicProgressUnlinkedWorkDto(),
            epicProgress.HasQaData,
            epicProgress.AverageTestCoverage);

    public static EpicProgressSummaryMetricsDto ToEpicProgressSummaryMetricsDto(this EpicProgressSummaryMetrics summaryMetrics) =>
        new(
            ActiveEpicCount: summaryMetrics.ActiveEpicCount,
            CompletedEpicCount: summaryMetrics.CompletedEpicCount,
            summaryMetrics.AverageCompletionPercentage);

    public static EpicProgressEntryDto ToEpicProgressEntryDto(this EpicProgressEntry entry) =>
        new(
            EpicKey: entry.EpicKey,
            EpicName: entry.EpicName,
            TotalTickets: entry.TotalTickets,
            DoneTickets: entry.DoneTickets,
            RemainingTickets: entry.RemainingTickets,
            TicketCompletionPercentage: entry.TicketCompletionPercentage,
            TotalSp: entry.TotalSp,
            DoneSp: entry.DoneSp,
            RemainingSp: entry.RemainingSp,
            ImputedSp: entry.ImputedSp,
            AdjustedTotalSp: entry.AdjustedTotalSp,
            SpCompletionPercentage: entry.SpCompletionPercentage,
            entry.UnestimatedTicketCount,
            Velocity: entry.Velocity,
            ProjectedSprintsRemaining: entry.ProjectedSprintsRemaining,
            entry.ProjectionConfidence,
            entry.ActiveSprintCount,
            StartedDate: entry.StartedDate,
            LastWorkDate: entry.LastWorkDate,
            entry.IsCompleted,
            entry.Tickets.Select(ToEpicProgressTicketEntryDto).ToList(),
            CoverageRate: entry.CoverageRate,
            PassRate: entry.PassRate,
            BugsFound: entry.BugsFound,
            FeatureTicketCount: entry.FeatureTicketCount,
            CoveredTicketCount: entry.CoveredTicketCount,
            CoverageRag: entry.CoverageRag,
            PassRateRag: entry.PassRateRag);

    public static EpicProgressUnlinkedWorkDto ToEpicProgressUnlinkedWorkDto(this EpicProgressUnlinkedWork unlinkedWork) =>
        new(
            unlinkedWork.TicketCount,
            unlinkedWork.TotalSp);

    public static EpicProgressTicketEntryDto ToEpicProgressTicketEntryDto(this EpicProgressTicketEntry ticketEntry) =>
        new(
            TicketKey: ticketEntry.TicketKey,
            Summary: ticketEntry.Summary,
            IssueType: ticketEntry.IssueType,
            ticketEntry.StoryPoints,
            CurrentStatus: ticketEntry.CurrentStatus,
            AssigneeDisplayName: ticketEntry.AssigneeDisplayName,
            ticketEntry.IsDone,
            ticketEntry.TestStatus,
            ticketEntry.TestPassRate,
            ticketEntry.TestBugsFound,
            ticketEntry.TestRunSummary?.ToTestRunSummaryDto());

    public static TestRunSummaryDto ToTestRunSummaryDto(this TestRunSummary testRunSummary) =>
        new(
            Passed: testRunSummary.Passed,
            Failed: testRunSummary.Failed,
            Todo: testRunSummary.Todo,
            Executing: testRunSummary.Executing,
            Aborted: testRunSummary.Aborted);
}
