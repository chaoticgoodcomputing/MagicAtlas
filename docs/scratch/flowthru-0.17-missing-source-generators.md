sa Flowthru 0.17.x NuGet packages ship without the schema source generators

## Summary

`Flowthru.Core` 0.17.0 through 0.17.3 publish only `lib/net10.0/Flowthru.Core.dll` — they no longer include the `analyzers/dotnet/cs/Flowthru.Core.SourceGenerators.dll` payload that earlier versions (≤ 0.16.1) shipped. As a result, types annotated with `[FlowthruSchema]` are not source-generated to implement the marker interfaces (`IStructuredSerializable`, `IFlatSchema`, `INestedSchema`, `ITextSerializable`, `IBinarySerializable`), and `Item.Of<T>(...).Json().AtPath(...).Build()` fails to compile because `JsonExtensions.Json<T>` requires `T : IStructuredSerializable`.

## Reproduction

Minimal csproj on .NET 10 with no project references to the in-repo Flowthru source:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Flowthru" Version="0.17.3" />
  </ItemGroup>
</Project>
```

```csharp
using Flowthru.Data.Catalog;
using Flowthru.Data.Schema;

namespace Repro;

[FlowthruSchema]
public partial record AtlasPoint
{
    [SerializedLabel("card_id")] public required Guid CardId { get; init; }
    [SerializedLabel("x")]       public required double X { get; init; }
    [SerializedLabel("y")]       public required double Y { get; init; }
}

public partial class Catalog : CatalogAbstract
{
    private readonly string _basePath;
    public Catalog(string basePath) { _basePath = basePath; }

    public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
        CreateItem(() => Item.Of<IEnumerable<AtlasPoint>>("AtlasPoints")
            .Json()
            .AtPath($"{_basePath}/_03_Primary/Datasets/atlas-points.json")
            .Build());
}
```

`dotnet build` fails with:

```
error CS0311: The type 'IEnumerable<AtlasPoint>' cannot be used as type parameter 'T' in
the generic type or method 'JsonExtensions.Json<T>(ItemAnchor<T>)'. There is no implicit
reference conversion from 'IEnumerable<AtlasPoint>' to 'Flowthru.Data.Schema.IStructuredSerializable'.
```

## Package layout comparison

```bash
$ ls ~/.nuget/packages/flowthru.core/0.16.1/
analyzers/   build/   lib/   …

$ find ~/.nuget/packages/flowthru.core/0.16.1/analyzers -type f
analyzers/dotnet/cs/Flowthru.Core.CodeFixes.dll
analyzers/dotnet/cs/Flowthru.Core.SourceGenerators.dll

$ ls ~/.nuget/packages/flowthru.core/0.17.3/
lib/   …    # no analyzers/, no build/
```

`Flowthru.Core` 0.16.1 also ships `build/Flowthru.Core.targets` which wires the analyzer DLL via `<Analyzer Include="…/analyzers/dotnet/cs/Flowthru.Core.SourceGenerators.dll" />`. 0.17.x ships neither the targets file nor the analyzer DLL.

The meta-package `Flowthru` 0.17.3 further declares its dependencies with `exclude="Build,Analyzers"`:

```xml
<dependency id="Flowthru.Core" version="0.17.3" exclude="Build,Analyzers" />
```

So even if `Flowthru.Core` 0.17.x were re-packaged with analyzers, the `Flowthru` meta-package would suppress them by default — consumers would need an explicit `<PackageReference Include="Flowthru.Core" />` to opt in.

## Likely root cause

`src/core/Flowthru.Core.SourceGenerators/Flowthru.Core.SourceGenerators.csproj` declares:

```xml
<IsPackable>false</IsPackable>
```

The 0.16.x release apparently packed the generator DLL into `Flowthru.Core`'s `analyzers/dotnet/cs/` via a custom `Pack` target. Whatever wired that pack step appears to have been removed (or stopped firing) somewhere between 0.16.1 and 0.17.0. The `Flowthru.Core` 0.17.x nuspec has no `<files>` entry for the analyzer DLL.

## Workaround (consumer side)

Pin `Flowthru` 0.17.* but keep manual marker-interface declarations on every schema:

```csharp
[FlowthruSchema]
public partial record AtlasPoint : IStructuredSerializable, IFlatSchema
{
    [SerializedLabel("card_id")] public required Guid CardId { get; init; }
    // …
}
```

The `[FlowthruSchema]` attribute is a no-op without the source generator, but keeping it preserves the intent and means consumers get the full generated payload (incl. `ITextSerializable` / `IBinarySerializable` flat-schema markers and any future codegen) "for free" once the upstream packaging is repaired — the manual interface declarations become harmless duplicates the compiler dedupes.

## Suggested fix

Either:

1. Restore the `Pack` target on `Flowthru.Core.SourceGenerators` so the analyzer DLL lands at `analyzers/dotnet/cs/` of `Flowthru.Core` again (matching the 0.16.x layout), and ship `build/Flowthru.Core.targets` to register it. **And** drop `exclude="Build,Analyzers"` from the `Flowthru` meta-package's dependency on `Flowthru.Core` so transitive consumers get the analyzer without needing a separate `Flowthru.Core` package reference.

2. Or publish `Flowthru.Core.SourceGenerators` as a separate NuGet package (`IsPackable=true`, `DevelopmentDependency=true`) and add it as a transitive dependency of `Flowthru.Core` / `Flowthru`.

Option 1 matches the 0.16.x shape and is the least-surprise fix for existing consumers.

## Versions tested

- Affected: `Flowthru` 0.17.0, 0.17.3 (and presumably 0.17.1-preview.82 / 0.17.2-preview.83)
- Last known good before regression: `Flowthru.Core` 0.16.1 — analyzers and build targets both present
- Toolchain: .NET 10.0.4, Linux, NuGet feed `api.nuget.org`

## Resolution

Fixed in `Flowthru` / `Flowthru.Core` **0.17.4**, published on the same day this report was filed.

Verified in the local NuGet cache:

```bash
$ find ~/.nuget/packages/flowthru.core/0.17.4 -maxdepth 1 -type d
analyzers/   build/   lib/

$ find ~/.nuget/packages/flowthru.core/0.17.4/analyzers -type f
analyzers/dotnet/cs/Flowthru.Core.CodeFixes.dll
analyzers/dotnet/cs/Flowthru.Core.SourceGenerators.dll
```

The meta-package's transitive-exclusion also resolved — `Flowthru` 0.17.4's nuspec now uses `include="All"` on the `Flowthru.Core` dependency:

```xml
<dependency id="Flowthru.Core" version="0.17.4" include="All" />
```

So consumers pinning `Flowthru` 0.17.4 get the analyzer transitively without needing a separate `Flowthru.Core` package reference. Both halves of the bug are resolved.

### Downstream cleanup applied to MagicAtlas

After bumping to 0.17.4 (already covered by the floating `Version="0.17.*"` pin on `libs/atlas-flows/MagicAtlas.Flows.csproj` and `tests/atlas-flow-test/MagicAtlas.Flows.Harness.csproj`):

- Manual `: IStructuredSerializable[, IFlatSchema]` declarations on the 16 schema partial records under [libs/atlas-flows/Data/](../../libs/atlas-flows/Data/) were removed — the source generator now emits them.
- `[FlowthruStep]` annotations were added to the seven node static classes under [libs/atlas-flows/Flows/](../../libs/atlas-flows/Flows/) (previously surfaced as `FT1101` warnings the moment the source generator started running).
- Full solution build is clean: 0 warnings / 0 errors across all five projects.
