namespace Fokus.API.Features.Sync.SyncBacklog;

public class SyncBacklogResponse
{
    public int BacklogSprintsSynced { get; set; }
    public int EpicTicketsDiscovered { get; set; }
    public int SprintFailures { get; set; }
    public int EpicFailures { get; set; }
}
