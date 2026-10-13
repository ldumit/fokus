---
name: add-hosted-service
description: Adds a background service — a periodic job or a start-up task — registered in one line, with no discarded task, start-up work kept out of constructors, a scope per pass for scoped services, and logs of its start, outcome and failures. Use when a service needs work that runs outside a request (a periodic clean-up, a poll of another system, an index built at start-up), or when moving file, network or database work out of a constructor.
---

# Add Hosted Service

Work that runs outside a request is a hosted service, registered in one line. Two shapes: a **periodic job** that
loops until the host stops, and a **start-up task** that runs once while the service starts. Work the service must
finish before it takes requests is neither: it is a line in the start-up file (§ Start-up Work).

## Assumes

- **The .NET generic host** (`BackgroundService`, `IHostedService`, `AddHostedService`) and **`TimeProvider`** as
  the clock.
- **Settings bound and validated at start-up** (`service-registration` § Options Validation) — in this repo Jira
  validates its required options at start-up (`ValidateDataAnnotations().ValidateOnStart()`,
  `src/Modules/Jira/Jira.RestApi/DependencyInjection.cs`); Xray only binds its optional options, with no start-up
  validation (`src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs`).
- The samples below are whole files of the service. A repo with a job scheduler (Quartz, Hangfire) uses the
  scheduler's job type instead and keeps every rule below. This repo has no hosted service and no scheduler today.

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

## Registration

One line, in the registration of the project that holds the class (`service-registration` § Registration
Patterns). Its settings join the service's one settings method (`service-registration` § Options Validation), and
the clock it waits on is registered once — a plain generic host registers none:

```csharp
.AddAndValidateOptions<OrderExpiryOptions>(configuration)
```

```csharp
services.TryAddSingleton(TimeProvider.System);
services.AddScoped<ExpireUnpaidOrdersHandler>();
services.AddHostedService<OrderExpiryWorker>();
```

`TryAddSingleton` keeps a clock the host or a test registered first, so a second registration of the clock never
replaces it.

- **No discarded task.** Nothing starts work with `_ = Task.Run(…)` or a call whose task is dropped: a discarded
  task loses its errors silently. The host runs the loop; `ExecuteAsync` awaits everything it starts.
- **One instance.** A hosted service is a singleton. It keeps no request state, and it resolves every scoped
  service — a handler, a repository, a typed client — from a new scope for each pass.

## Start-up Work

A constructor assigns, or builds in-memory state from its arguments; it does no file, network or database work, and
an application class reads neither the disk nor the environment. Work that must happen at start-up is one of:

- **A line in the start-up file** when the service must not take requests before it is done — a database migrated,
  a table seeded, an index created (`redis-patterns` creates its indexes this way). Each line calls an extension
  method in the project that owns the concern.
- **A start-up task** — a `BackgroundService` whose `ExecuteAsync` runs once, without a loop — when the service
  may take requests while it runs: a cache warmed, a search index rebuilt. An `IHostedService` whose `StartAsync`
  awaits the work holds the service back from taking requests just as the start-up line does; it is not this form.

**This repo:** the Fokus service's start-up work is all of the first kind, so it is start-up lines in
`src/Services/Fokus/Fokus.API/Program.cs`, one extension call per concern before the middleware: make sure the
SQLite file's folder exists and migrate; add the default settings row when the table is empty; copy configured Xray
credentials into the settings row (`persistence-patterns` § Migrations at Start-up, § Seed Data (Dual Pattern)).

```csharp file=ProductSearchIndexHostedService.cs
public class ProductSearchIndexHostedService(
    IServiceScopeFactory _scopeFactory,
    ILogger<ProductSearchIndexHostedService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product search index rebuild started");
        await using var scope = _scopeFactory.CreateAsyncScope();
        var indexer = scope.ServiceProvider.GetRequiredService<ProductSearchIndexer>();

        var count = await indexer.RebuildAsync(stoppingToken);
        _logger.LogInformation("Product search index rebuilt with {Count} products", count);
    }
}
```

A start-up task that fails is logged by the host, and by default the host stops — the service does not keep running
half-ready.

## The Periodic Job

```csharp file=OrderExpiryOptions.cs
using System.ComponentModel.DataAnnotations;

public class OrderExpiryOptions
{
    [Range(typeof(TimeSpan), "00:00:10", "1.00:00:00")]
    public required TimeSpan Interval { get; init; }
}
```

```csharp file=OrderExpiryWorker.cs
using Microsoft.Extensions.Options;

public class OrderExpiryWorker(
    IServiceScopeFactory _scopeFactory,
    IOptions<OrderExpiryOptions> options,
    TimeProvider _clock,
    ILogger<OrderExpiryWorker> _logger) : BackgroundService
{
    private readonly TimeSpan _interval = options.Value.Interval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order expiry started, every {Interval}", _interval);
        using var timer = new PeriodicTimer(_interval, _clock);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunPassAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Order expiry pass failed; the next pass runs in {Interval}", _interval);
            }
        }
    }

    private async Task RunPassAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ExpireUnpaidOrdersHandler>();

        var expired = await handler.HandleAsync(ct);
        _logger.LogInformation("Order expiry pass expired {Count} orders", expired);
    }
}
```

- **The clock is injected.** The timer takes `TimeProvider`, so a test advances time instead of waiting.
- **The loop is the job's top level.** No central error mapper stands above it, so its one `catch` logs the failed
  pass and lets the next one run. The filter lets a stop through and catches a timeout inside a pass like any other
  failure. Nothing below the loop catches (`error-handling` § Prohibitions (load-bearing)).
- **The work lives in a scoped class** named for its job, which a test calls without the host.

## Logging

A job or a hosted service logs its start, the outcome of each pass and its failures — a decision or an outcome,
never progress. It is recognised as a logging class by its name (`…HostedService`, `…Worker`, `…Job`) or its base
type. The scoped class doing the work logs nothing of its own.

## Verify

`dotnet build src/Fokus.slnx`; then search the service for `Task.Run(` and `_ = ` in front of a call (both 0 outside
tests), and for file, network or database calls in a constructor (0).

```bash
grep -rn --include="*.cs" "Task.Run(" src/Services src/Modules src/BuildingBlocks
grep -rnE --include="*.cs" "^\s*_ = [A-Za-z_.]+\(" src/Services src/Modules src/BuildingBlocks
```

## What this skill does NOT do

- **Consumers of bus messages** — the bus runs them; this repo has no message bus.
- **Seeding and index creation details** — `persistence-patterns` § Seed Data (Dual Pattern), `redis-patterns`.
- **Calling another system** — a typed HTTP client, resolved from the pass's scope; this repo's clients live in the
  modules (`src/Modules/Jira/Jira.RestApi`, `src/Modules/Xray/Xray.GraphQL`).
