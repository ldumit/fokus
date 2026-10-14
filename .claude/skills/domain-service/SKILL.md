---
name: domain-service
description: Creates a Domain-layer class for a computation that spans several aggregates — a calculator, resolver or checker named for its job, pure by default, returning domain types. This repo's form (the analytics calculators in Fokus.Domain/Analytics, the shared kernel, the clock as a parameter, registration in Fokus.Application) is stated first. Use when a calculation, a score, a derived figure or a check needs more than one aggregate and belongs in the Domain layer — including figures computed over several periods (totals, rates, trends).
user-invocable: true
---

# Domain Service

A Domain-layer class that holds a computation crossing several aggregates. Logic of one aggregate stays in that
aggregate's behaviour methods; only what genuinely spans several, or is too large for any one of them, comes here.
The name "domain service" is the pattern's; the class itself is never named `…Service`.

## Assumes

- **A Domain project** for the service, holding the aggregates and value objects the computation reads.
- **A caller that loads the data** — a handler or an endpoint — and passes it in.
- No shared library and no persistence library in the Domain project.

#### Look First

Before building anything a skill relies on — a guard helper, the central error mapper, a settings binder, an
aggregate or repository base, a save interceptor, an event publisher — look at what the repo already has: the shared
libraries under `src/BuildingBlocks/Blocks.*` (guards in `Blocks.Core`, the error family in `Blocks.Exceptions`, the
entity and aggregate bases in `Blocks.Domain`, the repository base and save interceptor in `Blocks.EntityFrameworkCore`,
the error middleware in `Blocks.AspNetCore`, the event publisher in `Blocks.FastEndpoints`), the module contracts
(`src/Modules/Jira/Jira.Contracts`, `src/Modules/Xray/Xray.Contracts`), and the packages in
`src/Directory.Packages.props`. For an analytics computation, look in `Fokus.Domain/Analytics/Shared/` first. Use
what you find. Only otherwise build it once, in the shared place: the lowest project every user of it already
references; where the users share no project, a new `Blocks.*` project each of them references
(`create-building-blocks-package`), with no reference cycle. Never a copy beside each user.

## This Repo's Form

Fokus computes its analytics with no stored figures: every analytics endpoint loads data and hands it to a
calculator. What applies here:

- **Home:** `src/Services/Fokus/Fokus.Domain/Analytics/`, one folder per area — `Sprints/`, `Developers/`, `Qa/`,
  `Workflow/` — and `Shared/` for the shared kernel. Namespaces follow the folders
  (`Fokus.Domain.Analytics.{Area}`). An area name never equals an aggregate type name (`Sprint`, `Developer`,
  `Ticket`, …): a namespace `…Analytics.Sprint` hides the `Sprint` aggregate from every `Fokus.Domain.Analytics.*`
  file (CS0118). The same names serve `Fokus.Application/Analytics/{Area}/`. Each calculator sits beside its own result records.
- **The shared kernel** (`Fokus.Domain/Analytics/Shared/`) is the one home for logic several analytics areas use:
  the transition attribution checks (`TransitionAttributionChecker`), the status-transition index, the completion
  check, the health score, the excluded-developer filter, the shared records (`SprintRef`, `SparklinePoint`,
  `TestingCrunchFlag`), and helpers such as the change direction and polarity, the bug test, business days and the
  stage index. Logic several analytics features use goes here, not into a private copy per calculator.
- **Static or instance:** a kernel helper with no state is a `static` class and is not registered. A calculator an
  endpoint calls is an instance class, registered scoped, one line, in `Fokus.Application`'s `AddFokusApplication`
  (`service-registration`).
- **The clock is a parameter:** a calculator that needs today's date takes it (`DateTime today`); the caller passes
  `timeProvider.GetUtcNow().UtcDateTime.Date`. Nothing in `Fokus.Domain/Analytics` reads `DateTime.UtcNow`.
- **Results, not replies:** a calculator returns its computation result. A reply wrapper — an HTTP mode, a
  single-sprint or multi-sprint alternative, presentation fields — is the endpoint's reply type, mapped by hand in
  `{Op}Extensions.cs` (`create-feature`, `workflows/Mappings.md`). No `System.Text.Json` attribute in the Domain.
- **Loading stays out:** the repository reads a calculator needs are made by the endpoint, or — for an endpoint
  that loads from many repositories — by one loader class in `Fokus.Application/Analytics/{Area}/` named for what
  it loads (`QaWorkloadInputs`), never `…Service`.
- **Known departure — the Jira contract in the Domain.** `Fokus.Domain` references `Jira.Contracts` (the aggregates'
  `FromJira` factories take Jira records), so "no outside shapes in" holds for the analytics calculators but not yet
  for the aggregates. Removing it is open work, outside this skill.

## Governing Principles

- **Computation in the domain** — a computation over several aggregates lives in the Domain layer and returns
  domain types or domain result records.
