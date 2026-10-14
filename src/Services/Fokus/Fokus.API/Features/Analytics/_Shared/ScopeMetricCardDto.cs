namespace Fokus.API.Features.Analytics.Shared;

public sealed record ScopeMetricCardDto(
    string Name,
    decimal Value,
    string DisplayValue,
    decimal? Delta,
    string? DeltaDirection,
    string? DeltaPolarity);
