using Fokus.Domain.Analytics.Shared;

namespace Fokus.Domain.Analytics.Sprints;

public static class ScopeMetricCardFactory
{
    public static ScopeMetricCard BuildMetricCard(
        string name,
        decimal value,
        string displayValue,
        decimal? delta,
        string polarity)
    {
        var (direction, deltaPolarity) = DeltaResolver.ResolveCardDelta(delta, polarity);

        return new ScopeMetricCard(
            name,
            Math.Round(value, 1),
            displayValue,
            delta.HasValue ? Math.Round(delta.Value, 1) : null,
            direction,
            deltaPolarity);
    }
}
