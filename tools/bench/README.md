# External Ground-Truth Benchmarks

Alignment initiative 04 — measure the **end product** of the interaction stack against an external,
crowd-sourced combo database the agents did not author. The evaluation stack is otherwise
self-referential (gold ASTs are authored and judged by the same model family); this harness answers a
question no internal test can: **does the interaction engine reconstruct the combos that are known to
exist?**

The harness lives here, OUT of the runtime libs (`libs/`) and OUT of the Flowthru test host
(`tests/magic-ast-tests`): it is evaluation tooling, not shipped product code. It depends on the libs;
nothing in the libs depends on it.

## Track A — combo recall (built)

`MagicAtlas.Bench/` — for each pinned Commander Spellbook combo whose every constituent card has a
hand-parsed gold fixture, run the **exact** MAST interaction pipeline over precisely that card set and
record whether the engine reconstructs the combo's interaction as a cycle, and at which certainty tier.

Pipeline (the same one `tests/magic-ast-tests/.../InteractionTriage` and `PortGraphEngineTest` drive):

```
PortWalk.Project(card, goldAbilities)   per combo card   →   PortGraph
PortGraphEngine.Materialize(graphs)                       →   PortEdge[]
PortGraphEngine.FindCycles(edges, maxLength: 5)           →   PortCycle[]
```

A combo is **reconstructed** iff some cycle spans ≥2 of its cards and every card the cycle touches
belongs to the combo. The combo's tier is the best (lowest `CertaintyTier`) such cycle — Green
(certified infinite) beats Amber (conditional); a combo with no spanning cycle is **Missed**.

### Why read the gold AST, not re-parse oracle text

The bench reads each fixture's committed gold AST (`Output.Oracle.Abilities`) directly. This keeps the
bench fully offline, independent of the parser's current coverage, and measures the *engine* over the
same trusted ASTs the MAST tests use — not the parser+engine compound.

### Data scoping (IMPORTANT)

The full parsed corpus (`card-inputs.json`) is gitignored and absent on a fresh checkout / in an
isolated worktree. So the eligible set is scoped to **combos whose every card has a hand-parsed gold
fixture** under `tests/magic-ast-tests/Fixtures/HandParsedCards/**` (these ARE committed). Of the
~91.8k Commander Spellbook combos, **33** multi-card combos are eligible against the current
946-card gold corpus. This scoping is recorded in the snapshot (`scope`, `eligibleCount`) and re-checked
at runtime. As the gold corpus grows the eligible set grows automatically (re-pin the snapshot).

### Running it

```bash
# Print the recall report (does not touch the committed report file):
dotnet run --project tools/bench/MagicAtlas.Bench

# Regenerate the derived report (bench-report.json) — do this after a deliberate, reviewed recall change:
dotnet run --project tools/bench/MagicAtlas.Bench -- --write

# The gate: each eligible combo must satisfy exactly the axes combo-axis-expectations.json expects.
dotnet test tools/bench/MagicAtlas.Bench

# Regenerate ONLY the eligible-set roster (id + cards) in combo-axis-expectations.json.
# Nothing regenerates the judged `verdict` field — see below.
dotnet run --project tools/bench/MagicAtlas.Bench -- --regenerate-roster
```

(`nx` targets are not wired here; invoke `dotnet` directly. The bench has no network or Python
dependency.)

### Two tiers: the strict gate here, the wide measurement in the flow host

This gold bench is the **strict gate tier** — small (33 combos), offline, deterministic, axis-pinned, CI-safe. Its 33-combo denominator is deliberately tiny (every card must have a committed gold
fixture), which makes it a rock-solid regression gate but **too small to show per-batch progress**: a
batch can unblock dozens of combos and move this number by zero.

