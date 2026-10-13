---
name: persistence-patterns
description: EF Core persistence — the data context named for its service, one configuration class per table, migrations, repositories over one base, read stores for raw reads, the save interceptor that sends domain events, seeding, and the cache of a small stable table. This repo's form (SQLite, RepositoryBase with FindByIdOrThrowAsync, the EntityConfiguration bases, explicit registration, start-up seeding of the settings row) is stated in each section. Use when working with database entities, a repository, a table configuration, seeding, migrations, or the persistence project's registration.
user-invocable: true
---

# Persistence Patterns (EF Core)

Every service stores its data through EF Core: a data context, one configuration class per table, migrations, and
repositories over one shared base. Raw SQL stays only where EF has no answer. The generic samples use an ordering
service; each section then says which form this repo uses.

## Assumes

- **EF Core** with a relational provider and its migrations tool. **This repo: SQLite**
  (`options.UseSqlite(configuration.GetConnectionString("Database"))` in
  `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs`; the connection string is `Data Source=fokus.db`).
  The general form assumes SQL Server or PostgreSQL; everything below holds for SQLite unless it says otherwise.
- **The entity and aggregate bases** (`src/BuildingBlocks/Blocks.Domain/Entities/` — `Entity`, `AggregateRoot`,
  `IEntity<TKey>`), **the error family and guards** (`error-handling`), **the event publisher**
  (`src/BuildingBlocks/Blocks.FastEndpoints/DomainEventPublisher.cs`, `create-domain-event-handler`).
- The repository base, the throwing lookup, the configuration bases and the save interceptor already exist in
  `src/BuildingBlocks/Blocks.EntityFrameworkCore/`; the samples of things this repo lacks (a queryable throwing
  lookup, a seed-file reader, a small-table cache) are built once under Look First when first needed.

#### Look First

Before building anything a skill relies on — a guard helper, the central error mapper, a settings binder, an
aggregate or repository base, a save interceptor, an event publisher — look at what the repo already has: the shared
libraries under `src/BuildingBlocks/Blocks.*` (guards in `Blocks.Core`, the error family in `Blocks.Exceptions`, the
entity and aggregate bases in `Blocks.Domain`, the repository base and save interceptor in `Blocks.EntityFrameworkCore`,
the error middleware in `Blocks.AspNetCore`, the event publisher in `Blocks.FastEndpoints`), the module contracts
(`src/Modules/Jira/Jira.Contracts`, `src/Modules/Xray/Xray.Contracts`), and the packages in
`src/Directory.Packages.props`. Use what you find. Only otherwise build it once, in the shared place: the lowest
project every user of it already references; where the users share no project, a new `Blocks.*` project each of them
references (`create-building-blocks-package`), with no reference cycle. Never a copy beside each user.

## Spine Rules

- **EF Core for every table the service owns.** SQL only where EF has no answer — a search EF cannot express, a read
  on a database the service does not own.
- **A table another library creates and owns** is not an EF table: no configuration and no migration for it, and it
  keeps that library's names.
- **The persistence project holds** the data context, the table configurations, the migrations, the repositories,
  the seeding and its registration — and declares no records, enums or settings of its own. **This repo:**
  `src/Services/Fokus/Fokus.Persistence/` has `Configurations/`, `Repositories/`, `Migrations/`, `FokusDbContext.cs`,
  `DesignTimeDbContextFactory.cs` (for `dotnet ef`, `conventions/ef-core.md` § Migrations) and one
  `DependencyInjection` class.
- **No project references an API project,** and a service never references another service's projects.
- **The repository or the data context is the unit of work.** An endpoint calls `SaveChangesAsync(ct)` once; the
  save interceptor sends the domain events on that save.
- **Registration is explicit:** each repository by its own scoped line — no assembly scan, no factory lambda per
  repository.
- **Table set-up runs at start-up** through migrations, never as a repository method.

## Repository Pattern

### The base

Built once, in the lowest project every service's persistence references. No interface: a repository has one
implementation.

**This repo's base:** `RepositoryBase<TContext, TEntity, TKey>` in
`src/BuildingBlocks/Blocks.EntityFrameworkCore/Repositories/RepositoryBase.cs` — keyed by `TKey`, because the
Jira-sourced entities use the Jira key (`Ticket`, `Developer`, `TestExecution` are keyed by `string`, `Sprint` by
`int`):

