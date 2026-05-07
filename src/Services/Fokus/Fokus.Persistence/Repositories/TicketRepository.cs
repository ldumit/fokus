using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public class TicketRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Ticket, string>(db)
{
    public override IQueryable<Ticket> Query() =>
        Entity.Include(t => t.StatusTransitions);

    public async Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<string>> GetAllKeysWithEpicAsync(CancellationToken ct = default) =>
        await Entity
            .Where(t => t.EpicKey != null)
            .Select(t => t.EpicKey!)
            .Distinct()
            .ToListAsync(ct);

    public async Task<HashSet<string>> GetExistingKeysAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        var existing = await Entity
            .Where(t => keyList.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync(ct);
        return [.. existing];
    }

    public new async Task UpsertAsync(Ticket ticket, CancellationToken ct = default)
    {
        var existing = await Entity.SingleOrDefaultAsync(t => t.Id == ticket.Id, ct);
        if (existing is null)
        {
            Entity.Add(ticket);
        }
        else
        {
            existing.Summary = ticket.Summary;
            existing.IssueType = ticket.IssueType;
            existing.StoryPoints = ticket.StoryPoints;
            existing.EpicKey = ticket.EpicKey;
            existing.EpicName = ticket.EpicName;
            existing.AssigneeId = ticket.AssigneeId;
            existing.Priority = ticket.Priority;
            existing.CurrentStatus = ticket.CurrentStatus;
            existing.CreatedDate = ticket.CreatedDate;
            existing.ResolvedDate = ticket.ResolvedDate;
        }
    }

    public async Task ReplaceTransitionsAsync(string ticketId, List<StatusTransition> transitions, CancellationToken ct = default)
    {
        var existing = await DbContext.StatusTransitions
            .Where(st => st.TicketId == ticketId)
            .ToListAsync(ct);

        DbContext.StatusTransitions.RemoveRange(existing);
        DbContext.StatusTransitions.AddRange(transitions);
    }
}
