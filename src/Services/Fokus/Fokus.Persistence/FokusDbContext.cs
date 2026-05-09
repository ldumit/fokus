using Blocks.EntityFrameworkCore;

namespace Fokus.Persistence;

public class FokusDbContext(DbContextOptions<FokusDbContext> options)
    : ApplicationDbContext<FokusDbContext>(options)
{
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<SprintMembership> SprintMemberships => Set<SprintMembership>();
    public DbSet<StatusTransition> StatusTransitions => Set<StatusTransition>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<DeveloperSprintCapacity> DeveloperSprintCapacities => Set<DeveloperSprintCapacity>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FokusDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
