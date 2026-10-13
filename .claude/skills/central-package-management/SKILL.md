---
name: central-package-management
description: Introduces or audits Central Package Management — one Directory.Packages.props at the solution root holding every version, bare PackageReference lines in project files, the pin rule, and the three-form verification search. Use when setting up central package management, bumping a version, or auditing a solution for stray version declarations.
user-invocable: true
---

# Central Package Management

One `Directory.Packages.props` at the solution's `src/` root declares every package version; project files carry
bare `<PackageReference Include="…" />` lines with no version.

## Assumes

- **A .NET solution** built with MSBuild and NuGet — here `src/Fokus.slnx`.
- The shared libraries (`src/BuildingBlocks/Blocks.*`) and the modules (`src/Modules/*`) are projects of the same
  solution, so they take their versions from the same props file as the service. Each package is at its current
  stable version unless a pin keeps it back.

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

## The CPM Convention

- **One version of each package, set once,** in `src/Directory.Packages.props`, with
  `ManagePackageVersionsCentrally` on. A `<PackageReference>` carries no version.
- **The version is current stable** — never a number copied from another project's file.
- **A pin.** A package the dependency graph would otherwise lift to a newer version, or one held on its major on
  purpose, stays pinned in the props file with a one-line comment directly above it giving the reason. That comment
  is the only comment a package file carries.

## When to Use

- Setting up central management on a solution whose project files carry versions.
- After a version bump, to confirm no stray version remains.
- Beside `framework-currency` when moving to the latest stable versions.

## The Props File

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="FastEndpoints" Version="{current stable}" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="{current stable}" />
    <!-- held on this major by owner decision; the upgrade is open work -->
    <PackageVersion Include="Refit.HttpClientFactory" Version="{the pinned version}" />
  </ItemGroup>
</Project>
```

- `ManagePackageVersionsCentrally` — turns central management on; a project file that declares a version fails the
  build.
- `CentralPackageTransitivePinningEnabled` — also pins transitive dependencies, so no graph lifts a package
  silently.
- `{current stable}` is the latest stable version on NuGet the day the line is written — read it there, not from
  memory. A preview or a floating version (`*`) is never written.

## Bare References in `.csproj`

Every `<PackageReference>` has no `Version` attribute and no `<Version>` child; other attributes and children stay:

```xml
<ItemGroup>
  <PackageReference Include="FastEndpoints" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Project files set nothing beyond the basics: no documentation-file generation, no warning suppressions (except the
one a pinned package needs), no comments.

## Migration Recipe

1. **Collect every version** — all three forms of the search below; record each package and its version.
2. **Write `src/Directory.Packages.props`** with one `<PackageVersion>` per package, at its current stable version
   unless a pin with a reason keeps it back.
3. **Strip every version from every project file** — the `Version` attribute, the `<Version>` child, and
   `VersionOverride` (keep an override only with a stated reason, as a pin).
4. **Run the search** again — zero hits in project files.
5. **Build** — `dotnet build src/Fokus.slnx`.

## Verification Search — Three Forms

```bash
grep -rn --include="*.csproj" 'Version=' src/             # an inline Version attribute
grep -rn --include="*.csproj" '<Version>' src/            # a Version child element
grep -rn --include="*.csproj" 'VersionOverride=' src/     # an override — legitimate only as a stated pin
```

All three return zero hits in project files; `Directory.Packages.props` declares versions by design. (A
`src/**/*.csproj` pattern matches nothing where the shell has no globstar, so use `--include`.)

## Nested Props Check — expect exactly one

MSBuild picks the **nearest** `Directory.Packages.props`, so a nested one silently overrides the root for the
projects under it — and the three-form search cannot see it. Count the files:

```bash
find src -name Directory.Packages.props -not -path '*/obj/*' -not -path '*/bin/*'
```

Exactly one hit, `src/Directory.Packages.props`. Fold any nested file into the root, or state why it stays.

## Interplay with `framework-currency`

A bump can bring a breaking change through a transitive package beyond the one bumped. Build and run after every
bump; `framework-currency` owns the currency rule.

## What This Skill Does NOT Cover

- Which version to move to — read the package's release notes.
- Transitive resolution beyond transitive pinning — the NuGet documentation.
- The target framework (`<TargetFramework>`) — `framework-currency`.
