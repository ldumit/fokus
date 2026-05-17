---
name: tdd
description: Test-driven development — red-green-refactor loop, one vertical slice at a time. Loaded by developer agent during implementation steps that have testable behavior.
---

# TDD

Test-driven development using red-green-refactor, one vertical slice at a time. Each cycle: write ONE test, see it fail (red), write minimal code to pass (green), refactor only when green.

## When to Use

- Plan step specifies "with tests" or "test coverage"
- Implementing domain logic with clear input/output contracts
- Adding a new endpoint with defined request/response behavior
- Fixing a bug (write the regression test FIRST — see `diagnose` skill Phase 5)
- User requests TDD explicitly

## When NOT to Use

- Scaffolding/wiring (DI registration, project setup, EF config) — no behavior to test
- UI layout/styling — visual, not behavioral
- One-shot scripts or migrations

## Anti-Pattern: Horizontal Slicing

**Never** write all tests first, then all implementation. This produces tests that verify shape (structure) rather than behavior (what happens when).

```
BAD:  Write 5 tests → implement all → all green
GOOD: Write 1 test → implement → green → write next test → implement → green
```

## Workflow

### Step 0: Bootstrap (first time only)

If no test project exists for the service:

**Backend (.NET):**
```
src/Services/{Svc}/{Svc}.Tests/
  {Svc}.Tests.csproj    ← xUnit + FluentAssertions + NSubstitute
  GlobalUsings.cs
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="FluentAssertions" Version="8.*" />
    <PackageReference Include="NSubstitute" Version="5.*" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\{Svc}.API\{Svc}.API.csproj" />
  </ItemGroup>
</Project>
```

Register in solution: `dotnet sln add src/Services/{Svc}/{Svc}.Tests/{Svc}.Tests.csproj`

**Frontend (Vue/TypeScript):**
```
client/vitest.config.ts
client/src/**/*.spec.ts   ← co-located with source
```

Add to `package.json`: `vitest`, `@testing-library/vue`, `@vue/test-utils`

### Step 1: Plan the Slice

Before writing code, identify the **behavior** to test — not the implementation:
- What does the caller send in?
- What comes back (or what side effect occurs)?
- What's the one edge case that matters most?

Write it as a sentence: "When {input}, it should {behavior}."

### Step 2: Red — Write One Failing Test

```csharp
[Fact]
public async Task Should_{expected_behavior}_When_{condition}()
{
    // Arrange — set up inputs and dependencies
    // Act — call the unit under test
    // Assert — verify the ONE behavior
}
```

**Run:** `dotnet test` — confirm it fails for the RIGHT reason (not a compile error, not a wrong assertion — the actual behavior is missing).

If it fails for the wrong reason: fix the test setup, not the production code.

### Step 3: Green — Minimal Implementation

Write the **minimum** code to make the test pass. No more. Hardcoding is acceptable if only one test exists — the next test will force generalization.

**Run:** `dotnet test` — green.

### Step 4: Refactor (only when green)

With all tests passing, improve the code:
- Remove duplication introduced by the minimal implementation
- Extract methods if a block is doing two things
- Rename for clarity

**Rules:**
- Never refactor while red
- Run tests after each refactoring move — stay green
- Don't anticipate future tests during refactoring

### Step 5: Next Slice

Return to Step 1 with the next behavior. Each cycle should take 5–15 minutes.

## What to Test (and at which level)

| Layer | Test Through | Mock |
|-------|-------------|------|
| Domain aggregate methods | Direct call | Nothing — pure logic |
| Domain services | Direct call | Repository (if data-dependent) |
| Endpoint handlers | `WebApplicationFactory` HTTP call | External services (gRPC clients, MassTransit) |
| Vue composables | Direct import + invoke | API module (msw or manual mock) |
| Vue components | `@vue/test-utils` mount | Store (provide mock), API (msw) |

## Mocking Rules

Mock **only at system boundaries:**
- External HTTP APIs (gRPC clients, REST clients)
- Message bus (MassTransit publish/consume)
- Time (`TimeProvider`)
- File system (if used)

**Never mock:**
- Internal classes within the same service
- Repositories when testing through WebApplicationFactory (use real SQLite)
- Domain logic (it's pure — test it directly)

## Test Naming

```
Should_{ExpectedBehavior}_When_{Condition}
```

Examples:
- `Should_ReturnActiveIssues_When_SprintIsInProgress`
- `Should_ThrowDomainException_When_TransitionInvalid`
- `Should_PublishDomainEvent_When_StatusChanges`

## Test Organization

```
{Svc}.Tests/
  Domain/
    {Aggregate}Tests.cs          ← aggregate method tests
  Features/
    {Area}/
      {Feature}EndpointTests.cs  ← integration tests via WebApplicationFactory
  _Fixtures/
    WebAppFixture.cs             ← shared WebApplicationFactory setup
    TestData.cs                  ← builder methods for test entities
```

## Integration with Developer Workflow

TDD is HOW the developer implements plan steps, not a separate phase:

1. Developer reads plan step
2. Developer identifies the testable behavior in that step
3. TDD loop (red-green-refactor) until the step's acceptance criteria are met
4. Move to next plan step

**Not every plan step needs TDD.** Steps that are pure wiring (DI, config, EF migration) skip directly to implementation. Steps with business logic or request/response contracts use TDD.

## Guardrails

- **One test at a time** — never write the next test until the current one is green
- **Test behavior, not implementation** — if refactoring internals breaks tests, the tests are wrong
- **No test-only abstractions** — don't introduce interfaces solely for testability when you can use WebApplicationFactory with real dependencies
- **Delete tests that test nothing** — a test that passes regardless of implementation is worse than no test
- **Integration over unit for endpoints** — prefer one `WebApplicationFactory` test over mocking 5 internal collaborators

## What This Skill Does NOT Do

- Set up CI/CD test pipelines — that's infrastructure
- Guide load/performance testing — different concern
- Handle flaky test investigation — see `diagnose` skill
- Make architectural decisions about test boundaries — that's the architect's job
