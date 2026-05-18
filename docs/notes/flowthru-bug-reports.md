# Flowthru bug reports — discovered while wiring 0.18.2 caching into MagicAtlas

Three issues uncovered during the 0.17.5 → 0.18.2 upgrade and the migration to
`cacheable=True` Python step caching. All reproduced against
`Flowthru.Extensions.Python 0.18.2` consumed via NuGet `PackageReference`.

---

## Bug 1: Python source generator only registers the first `@step` per `.py` file

**Severity**: medium (silent feature loss)

**Repro**: a `.py` file containing two or more `@step(cacheable=True)`-decorated
functions:

```python
# embed_oracle_text.py
@step(inputs=["A", "B"], outputs="X", cacheable=True)
def embed_default(...): ...

@step(inputs=["A", "C"], outputs="Y", cacheable=True)
def embed_finetuned(...): ...
```

**Expected**: both functions emitted into
`PythonSteps.g.cs`'s `PythonStepCacheRegistry.Register(...)` block — two
registrations, one per function.

**Actual**: only the first function (`embed_default`) is registered. The
second decorator is silently ignored. The framework runs `embed_finetuned`
correctly at runtime (the `@step` decorator works for execution) but
`PythonStepCacheRegistry.Lookup("Flows.…", "embed_finetuned")` returns null,
so `PythonStepFactory.AddPythonStep` leaves `codeVersion = null`, which
the cache plan classifies as uncacheable (`CachePlanBuilder.cs:108`). The
step re-runs every flow invocation — no warning, no diagnostic.

**Workaround**: one `@step` function per `.py` file (the documented pattern
in `examples/starter/KedroSpaceflightsPython`).

**Suggested fix**: emit one `Register(...)` call per `@step` decorator
discovered in each `.py` file, not just the first. Or emit `FT2xxx` diagnostic
on second+ decorator instances to make the limitation visible.

---

## Bug 2: `FT2007` warning message has unsubstituted `{0}` placeholder

**Severity**: low (cosmetic / observability)

**Repro**: any `@step(outputs="MyCatalogLabel")` decorator where
`MyCatalogLabel` is a catalog item name rather than a `[FlowthruSchema]`
type name. (Both are legal per the 0.18.2 release notes — the latter is
explicitly supported when consumed via `pipeline.AddPythonStep<TIn, TOut>`.)

**Expected**: `warning FT2007: Schema 'MyCatalogLabel' referenced in @step
decorator is not a [FlowthruSchema]-decorated type...`

**Actual**: `warning FT2007: Schema '{0}' referenced in @step decorator is
not a [FlowthruSchema]-decorated type in the consuming compilation;
named-factory consumers (PythonSteps.{X}) will see a downstream compile
error.`

Note the literal `'{0}'` and `{X}` — neither placeholder is substituted with
the actual schema name. A consumer seeing 36 of these warnings (in our
case) has no way to know which decorator each refers to without manually
correlating line numbers (which also aren't surfaced — the diagnostic is
emitted as a project-level message, not a file/line one).

**Suggested fix**: pass the schema name (and consumer name for the `{X}`
placeholder) into the `Diagnostic.Create` call so `DiagnosticDescriptor`'s
format string is substituted properly. Also emit per-location diagnostics
where the offending decorator lives, not as a single project-level message.

---

## Bug 3: Cascade uncacheability has no developer-visible diagnostic

**Severity**: medium (silent feature loss, very high debugging friction)

**Background**: `CachePlanBuilder.cs:108-138` correctly cascades
uncacheability — any step consuming an item whose producer is uncacheable
becomes uncacheable itself. This is the right semantic (you can't fingerprint
something whose upstream identity is unknown).

**Problem**: there's no way to discover *why* a step is uncacheable. We
spent ~2 hours bisecting because:

1. `@step(cacheable=True)` is marked → looks correct in source.
2. Source generator emits `PythonStepCacheRegistry.Register(...)` → looks
   correct in the generated `.g.cs`.
3. Flow runs end-to-end → step output IS fingerprinted (appears in
   `cache.json` `Items` map).
4. Step never appears in `cache.json` `Steps` map → no warning, no log line,
   no indication that an upstream item used `.Memory()` (deliberately
   non-fingerprintable) cascaded through 7+ steps to invalidate ours.

The cache plan silently filters uncacheable steps out of post-run
fingerprint persistence (`ComputePostRunFingerprintsAsync`). Without a log
line at `Information` or even `Debug` level saying *"step X uncacheable
because input Y's producer Z has no CodeVersion / uses a
non-fingerprintable adapter"*, developers have to read the source to
diagnose this.

**Suggested fix**: emit a structured log entry per uncacheable step in the
cache plan builder, naming the immediate cause (own `CodeVersion` null /
own `ServiceDependencies.Count > 0` / cascaded from upstream label X /
unfingerprintable input Y). Or expose a `--explain-cache` CLI flag that
walks each step and prints its cache-eligibility decision.

---

## Context

- Project: MagicAtlas (`/home/spelkington/Repos/cgc/MagicAtlas`).
- Flowthru consumed via NuGet `PackageReference` at
  `Flowthru.Core 0.18.*`, `Flowthru.Extensions.Python 0.18.*`,
  `Flowthru.Cli 0.18.*`, etc.
- ~~12-step pipeline mixing C# `[FlowthruStep]` transforms and Python
  `@step` functions (BERT encode, UMAP, HDBSCAN, c-TF-IDF, fine-tune,
  evaluate, Plotly render). 15 Python step entries total, all marked
  `cacheable=True`.
- All ConfigurationItem<T> instances confirmed fingerprintable
  (`ConfigurationItem.cs:152-155`).
- Repro: cold run populates `.flowthru/cache.json` with 4–5 step entries
  out of 17 expected; warm run is within noise of cold (108s vs 109s for
  `--flow ModelEvaluations`) because the heavyweight Python steps
  (`EmbedOracleText`, `EmbedOracleTextFineTuned`, UMAP×2) cascade
  uncacheable through `ProcessedCards` (`.Memory()` adapter, by design
  non-fingerprintable).
