using Fokus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fokus.Persistence.Repositories;

public class TicketRepository(FokusDbContext db)
{
    public IQueryable<Ticket> Query() =>
        db.Tickets.Include(t => t.StatusTransitions);

    public async Task<Ticket?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(t => t.Key == key, ct);

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public void Update(Ticket ticket) => db.Tickets.Update(ticket);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
