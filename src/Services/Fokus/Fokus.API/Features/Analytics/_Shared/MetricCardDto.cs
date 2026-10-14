namespace Fokus.API.Features.Analytics.Shared;

public sealed record MetricCardDto(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity,
    IReadOnlyList<SparklinePointDto> Sparkline)
{
    public string? Rag { get; init; }
}

public sealed record SparklinePointDto(
    string SprintName,
    decimal Value);
