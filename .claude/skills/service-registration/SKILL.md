---
name: service-registration
description: Within-service registration — one DependencyInjection class per layer (API, Application, Persistence), what goes where, the one-line registration forms and lifetimes, settings bound first, the start-up file's three regions and middleware order, and how a host composes several services. This repo's form (AddFokusServices, AddFokusApplication, AddFokusPersistence; the clock registered once; the modules' own registrations) is stated first. Use when registering a dependency, choosing its layer or lifetime, writing or ordering a start-up file, or composing services into one host.
user-invocable: true
---

# Service Registration

Where each dependency of a service is registered, in which form and with which lifetime, and how the start-up file
reads. Registration is written out line by line, so a reader sees what runs.

## Assumes

- **ASP.NET Core's service collection** and the options system, with settings bound so a missing value fails at
  start-up.
- **The service's projects** — API, an Application project where the service has one, Persistence.
- **The service's own framework** (FastEndpoints, Carter or minimal APIs) and, where it publishes or consumes
  events, a message bus (MassTransit samples below).
- No shared library: every registration below is a plain framework call.

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

## This Repo's Form

Fokus is one self-hosted service with three layers and two modules. What applies here, and which rules below it
departs from:

- **The layer methods keep this repo's names** — `AddFokusServices` (API, `src/Services/Fokus/Fokus.API/DependencyInjection.cs`),
  `AddFokusApplication` (Application, `src/Services/Fokus/Fokus.Application/DependencyInjection.cs`) and
  `AddFokusPersistence` (Persistence, `src/Services/Fokus/Fokus.Persistence/DependencyInjection.cs`) — not the
  `Add{Layer}Services` names of § Naming Conventions. `Program.cs` calls them in the order API, Application,
  Persistence.
- **The Application layer** (`Fokus.Application`) holds injected capability classes and no mediator: the analytics
  calculators of `Fokus.Domain/Analytics` (Domain has no registration of its own), the loaders of the fat endpoints,
  and the two sync classes around another system. Each is one scoped line in `AddFokusApplication`. Static
  helpers of the shared analytics kernel are not registered.
- **The clock** is registered once, `services.TryAddSingleton(TimeProvider.System)`, in `AddFokusServices`.
- **The API layer** also registers sign-in (`AddFokusAuth`, which takes the environment — a departure from "never
  takes the environment"), FastEndpoints, the OpenAPI document, SignalR, and the two modules: `AddRestApiJira`
  (`src/Modules/Jira/Jira.RestApi/DependencyInjection.cs`, a Refit client) and `AddGraphQLXray`
  (`src/Modules/Xray/Xray.GraphQL/DependencyInjection.cs`).
- **Settings:** there is no `ConfigureApiOptions`. Each module binds its own settings in its registration
  (`JiraOptions` with `ValidateDataAnnotations().ValidateOnStart()`; `XrayOptions` bound without validation).
- **Persistence:** the domain-event publisher (`Blocks.FastEndpoints/DomainEventPublisher.cs`) and the save
  interceptor (`DispatchDomainEventsInterceptor`) are scoped lines in `AddFokusPersistence`; the data context reads
  `ConnectionStrings:Database` and uses SQLite; each repository is one scoped line.
- **Known departures from the one-line rule:** the modules forward interfaces with lambdas (`IXrayClient` to
  `GraphQLXrayClient`; `IJiraClient` built per call from `JiraOptions.IsTeamManaged`), and `XrayBearerTokenManager`
  and `XrayRateLimiter` are singletons holding a token cache and a rate window. They predate this skill.
- **`Program.cs`** has no regions: it calls `AddFokusServices`, `AddFokusApplication` and `AddFokusPersistence`;
  then `MigrateFokusDatabase`, `SeedDefaultAppSettingsAsync` and `ApplyConfiguredXrayCredentialsAsync`; then the
  error middleware, static files, `UseFokusAuth`, `UseFokusMiddleware` (FastEndpoints with the active-user
  pre-processor; the OpenAPI document, Swagger and Scalar in development only), the SignalR hub and the SPA
  fallback. `UseFokusAuth` and `UseFokusMiddleware` extend `WebApplication` — a departure from § Naming Conventions.
- **No host composition, no message bus, no background service** today; those sections apply to a service that
  adds them.

## Layer Structure

One `DependencyInjection` class per project, each with one method `Add{Layer}Services(services, configuration)`:

```text
ConfigureApiOptions(services, configuration)        settings, first, as its own statement
AddApiServices(services, configuration)             API project
AddApplicationServices(services, configuration)     Application project, where the service has one
AddPersistenceServices(services, configuration)     Persistence project
```

The service's composition calls them in that order — settings first, then API, Application, Persistence — and the
host that runs the service calls the composition. A service with no Application project skips that layer. Where
the repo has modules, they have their own composition, called by the host the same way.

## What Goes Where

### API layer (`AddApiServices`)

- The endpoint framework (`AddFastEndpoints`, `AddCarter`) and the OpenAPI document
- Authentication, authorization and the authorization handlers (`authorization-patterns`)
- The request context and its readers, where the service has them
- The host's JSON settings, once
- gRPC clients and the gRPC server (`create-grpc-contract`); typed HTTP clients
- File storage, e-mail and other outside systems
- The clock, `TimeProvider`
- In a service with no Application project: the bus and the event publisher too
- In a service a host runs, the endpoint framework, the OpenAPI document and sign-in are registered by the host

### Application layer (`AddApplicationServices`)

- The mediator, its handlers and its open behaviours in order: identity stamp, validation, logging — in a mediator
  service
- The validators — in a mediator service
- Capability classes the endpoints or handlers call: domain calculators, loaders, classes around another system
- The service's consumers — in a self-hosted service with the bus; in a hosted service the host registers the bus
  and calls the service's `Add{Service}Consumers`; the domain-event publisher
- Factories of per-call objects (a state machine factory); the resource access checker

### Persistence layer (`AddPersistenceServices`)

- The data context, with its save interceptor; it reads its connection string itself
- Each repository and read store, scoped, one line each (`persistence-patterns`)
- The cache loader of a small, stable table, where one is cached

Middleware lives in a shared `Blocks.*` library or the API project's `Middlewares` folder and takes only injected
services.

## Registration Patterns

- **One line per class:** `services.Add{Lifetime}<IFoo, Foo>()`, or `services.Add{Lifetime}<Foo>()` where the class
  has no interface. No lambda that forwards an interface to another registration, and no `new Foo(…)` lambda for
  the repo's own class. Where a class still holds shared state, a forwarding line may keep its one shared instance
  until that state moves to a named cache. Two exceptions build in a lambda because nothing else can: a factory
  delegate that builds an object per call from run-time values (a state machine factory), and a second instance of a
  type told apart by its marker, built from that marker's own settings (a second file store).
- **Scoped by default.** State that outlives one request lives only in a named cache, added in the same change as
  the class. A singleton that is not such a cache holds no state. Working state of one request is not meant by this
  rule.
- **Which implementation runs is one visible line.** A setting never selects a stand-in: tests and local runs swap
  the implementation in their own set-up. The exception is a provider the deployment chooses by design; its
  registration says so.
- **A second instance of a type is told apart by a marker type** (a subclass, or a generic marker), not by a key.
  A choice: where a library already tells its clients apart by key, keyed registrations stay for that library.
- **Explicit, not scanned.** A service registers its own classes line by line; it does not scan its assembly for
  repositories, services or mappings. Discovery a framework does for its own types stays: the endpoint framework
  finding its endpoints, validators and summaries; the mediator finding its handlers; the validation library finding
  its validators; the bus finding its consumers; the data context applying its table configurations. A choice:
  scanning hides what is registered, and a line per class costs nothing when an agent writes it.
- **A background service** is one `services.AddHostedService<T>()` line and starts no discarded task
  (`add-hosted-service`).
- **The clock is one line,** `services.TryAddSingleton(TimeProvider.System)`, in the API registration (or a worker
  host's registration where there is no API): every class that needs the time takes `TimeProvider`, and the host
  registers none by itself. `TryAdd` keeps a clock a test registered first.

### Data context

```csharp
services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
services.AddDbContext<OrdersDbContext>((provider, options) => options
    .UseSqlServer(configuration.GetRequiredConnectionString("Orders"))
    .AddInterceptors(provider.GetServices<ISaveChangesInterceptor>()));
services.AddScoped<OrderRepository>();
services.AddScoped<CustomerRepository>();
services.AddScoped<OrderReadStore>();
```

The save interceptor is `persistence-patterns`'; it publishes domain events after a successful save. In this repo the
provider is SQLite (`UseSqlite(configuration.GetConnectionString("Database"))`).

### Mediator (mediator style)

```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
    config.AddOpenBehavior(typeof(StampActionBehavior<,>));
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
```

### Message bus

One call registers the bus and finds the consumers; its settings come from the bound options:

```csharp
services.AddMassTransit(bus =>
{
    bus.AddConsumers(typeof(DependencyInjection).Assembly);
    bus.UsingRabbitMq((context, rabbit) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        rabbit.Host(options.Host, options.VirtualHost, host =>
        {
            host.Username(options.UserName);
            host.Password(options.Password);
        });
        rabbit.ConfigureEndpoints(context);
    });
});
```

A self-hosted service registers the bus as shown; a hosted one contributes `Add{Service}Consumers` instead.

## Options Validation

All settings of a service are bound in one method, `ConfigureApiOptions`, called first and as its own statement; it
returns nothing. It sits in the registration of the service whose settings they are. The form matters little; the
point is that settings come first and in one place.
`ConfigureApiOptions` is called by the service's composing class (§ Composing services into one host).

```csharp file=OptionsRegistration.cs
public static class OptionsRegistration
{
    public static void ConfigureApiOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAndValidateOptions<JwtOptions>(configuration)
            .AddAndValidateOptions<RabbitMqOptions>(configuration);
    }
}
```

## Program.cs Structure (self-hosted service)

The start-up file has three regions — Add, InitData, Use — and no registration logic of its own: each line calls a
registration or an extension.

- **The error middleware comes early:** after HTTPS redirection, before routing and sign-in, so it also catches a
  sign-in failure. The request-context and request-diagnostics middlewares, where used, follow it directly, in that
  order.
- **The route prefix is set once,** on the route group or the endpoint framework's configuration; an endpoint
  declares only the rest of its route.
- **The API reference pages are served in development only.**
- **Table set-up runs at start-up, test data is seeded in development only** (`persistence-patterns`).

```csharp file=Program.cs
var builder = WebApplication.CreateBuilder(args);

#region Add
builder.Services.AddOrdersServices(builder.Configuration);
#endregion

var app = builder.Build();

#region InitData
app.Migrate<OrdersDbContext>();
if (app.Environment.IsDevelopment())
    app.SeedTestData();
#endregion

#region Use
app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestContextMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapAllEndpoints();
#endregion

app.Run();
```

In a FastEndpoints service the last mapping line is `app.UseFastEndpoints(config => …)`, which sets the route prefix
and the serializer's JSON settings; keep the framework's default JSON casing.

## Composing services into one host

Where one host runs several services, each service keeps one composing class, called by the host — the one place a
registration is named for its service. It binds the service's settings first, then calls the service's layers in
order, and nothing else:

```csharp file=OrdersRegistration.cs
public static class OrdersRegistration
{
    public static IServiceCollection AddOrdersServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureApiOptions(configuration);

        return services
            .AddApiServices(configuration)
            .AddApplicationServices(configuration)
            .AddPersistenceServices(configuration);
    }
}
```

The host itself — its project, its start-up file, the registrations it owns and the start-up order — is a separate
decision; Fokus has no host.

## Naming Conventions

| Item | Convention |
|------|-----------|
| Registration class | `DependencyInjection` (one per project) or `{Subject}Registration` (a provider or concern) — never `…Module` |
| Layer methods | `AddApiServices`, `AddApplicationServices`, `AddPersistenceServices` — named for the layer, never the service (this repo keeps `AddFokus{Layer}`, § This Repo's Form) |
| Settings | `ConfigureApiOptions`, returns nothing |
| Bus contribution | `Add{Service}Consumers` extends `IBusRegistrationConfigurator` — the one registration method on a type other than `IServiceCollection` |
| A registration class holds | only `Add…` and `Configure…` methods; nothing calls it at run time, and it never takes the environment |
| `Use…` and `Map…` | in a separate `Extensions` class, extending the narrowest type (`IApplicationBuilder`, `IHost`, `IEndpointRouteBuilder`) — never `WebApplication` |
| Parameters | `(this IServiceCollection services, IConfiguration configuration)` — `configuration`, never `config`, which collides with the libraries' own lambda parameter |
| Return type | `IServiceCollection`, for chaining (`ConfigureApiOptions` apart) |
| A middleware's request parameter | `context`; `httpContext` when the method also takes a second context object; never an abbreviation |

## When to Load This Skill

- Adding a dependency to an existing service, or choosing its lifetime
- Creating a service (beside `create-service`)
- Writing or reordering a start-up file
- Composing services into one host