The complement is the **wide measurement tier**, produced by the Flowthru host's CardAtlas flow (co-emitted
from the same per-combo reconstruction that builds D4):
`tests/magic-ast-tests/Data/_08_Reporting/extended-recall-report.json` (schema
`ExtendedRecallReport`). It reconstructs **every** combo whose cards are projection-ready in the current
corpus — thousands, not 33 — and reports `green / amber / missed`, `recallAtGreen`, `recallAtAmber`, and
a **popularity-weighted** recall (does the engine reconstruct the *popular* combos?). It has NO pins and
is NOT a gate — it needs the gitignored corpus, so it runs only where the corpus is present (main, not
worktrees). Regenerate with `dotnet run -- --flow CardAtlas` from `tests/magic-ast-tests`.

Use the two together: the gold gate HALTs the loop on any regression (correctness floor); the wide
report is the batch scoreboard (are we reconstructing more of the combos that actually exist?). Neither
subsumes the other — 33 pinned combos can't measure progress, and thousands of un-pinned combos can't be
a stable gate.

### The gate (`combo-axis-expectations.json`) and the derived report (`bench-report.json`)

The gate pins **which ADR-0002 §8 axes hold**, not a colour and not a diagnostics snapshot
(ADR 0004 §5, issue #31). It replaced `combo-expected-tiers.json`, which stored a per-combo *copy of what
the engine produced* — a `Green`/`Amber`/`Missed` tier plus an `expected` block carrying the winning
cycle's limiting hop — and asserted the live run still equalled it. That is a golden-file test with 33
golden files, and it churned on changes with no semantic content: the `LimitingHop`
null-when-nothing-limits fix (`776ff939`) moved **18 of 33 pins for zero semantic change**.

```json
{
  "_doc": "...",
  "axes": ["Firable", "CoCostsSatisfied", "Balanced", "LifeBalanced", "Productive"],
  "combos":          [ { "id": "11-3368", "cards": ["Narset's Reversal", "Reiterate"] }, ... ],
  "axisExceptions":  [ { "combo": "11-3368", "axis": "Balanced", "verdict": "genuine", "note": "" }, ... ],
  "unreconstructed": [ { "combo": "618-1692", "verdict": "no-reconstruction", "note": "" }, ... ]
}
```

**The default is stateless.** Every eligible combo is expected to satisfy all five axes — to be a
*certified infinite*. No entry is needed to say that, so a newly-eligible combo is covered the moment it
appears, and a silently-degrading one has nowhere to hide.

**An exception is `{combo, axis, verdict}`.** The engine computes *that* an axis fails; a human rules
*that the failure is genuine*. **Only `verdict` is hand-set, and nothing regenerates it** — an expectation
regenerated from the engine's own output asserts that the engine agrees with itself and can never fail
(ADR 0004 §5.2). When an axis moves, the gate prints the exact entry to paste, stamped `UNJUDGED`, which
is itself a hard failure until ruled on.

| verdict | meaning |
|---|---|
| `genuine` | the axis genuinely does not hold in Magic terms — the engine is right to floor it (judge-attested) |
| `modelling-gap` | the axis fails because the model is coarse, not because Magic says so — known debt, still pinned so it cannot move unnoticed |
| `carried-over` | inherited verbatim from the pre-#31 pin file, whose prose carried no judge attestation. **Debt, not an endorsement** — a burn-down list for the interaction-judge |

A failure names **which axis moved**, in which direction:

```
Combo '618-4404' (Kiki-Jiki, Mirror Breaker + Corridor Monitor) — AXIS MOVED: +Firable
  expected to fail : []
  actually fails   : [Firable]
  plain language   : needs a way to untap between iterations
```

**Honest classification.** `axisExceptions` is per-combo state: a *narrower pin*, not a stateless
invariant. `combos` is the eligible-set **roster** (id + cards) — Derived, gate-checked against the live
run, regenerated by `--regenerate-roster`, never hand-edited; it is committed only because two CORE-ring
consumers (the ADR-0004 §4 quarantine→tier cross-track join and the fidelity blast-radius report) need
`card → combo` without being able to run the interaction engine.

**`note` is narrative only** — no gate and no report treats it as truth (ADR 0004 §5.3). The mechanistic
prose is gone: plain-language text is now *generated* from the axis vector at display time by
`MagicAST.Interaction.ComboPlainLanguage` and stored nowhere.

**`unreconstructed`** pins the combos with no reconstruction at all (the old `Missed` tier). Issue #31
intended these to leave this gate for issue #32's derived demand backlog, but **#32 does not exist yet**,
so they stay here rather than being silently dropped (coverage removed with nothing replacing it) or
moved to a new hand-maintained backlog file (exactly the artifact ADR 0004 exists to remove). They stay
load-bearing meanwhile: the gate fails if one starts reconstructing (an unannounced gain) and fails if a
reconstructing combo stops (a regression). **#32 inherits these four combo ids as unserved demand**, at
which point this section and its gate assertions are deleted.

`MagicAtlas.Bench/bench-report.json` is a **derived report artifact**, regenerated by
`dotnet run -- --write`; it carries the aggregate recall and per-combo detail for humans, but it does not
drive pass/fail.

```json
{ "CombosEligible", "ReconstructedGreen", "ReconstructedAmber", "Missed", "RecallAtGreen", "RecallAtAmber", "Combos": [...] }
```

### Determinism

Two consecutive runs produce a byte-identical `bench-report.json` (verified). The snapshot pin is enforced three
ways: the combo list is a committed, **SHA-256-checksummed** snapshot (verified on load); the gold
corpus resolves duplicate card names first-path-wins in ordinal order; and combos + per-combo cards are
emitted in sorted order with a fixed JSON encoder (no timestamps, no machine paths).

## Track B — query precision/recall (Scryfall Tagger) — NOT built (licensing TODO)

The query-P/R track (score `mast-query` patterns against crowd-sourced Scryfall oracle tags) is a
**stretch goal and is intentionally not implemented**, because its data source has an unclear
redistribution license:

- Scryfall's **card data** is permissive (their generated data is CC0; gameplay text falls under WotC's
  Fan Content Policy). But the **Tagger** tags are a separate, crowd-sourced project.
