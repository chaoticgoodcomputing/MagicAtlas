# Catalog Developers

Read this when **declaring schemas or catalog items** — the typed data definitions a Flow reads and writes. For Steps and Flows, see [flow-developers.md](flow-developers.md); for the formats, media, and databases you can back an item with, see [extensions.md](extensions.md).

## Schemas: the shape of a record

A schema is the typed shape of one record — a `[FlowthruSchema] public partial record`:

<!-- flowthru:snippet:docs:schema-minimal:start -->
```csharp
[FlowthruSchema]
public partial record NameSchema
{
  /// <summary>
  /// A person's name.
  /// </summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }
}
```
_(source: [`Minimal/NameSchema.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Minimal/Data/_01_Raw/Schemas/NameSchema.cs))_
<!-- flowthru:snippet:docs:schema-minimal:end -->

- **`partial` is required** — a source generator adds members, and omitting `partial` is a design-time error (FT1001).
- **`[SerializedLabel("ext_name")]`** aliases a property to its external serialized name (a CSV header, JSON key, column). Without it, the property name is used verbatim.
- **`[SerializedEnum]`** serializes an enum as its string name rather than its numeric value.
- Prefer `required … { get; init; }` properties — records are immutable data, and `required` makes a missing field a compile error at every construction site.

## Catalog items: typed handles to stored data

A **catalog item** is a typed handle to a piece of stored data — `IItem<T>`, where `T` is the schema (or a collection of schemas). The mechanism: a catalog is any class deriving `CatalogAbstract`, and each item is a property returning `CreateItem(() => Item.Of<T>("label")…​.Build())`:

<!-- flowthru:snippet:docs:catalog-raw-companies:start -->
```csharp
/// <summary>Raw company data imported from external sources.</summary>
public IItem<IEnumerable<CompanySchema>> Companies =>
  CreateItem(() => Item.Of<IEnumerable<CompanySchema>>("Companies")
    .Csv()
    .AtPath($"{_basePath}/_01_Raw/Datasets/companies.csv")
    .Build());
```
_(source: [`Spaceflights/Catalog.Raw.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Spaceflights/Data/_01_Raw/Catalog.Raw.cs))_
<!-- flowthru:snippet:docs:catalog-raw-companies:end -->

The `"label"` is the item's identity in the DAG and diagnostics; keep it stable. Many projects split the catalog across `partial class` files (one per data layer) — that's starter convention, not a requirement; a single catalog class works identically.

## The three storage axes

An item is defined by three independent axes — read them off the fluent chain above:

| Axis | Question it answers | How it's expressed | Where it comes from |
|------|--------------------|--------------------|---------------------|
| **Container** | What in-memory shape? | `IItem<IEnumerable<T>>` = many rows; `IItem<T>` = a singleton. | Core |
| **Format** | How do bytes serialize? | `.Json()`, `.Csv()`, `.Excel().WithSheet(...)`, `.Parquet()`, … | **Extensions** ([extensions.md](extensions.md)) |
| **Medium** | Where do the bytes live? | `.AtPath(...)` — a local path, or an `https://…` / `s3://…` URI routed through a registered medium. | Core (filesystem) + **Extensions** (Http, S3) |

These axes are orthogonal: a Parquet format doesn't know about HTTP, and the HTTP medium doesn't know about Parquet — they meet through the resolver. So "read a Parquet file over HTTP" is just `.Parquet().AtPath("https://…")` once `UseHttp()` is registered. **JSON is built into Core; every other format and non-filesystem medium comes from an extension** — check [extensions.md](extensions.md) for what's available and which package/`UseXxx()` to add.

## Configuration-bound items

A step's options are modeled as a catalog item too, bound from `appsettings.json` so a config change invalidates the affected downstream cache automatically:

<!-- flowthru:snippet:docs:item-config:start -->
```csharp
public IItem<SplitAndEncodeStep.Options> SplitOptions =>
  CreateItem(() =>
    Item.Of<SplitAndEncodeStep.Options>("SplitOptions")
      .FromConfiguration(_configuration)
      .AtSection("Flowthru:Flows:DataEngineering:SplitOptions")
      .Build());
```
_(source: [`IrisFUnit/Catalog.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/IrisFUnit/Data/Catalog.cs))_
<!-- flowthru:snippet:docs:item-config:end -->

For this to resolve, the host registers the configuration with `b.UseConfiguration(configuration)` and the catalog's constructor receives the `IConfiguration`.

## Two constructor forms

You'll see two ways to construct an item; **both are valid — match the form already used in this project.**

- **Fluent (canonical):** `Item.Of<T>("label").<Format>().AtPath(...).Build()` — reads as container → format → medium.
- **Factory:** `ItemFactory.<Container>.<Format><T>(...)` — e.g. `ItemFactory.Enumerable.Parquet<Row>(...)`. Common where an extension ships a factory helper.

Both are wrapped in `CreateItem(() => …)` on the property. When adding to an existing catalog, mirror its prevailing style rather than mixing the two.

## Registration

The catalog registers once in the host: `b.RegisterCatalog(sp => new Catalog(basePath, …))`. From there, a flow factory receives it and references items by property (`catalog.Names`) when wiring steps — see [flow-developers.md](flow-developers.md).
