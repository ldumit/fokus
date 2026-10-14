using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetSprintSummary;

public static class GetSprintSummaryExtensions
{
    public static GetSprintSummaryResponse ToGetSprintSummaryResponse(this SprintSummaryResult sprintSummary) =>
        new(
            sprintSummary.Sprint?.ToSprintInfoDto(),
            sprintSummary.HealthScore?.ToHealthScoreDto(),
            sprintSummary.Metrics?.ToMetricsDto(),
            sprintSummary.TopEpics.Select(ToEpicProgressDto).ToList(),
            sprintSummary.Leaderboard.Select(ToDeveloperSummaryDto).ToList(),
            sprintSummary.Flags.ToFlagsDto());

    public static SprintInfoDto ToSprintInfoDto(this SprintInfo sprintInfo) =>
        new(
            sprintInfo.Id,
            sprintInfo.Name,
            StartDate: sprintInfo.StartDate,
            EndDate: sprintInfo.EndDate,
            sprintInfo.DurationDays,
            sprintInfo.SyncedAt);

    public static HealthScoreDto ToHealthScoreDto(this HealthScoreResult healthScore) =>
        new(
            healthScore.CompositeScore,
            healthScore.CompositeRag,
            healthScore.CompletionSubScore,
            healthScore.CompletionRag,
            healthScore.DisruptionSubScore,
            healthScore.DisruptionRag,
            healthScore.CarryOverSubScore,
            healthScore.CarryOverRag)
        {
            QualitySubScore = healthScore.QualitySubScore,
            QualityRag = healthScore.QualityRag,
            QualityBreakdown = healthScore.QualityBreakdown?.ToQualityBreakdownDto()
        };

    public static MetricsDto ToMetricsDto(this MetricsResult metrics) =>
        new(
            SpCompleted: metrics.SpCompleted.ToMetricCardDto(),
            CompletionRate: metrics.CompletionRate.ToMetricCardDto(),
            ScopeDisruptionRate: metrics.ScopeDisruptionRate.ToMetricCardDto(),
            BugDisruptionRate: metrics.BugDisruptionRate.ToMetricCardDto(),
            CarryOverRate: metrics.CarryOverRate.ToMetricCardDto(),
            metrics.BugSpCompleted);

    public static EpicProgressDto ToEpicProgressDto(this EpicProgress epicProgress) =>
        new(
            epicProgress.EpicName,
            SpCompletedThisSprint: epicProgress.SpCompletedThisSprint,
            TotalSp: epicProgress.TotalSp,
            DoneSp: epicProgress.DoneSp,
            CompletionPercentage: epicProgress.CompletionPercentage);

    public static DeveloperSummaryDto ToDeveloperSummaryDto(this DeveloperSummary developerSummary) =>
        new(
            DisplayName: developerSummary.DisplayName,
            AvatarUrl: developerSummary.AvatarUrl,
            SubTeam: developerSummary.SubTeam,
            SpCompleted: developerSummary.SpCompleted,
            FeatureSp: developerSummary.FeatureSp,
            BugSp: developerSummary.BugSp,
            FeatureTickets: developerSummary.FeatureTickets,
            BugTickets: developerSummary.BugTickets,
            CapacityPercent: developerSummary.CapacityPercent);

    public static FlagsDto ToFlagsDto(this FlagsResult flags) =>
        new(
            flags.ZombieTickets.Select(ToZombieTicketDto).ToList(),
            flags.MidSprintDisruption?.ToMidSprintDisruptionDto(),
            flags.ZeroSpDevelopers,
            flags.HasAnyFlags)
        {
            TestingCrunch = flags.TestingCrunch?.ToTestingCrunchFlagDto()
        };

    public static QualityBreakdownDto ToQualityBreakdownDto(this QualityBreakdownResult qualityBreakdown) =>
        new(
            qualityBreakdown.CoverageScore,
            qualityBreakdown.CoverageWeight,
            qualityBreakdown.PassRateScore,
            qualityBreakdown.PassRateWeight);

    public static ZombieTicketDto ToZombieTicketDto(this ZombieTicket zombieTicket) =>
        new(
            TicketKey: zombieTicket.TicketKey,
            Summary: zombieTicket.Summary,
            zombieTicket.SprintCount,
            zombieTicket.AssigneeName);

    public static MidSprintDisruptionDto ToMidSprintDisruptionDto(this MidSprintDisruption midSprintDisruption) =>
        new(
            midSprintDisruption.TotalSp,
            midSprintDisruption.TicketCount);

    public static TestingCrunchFlagDto ToTestingCrunchFlagDto(this TestingCrunchFlag testingCrunchFlag) =>
        new(
            testingCrunchFlag.CrunchPercentage,
            CrunchRunCount: testingCrunchFlag.CrunchRunCount,
            TotalRunCount: testingCrunchFlag.TotalRunCount);
}