```csharp
public abstract class RepositoryBase<TContext, TEntity, TKey>(TContext dbContext)
    where TContext : DbContext
    where TEntity : class, IEntity<TKey>
{
    protected readonly TContext DbContext = dbContext;
    protected DbSet<TEntity> Entity => DbContext.Set<TEntity>();

    public virtual IQueryable<TEntity> Query() => Entity;
    public Task<TEntity?> FindByIdAsync(TKey id, CancellationToken ct = default);
    public Task AddAsync(TEntity entity, CancellationToken ct = default);
    public Task UpsertAsync(TEntity entity, CancellationToken ct = default);
    public Task DeleteByIdAsync(TKey id, CancellationToken ct = default);
    public Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

- `Query()` is **public and virtual** here (the general form has it protected): a derived repository overrides it to
  add its standard includes (`SprintRepository` includes `Memberships`).
- The required lookup is the extension `FindByIdOrThrowAsync(id, ct)` in
  `src/BuildingBlocks/Blocks.EntityFrameworkCore/Extensions/RepositoryExtensions.cs` — shared, never re-implemented
  per repository (the general form calls it `GetByIdOrThrowAsync` on the base).
- `UpsertAsync` copies an existing row with `CurrentValues.SetValues(entity)` — the object overload, which uses
  plain reflection and skips members mapped to private fields. Neither overload copies an owned or complex member
  deeply: assign those by hand, as `AppSettingsRepository.SaveAsync` does for its JSON-owned members.

### The service's repositories

The general form puts a one-line generic `Repository<TEntity>` between the base and the tables. **This repo has
none:** each repository is its own class in `src/Services/Fokus/Fokus.Persistence/Repositories/`, with no
interface. Four derive the base directly (`SprintRepository`, `TicketRepository`, `DeveloperRepository`,
`TestExecutionRepository`); three take `FokusDbContext` and query it without the base (`AppUserRepository`,
`InvitationRepository`, `AppSettingsRepository`).

Names: a lookup by key is `GetBy{Key}Async` and returns null when missing; `Find…` is the base's by-id lookup or a
search by something that is not a key, which may return none or many; existence is `Exists…`; writes are
`AddAsync`, `UpsertAsync`, `SaveChangesAsync`. A required lookup throws not-found through the guards
(`error-handling` § Guard Utilities):

```csharp file=SprintRepository.cs
public class SprintRepository(FokusDbContext db)
    : RepositoryBase<FokusDbContext, Sprint, int>(db)
{
    public override IQueryable<Sprint> Query() =>
        Entity.Include(s => s.Memberships);

    public async Task<Sprint?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Query().SingleOrDefaultAsync(s => s.Id == id, ct);
}
```

`AppSettingsRepository.GetAsync` only reads: a missing settings row is a start-up fault, so it throws
`InvalidOperationException` ("the settings row is missing; start-up seeds it"), which the mapper answers with 500 —
deliberately not `OrThrowNotFound`, which would answer 404. It stays tracked, because its callers write through it.

### The queryable throwing lookup

Not in this repo yet; build it in `Blocks.EntityFrameworkCore/Extensions/` when a lookup needs it:

```csharp file=QueryableExtensions.cs
using System.Linq.Expressions;

