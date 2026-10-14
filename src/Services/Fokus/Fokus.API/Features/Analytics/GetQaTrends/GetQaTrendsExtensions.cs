using Fokus.API.Features.Analytics.Shared;

namespace Fokus.API.Features.Analytics.GetQaTrends;

public static class GetQaTrendsExtensions
{
    public static GetQaTrendsResponse ToGetQaTrendsResponse(this QaTrendsResult qaTrends) =>
        new(
            qaTrends.HasQaData,
            qaTrends.Sprints.Select(SprintRefExtensions.ToSprintRefDto).ToList(),
            qaTrends.QualityTrends.Select(ToQualityTrendEntryDto).ToList(),
            qaTrends.TestingVolume.Select(ToTestingVolumeEntryDto).ToList(),
            qaTrends.DefectCorrelation?.ToDefectCorrelationDto());

    public static QualityTrendEntryDto ToQualityTrendEntryDto(this QualityTrendEntry qualityTrendEntry) =>
        new(
            qualityTrendEntry.SprintId,
            qualityTrendEntry.SprintName,
            CoverageRate: qualityTrendEntry.CoverageRate,
            PassRate: qualityTrendEntry.PassRate,
            ExecutionRate: qualityTrendEntry.ExecutionRate,
            CoverageRag: qualityTrendEntry.CoverageRag,
            PassRateRag: qualityTrendEntry.PassRateRag,
            ExecutionRag: qualityTrendEntry.ExecutionRag);

    public static TestingVolumeEntryDto ToTestingVolumeEntryDto(this TestingVolumeEntry testingVolumeEntry) =>
        new(
            testingVolumeEntry.SprintId,
            testingVolumeEntry.SprintName,
            TeCount: testingVolumeEntry.TeCount,
            BugsFound: testingVolumeEntry.BugsFound);

    public static DefectCorrelationDto ToDefectCorrelationDto(this DefectCorrelationResult defectCorrelation) =>
        new(
            defectCorrelation.DataPoints.Select(ToDefectCorrelationDataPointDto).ToList(),
            defectCorrelation.PearsonR,
            defectCorrelation.DataPointCount);

    public static DefectCorrelationDataPointDto ToDefectCorrelationDataPointDto(this DefectCorrelationDataPoint defectCorrelationDataPoint) =>
        new(
            defectCorrelationDataPoint.SprintId,
            defectCorrelationDataPoint.SprintName,
            CoverageRate: defectCorrelationDataPoint.CoverageRate,
            NextSprintBugRatio: defectCorrelationDataPoint.NextSprintBugRatio,
            defectCorrelationDataPoint.NextSprintName);
}
