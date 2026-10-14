namespace Fokus.API.Features.Analytics.Shared;

public static class SprintRefExtensions
{
    public static SprintRefDto ToSprintRefDto(this SprintRef sprintRef) =>
        new(
            sprintRef.Id,
            sprintRef.Name,
            StartDate: sprintRef.StartDate,
            EndDate: sprintRef.EndDate);
}