- **No outside shapes in** — no request, reply or contract record enters the Domain.
- **The endpoint owns its reply** — it maps the domain result to its own reply, by hand, even when the shapes match.

## When to Use

- The computation needs two or more aggregates — an order's total with the customer's discount and the current price
  list, whether stock covers every line of an order.
- It can be tested without a database.
- A figure computed over several periods — a total, a rate, a trend, a change against the prior period — is the
  same pattern: a calculator over the periods its caller loads, returning one result record per period. The
  period-specific helpers (a rolling average, a change and its direction) are private steps of that calculator,
  unless several calculators use the same one — then it lives in the shared kernel (§ This Repo's Form).

## Location

```text
{Svc}.Domain/
  {Area}/
    {Name}Calculator.cs     the class
    {Name}Result.cs         its result record, when it has one
```

A computation over several aggregates found in an API project — an `…Service` class beside the endpoints — belongs
here.

## Naming Rule

Named for its job, from the vocabulary — calculator, resolver, checker, factory, provider — never `…Service`, never
`Helper`, `Builder` or `Planner`:

| Good | Bad |
|------|-----|
| `OrderTotalCalculator` | `OrderTotalService` |
| `DeliveryDateResolver` | `DeliveryDomainService` |
| `StockAvailabilityChecker` | `StockHelper` |

## Dependencies — Pure by Default

The class works on aggregates its caller loads and passes in. No database, so a unit test builds the inputs and
asserts the result.

```csharp file=OrderTotalCalculator.cs
public class OrderTotalCalculator
{
    public OrderTotal Calculate(Order order, Customer customer, IReadOnlyList<Price> prices)
    {
        var subtotal = order.Lines.Sum(line => line.Quantity * PriceOf(line.ProductCode, prices));
        var discount = subtotal * customer.DiscountRate;

        return new OrderTotal(subtotal, discount, subtotal - discount);
    }

    private static decimal PriceOf(string productCode, IReadOnlyList<Price> prices) =>
        prices.FirstOrDefault(price => price.ProductCode == productCode)?.Amount
            ?? throw new DomainException($"Product {productCode} has no price.");
}
```

```csharp file=OrderTotal.cs
public record OrderTotal(decimal Subtotal, decimal Discount, decimal Total);
```

### Escape Hatch — Narrow Domain-Owned Read Port (performance only)

When loading everything first is impractical — a history of hundreds of periods — the class takes a **narrow read
port**: an interface declared in the Domain project, returning domain types only, named for the read it performs. It
has an interface because the database is an outside call a test must replace.

```csharp file=IOrderHistory.cs
public interface IOrderHistory
{
    Task<IReadOnlyList<OrderTotal>> GetClosedPeriodTotalsAsync(int customerId, int periodCount, CancellationToken ct = default);
}
```

Never a generic repository, an `IQueryable<T>` or a data context in the Domain. The read port's implementation is a
read store in the Persistence project, registered there (`persistence-patterns`). Fokus uses no read port today.

## Output Boundary — Endpoint Always Owns the Response DTO

The class returns domain types or domain result records, never a reply type. The endpoint maps the result to its own
reply — a reply that wraps several shapes (a summary with a list, a choice between two modes) is assembled by the
endpoint, never mirrored into the Domain.

```csharp
var total = _orderTotalCalculator.Calculate(order, customer, prices);
await Send.OkAsync(new GetOrderTotalResponse(total.Subtotal, total.Discount, total.Total), ct);
```

## Input Boundary — No DTOs Enter the Domain

Parameters are domain types, value objects, or plain numbers the computation needs (a period count, a window size,
today's date). An outside shape — a contract, another system's reply — is mapped to domain types before the class
sees it.

## DI Registration

Scoped, one line, where the service registers its application classes (`service-registration` § What Goes Where):
`services.AddScoped<OrderTotalCalculator>()` — in this repo, in `AddFokusApplication`. A read port's implementation
is registered in Persistence: `services.AddScoped<IOrderHistory, OrderHistoryReadStore>()`.

## When This Doesn't Apply — The Application-Layer Class (Contrast)

Orchestration — loading, coordinating, saving — is not this pattern. It is an endpoint, or an Application capability
class the endpoint calls (a loader, a sync class around another system), which may use repositories. This skill's
class computes; it never loads.

## Relationship to Other Skills

| Skill | Relationship |
|-------|-------------|
| `domain-patterns` | The aggregates and value objects the class reads and returns |
| `persistence-patterns` | The read store behind a read port |
| `create-feature` | The endpoint that calls the class and maps its result to the reply |
| `service-registration` | Where the class is registered |
| `analytics-computation-service` | The analytics patterns (change triplet, sparkline, rolling average) a calculator uses |

## What This Skill Does NOT Cover

- Behaviour of one aggregate — `domain-patterns`.
- Table configuration for a result record — `persistence-patterns`.
- The endpoint — `create-feature`.
