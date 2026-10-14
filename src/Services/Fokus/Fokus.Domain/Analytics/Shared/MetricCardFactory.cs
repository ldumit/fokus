namespace Fokus.Domain.Analytics.Shared;

public static class MetricCardFactory
{
    public static MetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity,
        List<SparklinePoint> sparkline)
    {
        var (direction, deltaPolarity) = DeltaResolver.ResolveCardDelta(delta, polarity);

        return new MetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity,
            sparkline);
    }
}
