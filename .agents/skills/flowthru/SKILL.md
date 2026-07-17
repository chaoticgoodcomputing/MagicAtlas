---
name: flowthru
description: Use when authoring or modifying Flowthru data pipelines — creating flows, schemas, catalog items, or steps in a Flowthru (.NET) project. Covers the schema → catalog → step → flow model, typed flow wiring and registration, the end-to-end input-to-output workflow, and how to pull stack-specific extension deep skills (Csv, Parquet, EFCore, Python, DuckDB, Http, …). Routes to flow-developers.md, catalog-developers.md, and extensions.md.
---

# Flowthru Agent Skill

You are working inside a Flowthru data pipeline project. Flowthru is a type-safe, fail-fast data-engineering framework for .NET. You have baseline knowledge of data pipelines, DAGs, and ETL — this guide tells you how to express those concepts through Flowthru's mechanisms.

**Fail-fast is the whole philosophy.** Every failure Flowthru can catch, it catches as early as possible: *design-time* (compile errors, analyzer squigglies, fast unit tests) is the gold standard, *pre-flight* (after launch, before any step runs) is tolerable, and *runtime* is the enemy. When you write a Flow, lean into the type system — passing the wrong-typed catalog item to a step is a compile error, not a 2-a.m. ping.

## The model in four nouns

| Noun | What it is | Deep guide |
|------|-----------|------------|
| **Schema** | The typed shape of a record — a `[FlowthruSchema] partial record`. | [catalog-developers.md](catalog-developers.md) |
| **Catalog item** | A typed handle to stored data: `IItem<T>`, defined by *format × medium × container*. | [catalog-developers.md](catalog-developers.md) |
| **Step** | A pure transform: `[FlowthruStep]` with `Create(deps) => input => output`. | [flow-developers.md](flow-developers.md) |
| **Flow** | Steps wired between catalog items into a DAG (`BuiltFlow`). | [flow-developers.md](flow-developers.md) |

Read the deep guides **on demand, not eagerly**: [flow-developers.md](flow-developers.md) when writing steps or wiring flows, [catalog-developers.md](catalog-developers.md) when declaring schemas or catalog items, [extensions.md](extensions.md) when a task touches a stack-specific capability.

## Mechanism vs. convention

Flowthru requires **code shapes**, not folder layouts. The only structural requirements are:

- A schema is a `[FlowthruSchema] public partial record`.
- A catalog is a class deriving `CatalogAbstract`, exposing `IItem<T>` properties via `CreateItem(...)`.
- A step is a `[FlowthruStep] public static class` with a `Create(deps) => input => output` method.
- A flow is built with `FlowBuilder.CreateFlow(...)` and returns an immutable `BuiltFlow`.
- Flows register in the host via `services.AddFlowthru(b => b.RegisterCatalog(...).RegisterFlow(...))`.

For additional conventions and usage patterns, browse the [examples directory](https://github.com/chaoticgoodcomputing/flowthru/tree/main/examples) — `starter/` projects are cloneable templates, `advanced/` projects demonstrate production patterns and extension capabilities. The structure of these examples are conventional, but not required by the framework. **Follow the host project's existing structure**; don't impose convention on a project that doesn't use it.

## Workflow

Tasks are ordinarily: *"I have X input data, and I want Y output data."* Given the shape of the known input and desired output, work inward to define the required flows:

1. **Plan the transformation steps** — the discrete transforms from input to output.
2. **Plan the intermediate shapes** — the data between each step.
3. **Write the schemas** — see [catalog-developers.md](catalog-developers.md).
4. **Create the catalog items** referencing those schemas — see [catalog-developers.md](catalog-developers.md).
5. **Write the steps** — see [flow-developers.md](flow-developers.md).
6. **Wire the flow** — steps to items, typed end to end — see [flow-developers.md](flow-developers.md).
7. **Register the flow** in the host's `AddFlowthru(...)` block.
8. **Run and confirm** with `dotnet run` (no args) — the CLI runner discovers and executes registered flows. A dry run can be used to run only pre-flight checks, without executing any actual transformations.

## Extensions — the stack-specific menu

Core covers the model above. Everything stack-specific — file formats (Csv, Excel, Parquet, Xml), databases (EFCore), remote media (Http, S3), execution engines (Python, DuckDB), diagnostics — lives in **extensions**, and *each extension has its own deep skill*.

**[extensions.md](extensions.md) is the full menu.** Use it as a capability index: when a task needs a capability, check whether an extension already covers it. To detect what *this* project already uses, read its `.csproj` for `Flowthru.Extensions.*` `PackageReference`s and the `b.UseXxx()` calls in `Program.cs`. Each row names a deep skill (`--skill flowthru-<ext>`) — pull it for any extension the project uses or that you're about to introduce:

```bash
npx skills add chaoticgoodcomputing/flowthru --skill flowthru-<ext>   # e.g. flowthru-efcore-npgsql
```

This installs the deep skill into the project. **A skill added mid-session is not auto-loaded — read the installed `SKILL.md` now to use it**; it is then committed with the project and auto-loads in every later session. To consult a skill *without* installing, `npx skills use chaoticgoodcomputing/flowthru --skill flowthru-<ext>` prints it to stdout. The umbrella tells you *what exists*; the extension skill tells you *how to use it*.
