# Flow Developers

Read this when **writing Steps or wiring Flows**. For catalog items and schemas, see [catalog-developers.md](catalog-developers.md); for stack-specific step hosts and formats, see [extensions.md](extensions.md).

## Mental model

A **Flow** is a DAG of **Steps** wired between **catalog items**. A Step is a pure function `input → output`; the Flow declares which catalog items feed each Step's input and which receive its output. Flowthru builds the DAG from that wiring, validates it, and — at run time — reads inputs, runs each Step, and writes outputs, scheduling Steps as their inputs become available.

You write two kinds of code:

- **Step logic** — the transform itself, as plain C# (LINQ, loops, whatever). This is where your domain logic lives.
- **Flow wiring** — a declaration of how Steps connect to catalog items. No logic here; just typed plumbing.

The split matters because the wiring is checked at **compile time**: passing a catalog item of the wrong schema type to a Step input is a C# type error, caught before anything runs.

## Steps

A Step is a `[FlowthruStep] public static class` exposing a `Create` method that returns the transform as a `Func<TIn, TOut>`. Dependencies (loggers, services) are parameters to `Create`, captured by the returned closure — the transform itself only ever sees its typed input:

<!-- flowthru:snippet:docs:step-shape:start -->
```csharp
[FlowthruStep]
public static class TransformGreetingsStep
{
  /// <summary>
  /// Creates a transformation function that converts "Hello" greetings into
  /// both "Goodbye" and "So long" variants.
  /// </summary>
  /// <returns>
  /// A function that takes hello greetings and returns a tuple of
  /// (goodbye greetings, so long greetings).
  /// </returns>
  public static Func<
    IEnumerable<GreetingSchema>,
    (IEnumerable<GoodbyeSchema>, IEnumerable<SoLongSchema>)
  > Create()
  {
    return (helloGreetings) =>
    {
      // Convert to list to avoid multiple enumerations
      var greetings = helloGreetings.ToList();

      var goodbyeGreetings = greetings.Select(hello => new GoodbyeSchema
      {
        Greeting = hello.Greeting.Replace("Hello", "Goodbye"),
      });

      var soLongGreetings = greetings.Select(hello => new SoLongSchema
      {
        Greeting = hello.Greeting.Replace("Hello", "So long"),
      });

      return (goodbyeGreetings, soLongGreetings);
    };
  }
}
```
_(source: [`Minimal/TransformGreetingsStep.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Minimal/Flows/Greetings/Steps/TransformGreetingsStep.cs))_
<!-- flowthru:snippet:docs:step-shape:end -->

- **`Create(deps) => input => …`** is the canonical authoring shape. Inject services as `Create` parameters (`Create(ILogger logger)`); keep the transform body a pure function of `input`.
- **Tuples carry arity.** A `(A, B)` input or `(X, Y)` output tuple is how a Step declares more than one input/output — the example above is a 1→2 transform.
- **Options-as-input.** A Step's configuration is just another typed input (often a nested `Options` record), sourced from the catalog as a configuration-bound item (see [catalog-developers.md](catalog-developers.md)). A config change then invalidates the step's cached output like any other input change — don't reach for ambient/global config.

## Flows

A Flow factory is a `public static class` with a `Create` method returning `BuiltFlow`. It takes the catalog (and any services the wiring needs) and calls `FlowBuilder.CreateFlow(label, pipeline => …)`, adding one `AddStep` per Step:

<!-- flowthru:snippet:docs:flow-wiring:start -->
```csharp
public static BuiltFlow Create(Catalog catalog)
{
  return FlowBuilder.CreateFlow("Greetings", pipeline =>
  {
    pipeline.AddStep<IEnumerable<NameSchema>, IEnumerable<GreetingSchema>>(
      label: "CreateHello",
      transform: CreateHelloStep.Create(),
      inputs: catalog.Names,
      outputs: catalog.HelloGreetings
    );

    pipeline.AddStep<
      IEnumerable<GreetingSchema>,
      IEnumerable<GoodbyeSchema>,
      IEnumerable<SoLongSchema>
    >(
      label: "TransformGreetings",
      transform: TransformGreetingsStep.Create(),
      inputs: catalog.HelloGreetings,
      outputs: (catalog.Goodbyes, catalog.SoLongs)
    );
  });
}
```
_(source: [`Minimal/GreetingsFlow.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Minimal/Flows/Greetings/GreetingsFlow.cs))_
<!-- flowthru:snippet:docs:flow-wiring:end -->

- **`AddStep<…>` is fully typed.** The generic parameters are the Step's input types followed by its output types; `inputs`/`outputs` are catalog items (tuples for arity > 1). The compiler checks that each item's schema type matches the Step's signature — wrong type, wrong arity, or swapped input/output all fail to compile.
- **`BuiltFlow` is immutable.** `CreateFlow` returns a fully-built, validated flow; there is no mutate-after-build.
- **The DAG is inferred from the wiring.** You don't declare edges; a Step that consumes an item another Step produces is automatically downstream of it.

## Registration & running

Flows register in the host's `AddFlowthru(...)` block; the entrypoint hands off to the Flowthru CLI runner (`FlowthruCli.RunStandaloneAsync`), which discovers and executes registered flows:

<!-- flowthru:snippet:docs:register-flows:start -->
```csharp
services.AddFlowthru(flowthru =>
{
  flowthru.RegisterCatalog(_ => new Catalog(basePath));
  flowthru.ConfigureMetadata(meta =>
  {
    var metadataPath = Path.Combine(basePath, "Metadata");
    meta.AddJsonMetadata(opt => opt.WithOutputDirectory(metadataPath));
    meta.AddMermaidMetadata(opt => opt.WithOutputDirectory(metadataPath));
  });

  flowthru
    .RegisterFlow<Catalog>("Greetings", GreetingsFlow.Create)
    .WithDescription("A minimal pipeline demonstrating name transformation into multiple greeting formats");
});
```
_(source: [`Minimal/Program.cs`](https://github.com/chaoticgoodcomputing/flowthru/blob/main/examples/starter/Minimal/Program.cs))_
<!-- flowthru:snippet:docs:register-flows:end -->

- A flow factory that needs a DI service takes it as its second parameter, declared as the second generic: `b.RegisterFlow<Catalog, ILogger>("DataEngineering", DataEngineeringFlow.Create)`. The runner resolves and injects it.
- Chain `.WithDescription(...)` for the CLI listing. Each consumed extension is enabled with its own `b.UseXxx()` — see [extensions.md](extensions.md).
- Run with **`dotnet run`** (no args) and confirm the output.

## Testing steps

Because a Step is a plain function, it unit-tests without any pipeline: build typed input rows, invoke the transform, assert on the result. The optional **`Flowthru.FUnit`** package puts those tests beside the Step itself (a nested `Tests : FUnitContext` class behind `#if FUNIT_ENABLED`) — if this project references `Flowthru.FUnit`, or you're adding step tests, pull its deep skill:

```bash
npx skills add chaoticgoodcomputing/flowthru --skill flowthru-funit
```
