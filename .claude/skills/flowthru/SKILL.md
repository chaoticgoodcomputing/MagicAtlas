---
name: flowthru
description: Use when authoring or modifying Flowthru data pipelines — creating flows, schemas, catalog items, or steps in a Flowthru project. Covers project structure (the layered Data/ directories from _01_Raw to _08_Reporting), the catalog/schema/step/flow model, and the end-to-end workflow for going from input data to output data.
---

# Flowthru Flow Agent Guide

You are working inside a Flowthru data pipeline project. Flowthru is a type-safe data engineering framework for .NET. You have baseline knowledge of data pipelines, DAGs, and ETL — this guide tells you how to express those concepts in Flowthru's structure.

Exact syntax for schemas, catalog items, steps, and flows can be gleaned from the existing code in this project. **The canonical reference is `examples/starter/KedroIrisFUnit/`** — when in doubt, model new code on that example's shape.

## Project Structure

```
Program.cs                          # Entrypoint — flows are registered here
                                    # Project runs as a Flowthru CLI service via `dotnet run`
Data/                               # Everything related to data definitions
├── _01_Raw/                        # Immutable source data — never mutated, only copied from
│   ├── Catalog.Raw.cs              # Catalog items for this layer
│   └── Schemas/                    # Schemas referenced by catalog items
│       └── *.cs
├── _02_Intermediate/               # Typed mirror of raw data — cleaned, parsed, typed formats
│   ├── Catalog.Intermediate.cs
│   └── Schemas/
│       └── *.cs
├── _03_Primary/                    # Domain data — restructured for the problem being solved
├── _04_Feature/                    # Engineered features derived from primary data
├── _05_ModelInput/                 # Joined feature sets ready for model consumption
├── _06_Models/                     # Serialized trained models
├── _07_ModelOutput/                # Results and predictions from model runs
└── _08_Reporting/                  # Descriptive and summary outputs for business consumption

Flows/                              # All flows
└── <FlowName>/
    ├── <FlowName>Flow.cs       # Steps wired to input/output catalog items
    └── Steps/
        └── *.cs                    # Individual transformation steps
```

**Key relationships:**

- Each **catalog item** is an `IItem<T>` referencing a **schema** type and produced by a smart constructor (`ItemFactory.Enumerable.Csv<T>(...)`, `ItemFactory.Singleton.EFCore<T, TContext>(...)`, etc.) from `using Flowthru.Data.Catalog;`.
- Each **schema** is a `[FlowthruSchema] public partial record` — the `partial` keyword is required by the source generator (FT1001 enforces this).
- Each **step** is a `[FlowthruStep] public static class` with the canonical `public static Func<TIn, TOut> Create([deps]) => input => …` authoring shape. Service injection is parameters to `Create`, captured by closure.
- Each **flow** wires steps to catalog items via `FlowBuilder.CreateFlow(label, b => b.AddStep(label, step.Create(deps), input1, input2, output))`. `BuiltFlow` is the immutable result.
- `Program.cs` registers flows via `services.AddFlowthru(b => b.RegisterCatalog<TCatalog>().RegisterFlow<TCatalog>(label, factory).UseXxx())` so the CLI runner can discover and execute them.

**Effects.** I/O is wrapped in `FlowIO<T>` from `using Flowthru.Prelude;`. Failures are typed values (`RuntimeError`/`PreFlightError` closed sums) — nothing throws across the FlowIO boundary. Most step bodies don't see this directly; the framework wraps the user's `Func<TIn, TOut>` for them.

## Workflow

Tasks in Flowthru are ordinarily of the form: *"I have X input data, and I want to transform it to Y output data."*

Follow these steps in order:

1. **Plan transformation steps.** Determine what discrete transformations are needed to get from the input data to the output data.
2. **Plan intermediate schemas.** Identify what data shapes are needed between each transformation step.
3. **Write schemas.** Create `[FlowthruSchema] public partial record` types for input, output, and all intermediary data in the appropriate `Data/<Layer>/Schemas/` directories. Use `[SerializedLabel]` for external-name aliases and `[SerializedEnum]` for enum-as-string serialization.
4. **Create catalog items.** Add `IItem<T>` properties to the corresponding `Catalog.<Layer>.cs` file using `ItemFactory.X.Y(...)` smart constructors from `using Flowthru.Data.Catalog;`. Each item identifies its schema type via the generic parameter and its storage strategy via the smart-constructor choice.
5. **Write steps.** Add `[FlowthruStep] public static class` files in `Flows/<FlowName>/Steps/`. Each step has the canonical shape: `public static Func<TIn, TOut> Create([deps]) => input => …`. Tuples (e.g. `(IEnumerable<X>, IEnumerable<Y>)`) carry multi-input / multi-output shape.
6. **Wire the flow.** In `Flows/<FlowName>/<FlowName>Flow.cs`, define a `Create(Catalog catalog, …)` method returning `BuiltFlow` via `FlowBuilder.CreateFlow(label, b => b.AddStep(...))`. The `AddStep<...>` overloads are typed — passing the wrong-typed catalog item is a C# compile error.
7. **Register the flow.** In `Program.cs`, call `services.AddFlowthru(b => b.RegisterCatalog<Catalog>().RegisterFlow<Catalog>(label, FlowName.Create))` plus `b.UseXxx()` for each consumed extension (`UseHttp`, `UsePython`, `UseDiagnostics`, etc.).
8. **Run and confirm.** Execute the project with `dotnet run` (no args) and verify the output. The CLI runner discovers registered flows and runs them.

**If a build error or runtime failure surfaces something the skill doesn't cover, stop and ask.** The framework's API surface is intentionally narrow; if the natural code shape doesn't fit it, that's a signal to discuss before working around it.