public static class QueryableExtensions
{
    public static async Task<T> SingleOrThrowAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : class =>
        await query.SingleOrDefaultAsync(predicate, ct)
            ?? throw new NotFoundException($"No {typeof(T).Name} matches the lookup.");
}
```

### Read stores

The general form puts read-only queries — often projected, and the raw SQL EF has no answer for — in a `…ReadStore`
class that returns domain types, never a reply type of the API project. **This repo has no read stores:** an
analytic or cross-aggregate read goes on the repository that owns the primary data, with `AsNoTracking()`
(`conventions/ef-core.md` § Cross-Aggregate Queries), until the pattern recurs.

## Entity Configuration

- **One configuration class per table,** `{Entity}Configuration`, in
  `src/Services/Fokus/Fokus.Persistence/Configurations/`, applied by the context.
- **This repo's bases** (`src/BuildingBlocks/Blocks.EntityFrameworkCore/EntityConfigurations/`):
  - `EntityConfiguration<TEntity>` and `EntityConfiguration<TEntity, TKey>` set the key, and their virtual
    `HasGeneratedId` (default `true`) chooses `ValueGeneratedOnAdd()` or `ValueGeneratedNever()`. A Jira-keyed table
    overrides it to `false` (`SprintConfiguration`, `TicketConfiguration`, `DeveloperConfiguration`, and the Xray
    tables).
  - `AuditedEntityConfiguration<TEntity>` (and `<TEntity, TKey>`) extends it for an `AggregateRoot`: the four audit
    columns (`CreatedById`, `CreatedOn`, `LastModifiedById`, `LastModifiedOn`) and a virtual `HasConcurrencyToken`
    (default `false`) that adds a `RowVersion` shadow property. No Fokus table uses it today.
  - A configuration with nothing to inherit implements `IEntityTypeConfiguration<T>` directly (`AppSettings`,
    `AppUser`, `Invitation`, `DeveloperSprintCapacity`, `SprintMembership` and `TestExecutionLink` do).
  - The bases set no seed data (§ Seed Data (Dual Pattern)).
- **Lengths:** the general form takes them from field-length constants, never a literal. This repo has the constants
  (`src/BuildingBlocks/Blocks.Core/MaxLength.cs`, `MaxLength.C256` …) but its configurations still write literals
  (`HasMaxLength(256)`); a new configuration uses the constants.
- **Enums are stored as text** (`HasConversion<string>()`, as `SprintConfiguration` does).
- **Value objects and settings groups** map as complex types (`ComplexProperty`, flat columns) or owned types
  (`OwnsOne`, a separate or JSON column). This repo maps the settings groups as JSON columns
  (`OwnsOne(..., b => b.ToJson())` in `AppSettingsConfiguration`) — and `HasData` cannot seed an entity with a
  `ToJson()` member (`conventions/ef-core.md` § Additional Gotchas).
- **The concurrency token is off by default.** Turn it on for a table only with a stated reason (two writers really
  race on it).
- **Defaults:** a column with a meaningful default sets `HasDefaultValue(...)` so the migration matches the
  property initializer (`conventions/ef-core.md` § Migration Defaults).
- **Table and column names:** EF Core's defaults in this repo — a table takes its `DbSet` property's name on
  `FokusDbContext` (`Sprints`, `Tickets`, `AppUsers`), a column its property's name.

```csharp file=CustomerConfiguration.cs
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Name).HasMaxLength(MaxLength.C256).IsRequired();
        builder.ComplexProperty(customer => customer.Email, email =>
            email.Property(value => value.Value).HasColumnName("Email").HasMaxLength(MaxLength.C256));
        builder.Property(customer => customer.Tier).HasConversion<string>().HasMaxLength(MaxLength.C32);
    }
}
```

**Changing a tracked entity.** An aggregate changes through its behaviour methods; nothing copies a command's members
onto a tracked entity. Where a row is replaced whole, `UpsertAsync` copies its values; an owned or complex member is
assigned by hand. The check that catches a write that silently did nothing is **save, reload, assert the value
changed** — a test, not a reading of the code.

## DbContext Setup

The context is named for its service — never `AppDbContext` — and applies its configurations from its assembly.
**This repo:** `FokusDbContext` (`src/Services/Fokus/Fokus.Persistence/FokusDbContext.cs`) derives
`ApplicationDbContext<FokusDbContext>` (`src/BuildingBlocks/Blocks.EntityFrameworkCore/ApplicationDbContext.cs`, an
empty base over `DbContext` with no helpers of its own):

```csharp
public class FokusDbContext(DbContextOptions<FokusDbContext> options)
    : ApplicationDbContext<FokusDbContext>(options)
{
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    // one DbSet per table

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FokusDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

The provider is chosen once, in the registration — here `UseSqlite`. A Redis-backed service uses no EF Core
(`redis-patterns`).

```csharp file=DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddFokusPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<FokusDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("Database"));
            options.AddInterceptors(sp.GetRequiredService<ISaveChangesInterceptor>());
        });

        services.AddScoped<SprintRepository>();
        services.AddScoped<AppSettingsRepository>();
        // one scoped line per repository

        return services;
    }
}
```

## Save Interceptor

Sends the domain events of the tracked aggregates after a successful save — the one place domain events are sent on
an EF Core store. **This repo:** `src/BuildingBlocks/Blocks.EntityFrameworkCore/Interceptors/DispatchDomainEventsInterceptor.cs`,
which hands the whole list to `IDomainEventPublisher.PublishAsync(domainEvents, ct)`; the FastEndpoints publisher
(`Blocks.FastEndpoints/DomainEventPublisher.cs`) publishes each with `Mode.WaitForAll`.

```csharp file=DispatchDomainEventsInterceptor.cs
public class DispatchDomainEventsInterceptor(IDomainEventPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, ct);

        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();
        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        await publisher.PublishAsync(domainEvents, ct);
    }
}
```

The events are cleared before they are sent, so a handler that saves again does not send them twice. **The
transactional flavour** is described here, not sampled, and this repo does not use it: it opens a transaction before
the save (in `SavingChangesAsync`), sends after it, and commits — so a handler's writes land with the trigger's; use
it only where a handler must write in that same transaction (`create-domain-event-handler`).

## Migrations at Start-up

Table set-up runs once, at start-up, before the service takes requests — one start-up line per data context, an
extension in the project that owns the context, never a `BackgroundService` (`add-hosted-service` § Start-up Work):

```csharp file=MigrationExtensions.cs
public static class MigrationExtensions
{
    public static IHost Migrate<TContext>(this IHost host)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TContext>().Database.Migrate();

        return host;
    }
}
```

**This repo:** the start-up line for `FokusDbContext` first makes sure the SQLite file's folder exists (it reads the
`Data Source` from the connection string with `SqliteConnectionStringBuilder`), then migrates. It is called from
`src/Services/Fokus/Fokus.API/Program.cs` before the middleware, followed by the settings-row line (§ Seed Data (Dual
Pattern)). A model change always comes with its migration: the API refuses to migrate while the model has pending
changes, and a migration is never added just to silence that error.

