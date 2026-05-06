namespace Fokus.API.Features.Sync.SyncSprints;

public class SyncSprintsResponse
{
    public int SprintsAttempted { get; set; }
    public int SprintsSynced { get; set; }
    public int TicketsUpserted { get; set; }
    public int DevelopersDiscovered { get; set; }
    public List<SprintSyncFailure> Failures { get; set; } = [];
}

public class SprintSyncFailure
{
    public int SprintId { get; set; }
    public string Error { get; set; } = string.Empty;
}
