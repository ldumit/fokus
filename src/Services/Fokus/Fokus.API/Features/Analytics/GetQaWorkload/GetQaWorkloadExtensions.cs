using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetQaWorkload;

public static class GetQaWorkloadExtensions
{
    public static QaWorkloadResponseDto ToQaWorkloadResponseDto(this QaWorkloadMultiSprintResult multiSprint) => new()
    {
        HasQaData = true,
        Mode = "multi",
        MultiSprint = new QaWorkloadMultiSprintDto
        {
            Sprints = multiSprint.Sprints.Select(ToQaWorkloadSprintInfoDto).ToList(),
            TeamMetrics = new QaWorkloadTeamMetricsDto
            {
                TotalTes = multiSprint.TeamMetrics.TotalTes,
                TotalRunsCompleted = multiSprint.TeamMetrics.TotalRunsCompleted,
                PassCount = multiSprint.TeamMetrics.PassCount,
                FailCount = multiSprint.TeamMetrics.FailCount,
                TeamPassRate = multiSprint.TeamMetrics.TeamPassRate
            },
            Developers = multiSprint.Developers.Select(ToQaWorkloadEntryDto).ToList()
        }
    };

    public static QaWorkloadResponseDto ToQaWorkloadResponseDto(this QaWorkloadSingleSprintResult singleSprint) => new()
    {
        HasQaData = true,
        Mode = "single",
        SingleSprint = new QaWorkloadSingleSprintDto
        {
            Sprint = singleSprint.Sprint.ToQaWorkloadSprintInfoDto(),
            TeamMetrics = new QaWorkloadSingleTeamMetricsDto
            {
                TotalTes = singleSprint.TeamMetrics.TotalTes.ToQaWorkloadMetricCardDto(),
                TotalRunsCompleted = singleSprint.TeamMetrics.TotalRunsCompleted.ToQaWorkloadMetricCardDto(),
                TeamPassRate = singleSprint.TeamMetrics.TeamPassRate.ToQaWorkloadMetricCardDto()
            },
            Developers = singleSprint.Developers.Select(ToQaWorkloadSingleEntryDto).ToList()
        }
    };

    public static QaWorkloadSprintInfoDto ToQaWorkloadSprintInfoDto(this SprintRef s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        StartDate = s.StartDate,
        EndDate = s.EndDate
    };

    public static WorkloadAlertDto ToWorkloadAlertDto(this WorkloadAlert alert) => new()
    {
        IsActive = alert.IsActive,
        ConsecutiveSprintCount = alert.ConsecutiveSprintCount,
        ThresholdPercent = alert.ThresholdPercent
    };

    public static QaWorkloadEntryDto ToQaWorkloadEntryDto(this QaWorkloadEntry e) => new()
    {
        AccountId = e.AccountId,
        DisplayName = e.DisplayName,
        SubTeam = e.SubTeam,
        AvatarUrl = e.AvatarUrl,
        TesOwned = e.TesOwned,
        RunsCompleted = e.RunsCompleted,
        PassCount = e.PassCount,
        FailCount = e.FailCount,
        PassRate = e.PassRate,
        StoriesCovered = e.StoriesCovered,
        BugsFound = e.BugsFound,
        SprintBreakdowns = e.SprintBreakdowns.Select(b => new QaWorkloadSprintBreakdownDto
        {
            SprintId = b.SprintId,
            SprintName = b.SprintName,
            TesOwned = b.TesOwned,
            RunsCompleted = b.RunsCompleted,
            PassCount = b.PassCount,
            FailCount = b.FailCount,
            PassRate = b.PassRate,
            StoriesCovered = b.StoriesCovered,
            BugsFound = b.BugsFound
        }).ToList(),
        WorkloadAlert = e.WorkloadAlert.ToWorkloadAlertDto()
    };

    public static QaWorkloadSingleEntryDto ToQaWorkloadSingleEntryDto(this QaWorkloadSingleEntry e) => new()
    {
        AccountId = e.AccountId,
        DisplayName = e.DisplayName,
        SubTeam = e.SubTeam,
        AvatarUrl = e.AvatarUrl,
        TesOwned = e.TesOwned,
        RunsCompleted = e.RunsCompleted,
        PassCount = e.PassCount,
        FailCount = e.FailCount,
        PassRate = e.PassRate,
        StoriesCovered = e.StoriesCovered,
        BugsFound = e.BugsFound,
        TesOwnedDelta = e.TesOwnedDelta,
        TesOwnedDirection = e.TesOwnedDirection,
        TesOwnedPolarity = e.TesOwnedPolarity,
        RunsCompletedDelta = e.RunsCompletedDelta,
        RunsCompletedDirection = e.RunsCompletedDirection,
        RunsCompletedPolarity = e.RunsCompletedPolarity,
        PassCountDelta = e.PassCountDelta,
        PassCountDirection = e.PassCountDirection,
        PassCountPolarity = e.PassCountPolarity,
        FailCountDelta = e.FailCountDelta,
        FailCountDirection = e.FailCountDirection,
        FailCountPolarity = e.FailCountPolarity,
        PassRateDelta = e.PassRateDelta,
        PassRateDirection = e.PassRateDirection,
        PassRatePolarity = e.PassRatePolarity,
        StoriesCoveredDelta = e.StoriesCoveredDelta,
        StoriesCoveredDirection = e.StoriesCoveredDirection,
        StoriesCoveredPolarity = e.StoriesCoveredPolarity,
        BugsFoundDelta = e.BugsFoundDelta,
        BugsFoundDirection = e.BugsFoundDirection,
        BugsFoundPolarity = e.BugsFoundPolarity,
        WorkloadAlert = e.WorkloadAlert.ToWorkloadAlertDto()
    };

    public static QaWorkloadMetricCardDto ToQaWorkloadMetricCardDto(this MetricCard card) => new()
    {
        Name = card.Name,
        Value = card.Value,
        DisplayValue = card.DisplayValue,
        Delta = card.Delta,
        DeltaDirection = card.DeltaDirection,
        DeltaPolarity = card.DeltaPolarity,
        Sparkline = card.Sparkline.Select(p => p.ToSparklinePointDto()).ToList(),
        Rag = card.Rag
    };
}