## Seed Data (Dual Pattern)

Two kinds of seed data, two places — never a repository method, and never an automatic hook that every table
configuration runs:

- **Master data** — rows that ship with the schema (a transition table, a list of kinds): the table's own
  configuration reads the JSON file explicitly and seeds it with `HasData`, so the rows land in a migration.
- **Test data** — rows for local work: a seeding step called from the start-up file, in development only.

**This repo:**

- **No master data and no test data exist.** The configuration bases seed nothing; a table that gets master data
  later follows the explicit pattern below in its own configuration.
- **Required data — the default settings row.** `AppSettings` must exist in every environment, and its JSON-owned
  members rule out `HasData`. A start-up line in `Program.cs`, right after the migration, adds
  `AppSettings.CreateDefault()` when the table is empty — in every environment. An existing row is never touched.
- **Configured Xray credentials** are copied into the settings row by their own start-up line, after it: an API
  concern, because it reads `XrayOptions`.

```csharp file=SeedFile.cs
using System.Text.Json;
using System.Text.Json.Serialization;

public static class SeedFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<TEntity> Read<TEntity>(string folder)
    {
        var path = Path.Combine(AppContext.BaseDirectory, folder, $"{typeof(TEntity).Name}.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"The seed file {path} is missing.");

        return JsonSerializer.Deserialize<List<TEntity>>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException($"The seed file {path} holds no rows.");
    }
}
```

A master-data configuration seeds with `builder.HasData(SeedFile.Read<OrderStateTransition>("Data/Master"))`. The
test-data step is the service's own, called in development only, and adds the rows of a table that is still empty:

```csharp file=SeedExtensions.cs
public static class SeedExtensions
{
    public static IHost SeedTestData(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        if (!dbContext.Customers.Any())
        {
            dbContext.Customers.AddRange(SeedFile.Read<Customer>("Data/Test"));
            dbContext.SaveChanges();
        }

        return host;
    }
}
```

**The gotchas:**

- **A missing seed file fails,** as above — a reader that returns nothing for a missing file hides a broken deploy.
- **The file is named after the entity type exactly** (`OrderStateTransition.json`).
- **The files are copied to the output** — the persistence project file carries
  `<None Update="Data\**\*.json" CopyToOutputDirectory="PreserveNewest" />`.
- **`HasData` cannot seed an entity with a `ToJson()` member** — seed it at start-up instead, as the settings row is.
- **Explicit ids in test data** need the provider's identity-insert switch while the rows are added (SQL Server).
- **ASP.NET Core Identity users** are seeded through `UserManager`, never as rows.

## Caching Small Tables

This repo caches no table. A table that is truly small and stable — a transition table, a list of kinds — is cached
once, whole, in the persistence layer, keyed by its entity type's name, and read from the cache everywhere else.
Only for such a table: a cache is one more thing that can be stale. It is filled at start-up by a hosted service,
never in a constructor:

```csharp file=SmallTableCacheLoader.cs
public class SmallTableCacheLoader<TContext, TEntity>(IServiceScopeFactory _scopeFactory, IMemoryCache _cache, ILogger<SmallTableCacheLoader<TContext, TEntity>> _logger)
    : IHostedService
    where TContext : DbContext
    where TEntity : class
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        IReadOnlyList<TEntity> rows = await dbContext.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

        _cache.Set(typeof(TEntity).Name, rows);
        _logger.LogInformation("Cached {Count} {Table} rows", rows.Count, typeof(TEntity).Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Register `services.AddMemoryCache()` on the line before
`services.AddHostedService<SmallTableCacheLoader<FokusDbContext, TEntity>>()`: a plain host registers no cache, and
without it the service cannot start. A reader takes `IMemoryCache` and reads the table through one shared helper,
which fails when the entry is missing:

```csharp file=CachedTableExtensions.cs
public static class CachedTableExtensions
{
    public static IReadOnlyList<TEntity> GetCachedTable<TEntity>(this IMemoryCache cache) =>
        cache.Get<IReadOnlyList<TEntity>>(typeof(TEntity).Name)
            ?? throw new InvalidOperationException($"The {typeof(TEntity).Name} table is not cached; the start-up cache loader did not run.");
}
```

## Interceptors

One line in the persistence registration attaches the save interceptor (above). `create-domain-event-handler` covers
the handlers the published events reach.
