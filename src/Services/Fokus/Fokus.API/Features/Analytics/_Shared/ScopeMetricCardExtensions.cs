namespace Fokus.API.Features.Analytics.Shared;

public static class ScopeMetricCardExtensions
{
    public static ScopeMetricCardDto ToScopeMetricCardDto(this ScopeMetricCard scopeMetricCard) =>
        new(
            scopeMetricCard.Name,
            scopeMetricCard.Value,
            scopeMetricCard.DisplayValue,
            scopeMetricCard.Delta,
            DeltaDirection: scopeMetricCard.DeltaDirection,
            DeltaPolarity: scopeMetricCard.DeltaPolarity);
}
