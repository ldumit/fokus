using Fokus.Application.Sync;

namespace Fokus.API.Features.Sync.SyncSprints;

public static class SyncSprintsExtensions
{
    public static SprintSyncFailureDto ToSprintSyncFailureDto(this SprintSyncFailure sprintSyncFailure) =>
        new(
            sprintSyncFailure.SprintId,
            sprintSyncFailure.Error);
}
