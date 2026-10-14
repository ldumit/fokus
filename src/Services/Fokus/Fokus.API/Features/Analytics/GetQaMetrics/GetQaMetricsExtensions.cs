using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetQaMetrics;

public static class GetQaMetricsExtensions
{
    public static QaMetricsResponse ToQaMetricsResponse(this QaMetricsResult qaMetrics) => new()
    {
        HasQaData = qaMetrics.HasQaData,
        CoverageRate = qaMetrics.CoverageRate.ToMetricCardDto(),
        ExecutionRate = qaMetrics.ExecutionRate.ToMetricCardDto(),
        PassRate = qaMetrics.PassRate.ToMetricCardDto(),
        BugsFound = qaMetrics.BugsFound,
        QualitySubScore = qaMetrics.QualitySubScore,
        QualityBreakdown = qaMetrics.QualityBreakdown is null ? null : new QualityBreakdownResponse
        {
            CoverageScore = qaMetrics.QualityBreakdown.CoverageScore,
            CoverageWeight = qaMetrics.QualityBreakdown.CoverageWeight,
            PassRateScore = qaMetrics.QualityBreakdown.PassRateScore,
            PassRateWeight = qaMetrics.QualityBreakdown.PassRateWeight
        },
        UntestedCount = qaMetrics.UntestedCount,
        FailingCount = qaMetrics.FailingCount
    };
}
