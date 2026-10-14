using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Qa;


public record QualityTrendEntry(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal PassRate,
    decimal ExecutionRate,
    string CoverageRag,
    string PassRateRag,
    string ExecutionRag);

public record TestingVolumeEntry(int SprintId, string SprintName, int TeCount, int BugsFound);

public record DefectCorrelationDataPoint(
    int SprintId,
    string SprintName,
    decimal CoverageRate,
    decimal? NextSprintBugRatio,
    string? NextSprintName);

public record DefectCorrelationResult(
    List<DefectCorrelationDataPoint> DataPoints,
    decimal? PearsonR,
    int DataPointCount);

public record QaTrendsResult(
    bool HasQaData,
    List<SprintRef> Sprints,
    List<QualityTrendEntry> QualityTrends,
    List<TestingVolumeEntry> TestingVolume,
    DefectCorrelationResult? DefectCorrelation);
