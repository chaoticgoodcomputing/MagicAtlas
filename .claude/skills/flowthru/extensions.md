# Extensions

Flowthru Core gives you the model — schemas, catalog items, steps, flows ([flow-developers.md](flow-developers.md), [catalog-developers.md](catalog-developers.md)). Everything **stack-specific** is an extension: file formats, databases, remote media, execution engines, and diagnostics sinks. Each extension is a NuGet package (`Flowthru.Extensions.<Stack>`), enabled with a `b.UseXxx()` call in `Program.cs`, and **each has its own deep skill**.

## How to use this index

This page is a **capability menu**, not a tutorial. Use it two ways:

1. **"Can Flowthru already do X?"** — scan the table. If an extension covers it, you know the package to add and the axis it sits on (format / medium / database / engine / metadata).
2. **"This project uses X — deepen my knowledge."** — pull that extension's skill. The one-liner here is only enough to recognize the capability; the extension skill carries the real usage, configuration, gotchas, and diagnostics.

**Detect what this project already uses:** read the `.csproj` for `Flowthru.Extensions.*` `PackageReference`s, and the `b.UseXxx()` calls in `Program.cs`. For any of them (or any you're about to introduce), pull the deep skill:

```bash
npx skills add chaoticgoodcomputing/flowthru --skill flowthru-<ext>
```

Treat each pulled extension skill as an extra bit of competence matched to this project's stack.

<!-- The catalog below is GENERATED from each extension's SKILL.md frontmatter by
     scripts/generate-skill-extensions.mjs. Do not edit between the markers by hand;
     edit the per-extension src/extensions/<Ext>/SKILL.md and regenerate. -->
<!-- flowthru:extensions:start -->
### Formats — how bytes serialize

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.Csv` | Text, delimited on-disk format for row sequences — the raw-edge default when an external producer dictates CSV. | `— (declare a .Csv() item)` | `--skill flowthru-csv` |
| `Flowthru.Extensions.Excel` | Read-only .xlsx worksheet format for row sequences — ingest a named sheet at the raw edge; cannot be written. | `— (declare a .Excel() item)` | `--skill flowthru-excel` |
| `Flowthru.Extensions.Parquet` | Columnar, compressed on-disk format for large typed row sets — the default for intermediate and primary data. | `— (declare a .Parquet() item)` | `--skill flowthru-parquet` |
| `Flowthru.Extensions.Xml` | Document-mode XML format for a whole object per file — nested trees like config, manifests, and coverage reports, not row sequences. | `— (declare a .Xml() item)` | `--skill flowthru-xml` |

### Media — where bytes live

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.AWS.S3` | Read/write any-format Item against s3://bucket/key — a remote object medium with atomic writes and a pre-flight write-probe; format is untouched. | `b.UseS3(…) / b.UseLocalS3(…)` | `--skill flowthru-s3` |
| `Flowthru.Extensions.Google.Sheets` | A Google Sheets tab as a typed Item, addressed by (spreadsheetId, tableName) — read source rows, write derived rows back, offline or live. | `b.AddGoogleSheets(…)` | `--skill flowthru-google-sheets` |
| `Flowthru.Extensions.GQL` | Read (and optionally write) Catalog Items against a GraphQL API via a StrawberryShake client — an Item is a named handle on a query, load runs the operation and a projection pulls rows out. | `— (declare a .Gql() item; UseGql() only caps concurrency)` | `--skill flowthru-gql` |
| `Flowthru.Extensions.Http` | Read any-format Item from an http(s):// URL — a remote read medium with conditional-GET caching; format is untouched. | `b.UseHttp(…)` | `--skill flowthru-http` |

### Databases

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.EFCore` | Back Catalog Items with a relational DB via an EF Core DbContext — as a table, single entity, or deferred query. | `AddDbContextFactory(…) + b.UseEFCore()` | `--skill flowthru-efcore` |
| `Flowthru.Extensions.EFCore.Bulk` | Swap an EF Core Item's per-row save for a provider-native bulk-copy — Insert, TruncateAndInsert, InsertOrUpdate, or InsertOrUpdateOrDelete. | `.WithSave(BulkSave.…) on an EF Core item` | `--skill flowthru-efcore-bulk` |
| `Flowthru.Extensions.EFCore.Npgsql` | Declare a table Item with .NpgsqlTable and AddBulkTransfer promotes Postgres-to-Postgres as a binary COPY passthrough — no row materialised in .NET. | `.NpgsqlTable(…) + flow.AddBulkTransfer(…)` | `--skill flowthru-efcore-npgsql` |

### Execution engines

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.DuckDB` | Run a wide transform as SQL in the embedded DuckDB engine between Parquet Items; rows never enter .NET, CLR memory stays flat. | `b.UseDuckDb(…)` | `--skill flowthru-duckdb` |

### Step hosts

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.Python` | Run Python (pandas/scikit-learn) functions as typed Steps; rows cross the boundary as Arrow → pandas.DataFrame. | `b.UsePython(…)` | `--skill flowthru-python` |

### Testing

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.FUnit` | Inline step tests beside the Step they exercise — typed Invoke + sample-data helpers; `dotnet test` discovers them via source-generated runners. | `— (tests compile behind FUNIT_ENABLED)` | `--skill flowthru-funit` |

### Metadata & diagnostics

| Package | Capability | Enable | Deep skill |
|---------|-----------|--------|-----------|
| `Flowthru.Extensions.Metadata.Diagnostics` | Curated post-run diagnostic providers — step timings and a run summary by default, opt-in row counts and output-existence audit. | `meta.UseDiagnostics()` | `--skill flowthru-metadata-diagnostics` |
| `Flowthru.Extensions.Metadata.Json` | Serializes the planned DAG and run result to JSON — a pre-run manifest and a post-run result file per Flow. | `meta.AddJsonMetadata(…)` | `--skill flowthru-metadata-json` |
| `Flowthru.Extensions.Metadata.Mermaid` | Draws the planned DAG and a colour-coded run-result diagram as Mermaid Markdown — renders anywhere Mermaid is supported. | `meta.AddMermaidMetadata(…)` | `--skill flowthru-metadata-mermaid` |
<!-- flowthru:extensions:end -->
