namespace Fokus.Domain.Analytics.Shared;

public record MetricCard(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    List<SparklinePoint> Sparkline)
{
    public string? Rag { get; init; }
}
