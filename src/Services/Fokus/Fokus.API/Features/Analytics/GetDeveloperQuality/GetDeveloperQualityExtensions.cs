namespace Fokus.API.Features.Analytics.GetDeveloperQuality;

public static class GetDeveloperQualityExtensions
{
    public static DeveloperQualityResponse ToDeveloperQualityResponse(this DeveloperQualityResult developerQuality) => new()
    {
        HasQaData = developerQuality.HasQaData,
        Sprints = developerQuality.Sprints.Select(s => new DeveloperQualitySprintInfoResponse
        {
            Id = s.Id,
            Name = s.Name,
            StartDate = s.StartDate,
            EndDate = s.EndDate
        }).ToList(),
        Developers = developerQuality.Developers.Select(d => new DeveloperQualityEntryResponse
        {
            AccountId = d.AccountId,
            DisplayName = d.DisplayName,
            SubTeam = d.SubTeam,
            AvatarUrl = d.AvatarUrl,
            BelowMedianStreak = d.BelowMedianStreak,
            SprintBreakdowns = d.SprintBreakdowns.Select(b => new DeveloperQualitySprintBreakdownResponse
            {
                SprintId = b.SprintId,
                Stories = b.Stories,
                Covered = b.Covered,
                CoveragePercent = b.CoveragePercent,
                PassRatePercent = b.PassRatePercent,
                Untested = b.Untested,
                BugsFound = b.BugsFound,
                CoverageRag = b.CoverageRag,
                PassRateRag = b.PassRateRag,
                CoveragePercentDelta = b.CoveragePercentDelta,
                CoveragePercentDeltaDirection = b.CoveragePercentDeltaDirection,
                CoveragePercentDeltaPolarity = b.CoveragePercentDeltaPolarity,
                PassRatePercentDelta = b.PassRatePercentDelta,
                PassRatePercentDeltaDirection = b.PassRatePercentDeltaDirection,
                PassRatePercentDeltaPolarity = b.PassRatePercentDeltaPolarity,
                BugsFoundDelta = b.BugsFoundDelta,
                BugsFoundDeltaDirection = b.BugsFoundDeltaDirection,
                BugsFoundDeltaPolarity = b.BugsFoundDeltaPolarity,
                CoverageSparkline = b.CoverageSparkline?.Select(p => new SparklinePointResponse
                {
                    SprintName = p.SprintName,
                    Value = p.Value
                }).ToList(),
                PassRateSparkline = b.PassRateSparkline?.Select(p => new SparklinePointResponse
                {
                    SprintName = p.SprintName,
                    Value = p.Value
                }).ToList()
            }).ToList()
        }).ToList()
    };
}