- Tagger data is **not** in any documented Scryfall bulk-data file, and there is **no documented public
  API or stated redistribution license** for it (it is served from the undocumented
  `tagger.scryfall.com` GraphQL endpoint).

Per the initiative's guardrail — *do NOT commit Tagger raw data on an unclear license* — no Tagger
snapshot is committed. **TODO (Track B):** if/when a clearly-licensed tag source is identified
(documented API + explicit reuse terms), implement it as a *fetch-with-checksum + attribution* snapshot
(never a committed raw dump), curate 5–10 high-signal tags with clean AST equivalents, author the
matching `mast-query` patterns, and emit a per-tag P/R table into the bench report.

## Sources, licensing & attribution

| Source | Used for | License / terms | Committed? |
|---|---|---|---|
| **Commander Spellbook** — `variants.json` bulk dump (`https://json.commanderspellbook.com/variants.json`) | Track A combo list (the eligible-set ground truth) | Backend is **MIT-licensed** (`SpaceCowMedia/commander-spellbook-backend`). Card names/oracle text are WotC IP under the Fan Content Policy; Commander Spellbook is unofficial Fan Content. | Yes — a lean, **scoped** snapshot (only `{id, popularity, identity, cards{name,oracleId}, results}` for eligible combos; the ~510 MB raw dump and its image-URI/price/legality bloat are NOT committed). The snapshot records the CSB `version`, `timestamp`, and `ETag` for provenance. |
| **Scryfall Tagger** — oracle tags | Track B query P/R (not built) | Unclear redistribution license (see Track B). | **No** — deliberately not committed. |

The snapshot is **eval data, not shipped product data**. Combo identities, card names, and produced
results originate with Commander Spellbook and the respective rights holders.

### Pinned snapshot provenance

`MagicAtlas.Bench/Data/spellbook-combos.snapshot.json` (+ `.sha256`): CSB `variants.json` version
**5.4.10**, timestamp **2026-06-12T07:24:47Z**, 33 eligible combos pinned from the full ~91,795-combo
dump.
