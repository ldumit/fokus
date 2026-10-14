namespace Fokus.API.Features.Analytics.Shared;

public sealed record SprintRefDto(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate);
