using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Developers;


public record DeveloperQualitySprintBreakdown(
    int SprintId,
    int Stories,
    int Covered,
    decimal CoveragePercent,
    decimal PassRatePercent,
    int Untested,
    int BugsFound,
    string? CoverageRag,
    string? PassRateRag,
    decimal? CoveragePercentDelta,
    string? CoveragePercentDeltaDirection,
    string? CoveragePercentDeltaPolarity,
    decimal? PassRatePercentDelta,
    string? PassRatePercentDeltaDirection,
    string? PassRatePercentDeltaPolarity,
    int? BugsFoundDelta,
    string? BugsFoundDeltaDirection,
    string? BugsFoundDeltaPolarity,
    List<SparklinePoint>? CoverageSparkline,
    List<SparklinePoint>? PassRateSparkline);

public record DeveloperQualityEntry(
    string AccountId,
    string DisplayName,
    string? SubTeam,
    string? AvatarUrl,
    List<DeveloperQualitySprintBreakdown> SprintBreakdowns,
    int? BelowMedianStreak);

public record DeveloperQualityResult(
    bool HasQaData,
    List<SprintRef> Sprints,
    List<DeveloperQualityEntry> Developers);
