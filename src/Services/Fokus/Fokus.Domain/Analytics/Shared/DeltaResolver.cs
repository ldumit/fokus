namespace Fokus.Domain.Analytics.Shared;

public static class DeltaResolver
{
    public static string DeltaDirection(decimal delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    public static string DeltaDirection(int delta) =>
        delta > 0 ? "up" : delta < 0 ? "down" : "flat";

    public static string DeltaPolarity(decimal delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    public static string DeltaPolarity(int delta, bool positiveUp) =>
        positiveUp
            ? (delta > 0 ? "positive" : delta < 0 ? "negative" : "neutral")
            : (delta < 0 ? "positive" : delta > 0 ? "negative" : "neutral");

    public static (string? Direction, string? Polarity) ResolveCardDelta(decimal? delta, string polarity)
    {
        if (!delta.HasValue)
            return (null, null);

        var deltaPolarity = polarity switch
        {
            "positive-up" => DeltaPolarity(delta.Value, positiveUp: true),
            "positive-down" => DeltaPolarity(delta.Value, positiveUp: false),
            _ => "neutral"
        };
        return (DeltaDirection(delta.Value), deltaPolarity);
    }
}
