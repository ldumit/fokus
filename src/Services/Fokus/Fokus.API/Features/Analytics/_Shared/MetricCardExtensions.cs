namespace Fokus.API.Features.Analytics.Shared;

public static class MetricCardExtensions
{
    public static MetricCardDto ToMetricCardDto(this MetricCard metricCard) =>
        new(
            metricCard.Name,
            metricCard.Value,
            metricCard.DisplayValue,
            metricCard.Delta,
            DeltaDirection: metricCard.DeltaDirection,
            DeltaPolarity: metricCard.DeltaPolarity,
            metricCard.Sparkline.Select(ToSparklinePointDto).ToList())
        {
            Rag = metricCard.Rag
        };

    public static SparklinePointDto ToSparklinePointDto(this SparklinePoint sparklinePoint) =>
        new(
            sparklinePoint.SprintName,
            sparklinePoint.Value);
}
