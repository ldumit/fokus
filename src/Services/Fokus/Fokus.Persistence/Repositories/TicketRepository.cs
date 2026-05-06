using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fokus.Persistence.Repositories;

public class TicketRepository(FokusDbContext db)
{
    public IQueryable<Ticket> Query() =>
        db.Tickets.Include(t => t.StatusTransitions);

    public async Task<Ticket?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(t => t.Key == key, ct);

    public async Task<List<string>> GetAllKeysWithEpicAsync(CancellationToken ct = default) =>
        await db.Tickets
            .Where(t => t.EpicKey != null)
            .Select(t => t.EpicKey!)
            .Distinct()
            .ToListAsync(ct);

    public async Task<HashSet<string>> GetExistingKeysAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var keyList = keys.ToList();
        var existing = await db.Tickets
            .Where(t => keyList.Contains(t.Key))
            .Select(t => t.Key)
            .ToListAsync(ct);
        return [.. existing];
    }

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public void Update(Ticket ticket) => db.Tickets.Update(ticket);

    public async Task UpsertAsync(Ticket ticket, CancellationToken ct = default)
    {
        var existing = await db.Tickets.SingleOrDefaultAsync(t => t.Key == ticket.Key, ct);
        if (existing is null)
        {
            db.Tickets.Add(ticket);
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

    public async Task ReplaceTransitionsAsync(string ticketKey, List<StatusTransition> transitions, CancellationToken ct = default)
    {
        var existing = await db.StatusTransitions
            .Where(st => st.TicketKey == ticketKey)
            .ToListAsync(ct);

        db.StatusTransitions.RemoveRange(existing);
        db.StatusTransitions.AddRange(transitions);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
