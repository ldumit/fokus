using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Qa;


public record QaMetricsResult(
    bool HasQaData,
    MetricCard CoverageRate,
    MetricCard ExecutionRate,
    MetricCard PassRate,
    int BugsFound,
    decimal QualitySubScore,
    QualityBreakdownInfo QualityBreakdown,
    int UntestedCount,
    int FailingCount);

public record QualityBreakdownInfo(
    decimal CoverageScore,
    int CoverageWeight,
    decimal PassRateScore,
    int PassRateWeight);
