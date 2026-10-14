namespace Fokus.Domain.Analytics.Shared;

public sealed record TestingCrunchFlag(
    decimal CrunchPercentage,
    int CrunchRunCount,
    int TotalRunCount);
