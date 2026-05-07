using Blocks.EntityFrameworkCore.Repositories;

namespace Fokus.Persistence.Repositories;

public class DeveloperRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Developer, string>(db)
{
    public async Task<Developer?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await Entity.SingleOrDefaultAsync(d => d.Id == id, ct);

    public async Task<List<Developer>> GetAllAsync(CancellationToken ct = default) =>
        await Entity.OrderBy(d => d.DisplayName).ToListAsync(ct);

    public new async Task UpsertAsync(Developer developer, CancellationToken ct = default)
    {
        var existing = await Entity.SingleOrDefaultAsync(d => d.Id == developer.Id, ct);
        if (existing is null)
        {
            Entity.Add(developer);
        }
        else
        {
            existing.DisplayName = developer.DisplayName;
            existing.AvatarUrl = developer.AvatarUrl;
            existing.IsActive = developer.IsActive;
        }
    }
}
