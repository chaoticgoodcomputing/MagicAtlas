# Interaction track — engine coverage, correctness, and taxonomy

The interaction-layer track: extend how much the engine reconstructs (**coverage**), keep what it reconstructs trustworthy (**correctness**), and grow the port taxonomy those reconstructions and golds are built from. One track, three currencies plus a standing quality check that runs at the end of every round.

**Vocabulary, stated once.** Ports and edges are named by **mechanism/role** — `sac`, `ltb`, `etb`, `tap`, colon-chained with qualifiers (e.g. `trigger:damage:combat:player`). This `Label` string is what the engine's matching guards, bridges, cycle detection, and every report in this track actually key on — treat it as the vocabulary that matters when reading a report or writing an arm. Interaction golds (`Fixtures/Interactions/golds/**`) are additionally authored against a `stem` + `attrs` schema (the is-a spine plus an unordered attribute set — see [golds/README.md](../../../tests/magic-ast-tests/Fixtures/Interactions/golds/README.md) for the full grammar) because that's the schema the gold format and the rollup validator expect; treat that as the authoring format for golds specifically, not a competing engine vocabulary to reconcile. When both appear on the same port (a report entry, a judge's cross-check), that's normal — they answer different questions (how the engine matches it vs. how a gold declares it) — not a conflict to flag.

## Currency A — projection-slice (cheapest edge inventory)

A card that **already parses** can still project to a coarse `emit:<x>` / `emit:unparsed` label that no flow arm reads, forming **no interaction edge** — thousands of such cards exist, paid-for parse work sitting dark. Closing one means adding a **PortWalk flow arm** — usually **no parser or gold work at all** — and one arm lights up *every* card carrying that label.

- **Entry report:** `port-label-census.json` → `topProjectionGaps[]` — coarse emit labels ranked by the combo-popularity mass of the cards behind them (same value axis as `fusedScore`). Refresh: `nx run mast:run` then `dotnet run -- --flow PortLabelCensus`.
- **Gold artifact:** a new flow arm per [`libs/mast-interaction/docs/adding-a-flow-arm.md`](../../../libs/mast-interaction/docs/adding-a-flow-arm.md).
- **Gates:** `PortWalkExhaustivenessTests` (the new arm must project or be whitelisted), the Step-8 edge-diff (a new arm SHOULD change edges — the target labels are the expected footprint), `nx run bench:recall`.
- **Signal:** ports/edges ↑, `emit:unparsed` card count ↓.

## Currency B — precision-fix (multiplies the value of all coverage)

Lift a reconstructed edge **AMBER → GREEN** (usually a `Subsumes` type-proof the operator can't yet make), or kill a **false-GREEN** (an over-approximating port that fabricates an "infinite combo" — e.g. a death-payoff arm conflating self-death and other-death). Until GREENs are trustworthy the novel-combo product is worthless, so precision work retroactively raises the value of every parse and projection unit.

- **Entry report:** the `bench:recall` AMBER/missed combos (`tools/bench/MagicAtlas.Bench/bench-report.json` + `combo-expected-tiers.json`) and the `interaction-judge` findings under `docs/judgments/`.
- **Gold artifact:** an operator/port fix + a re-pinned `combo-expected-tiers.json` entry (a named pin edit, never a silent baseline rewrite).
- **Gates:** `nx run bench:recall` (per-combo expected-tier; a regression HALTs, an improvement re-pins) + the `interaction-judge` (the false-GREEN guard).
- **Signal:** RecallAtGreen ↑, judge-PASS rate ↑.

## Currency C — witnessing (grow the taxonomy)

Witness that a port stem / attribute / edge / residual rule exists, is AST-derivable, and is CR-correct, then let the rollup accrete it. This is *schema-by-accretion* (`libs/mast-interaction/docs/adr/0003-taxonomy-redesign.md` §8): the taxonomy grows only by golds that pay for each new term with a concrete witness. Its unit of work is an **interaction gold** — the only hand-authored artifact in the interaction layer.

### Step 1 — Pick from the demand-ranked reporting layer

Three surfaces, all under `Data/_08_Reporting/` and `Fixtures/Interactions/rollup/`. Refresh them first: `nx run mast:combo-anchors` (the corpus demand source), then `dotnet run -- --flow InteractionRollup` (the hermetic topology), then `dotnet run -- --flow TopologyDemand` (the value overlay).

- **`port-topology-demand.json`** (PRIMARY — value-ranked pick surface). Sections `witnessed_stems` / `declared_stems` / `holes` / `supergroups`, each entry `{ concept, kind, status, demand:{ witnessed?, corpus? }, priority?, matched_payoffs[], note? }`.
  - **`holes[]`** (the targeted-witnessing backlog) ranked by `demand.corpus DESC, priority ASC` — the combo-popularity mass of the payoffs whose slang/concept-word matches the hole. This is where to pick: the top sought hole with real corpus demand is the highest-value taxonomy gap.
  - **Honest caveat, encoded in the report.** `demand.corpus` measures *payoff*-side demand, so **enabler** holes (`cost-modification`, `restriction-grant`) read low/zero even when structurally important — their `note` says "enabler-side; payoff-invisible — panel priority governs". For those, `priority` (panel corroboration) is the real signal, not the corpus number. Do not down-rank a priority-1 enabler hole because its corpus demand is 0; that is the metric's known blind spot, not a verdict. (The `library`-token over-count on `library-search`/`-selection` is the same coarseness — cross-check `matched_payoffs`.)
  - **`witnessed_stems[]`** ranked by `demand.witnessed` (Σ popularity of the golds that witness the stem). Thin today — only golds carrying `source.popularity` contribute — so it corroborates rather than drives; enrich it by giving new golds a `source.popularity`.
- **`port-topology.cited.json`** (STRUCTURE — what exists and its status). The full port universe: `stems` (each `status: declared | witnessed`, `witnesses[]`, `unpredicted?`), `supergroups`, `event_verbs`, `aliases`, `holes` (`status: sought`), `attribute_axes` (the closed licensed-attribute set + lattices). Read this to see the shape you are adding to, and to spot **`unpredicted` witnessed stems** — a stem a gold projects that the scaffold never declared (a taxonomy surprise worth a scaffold update). The lean `port-topology.json` strips `witnesses`; use `.cited` when you need them.
- **`port-interactions.cited.json`** (RULES — the residual layer + promotion ladder). `polarity` / `match_policy` / `guards` / `bridges`, each rule with its promotion status (`observed → corroborated → confirmed`) and CR citations. Read this before declaring a rule — **an existing rule you can reuse must not be re-declared** (the rollup unions by id and FAILS on a same-id/different-content conflict). Declaring a *corroborating* witness for an `observed` rule is how it climbs the ladder.

**Pick heuristic.** Prefer a top-demand sought hole whose witness is a single card or a known pairwise/combo (cheapest to author), or a stem whose `status` is `declared` but never `witnessed` (turn a scaffold prediction into a witnessed fact). A hole that a popular, already-parsing card witnesses is the highest-leverage pick — it burns down the backlog *and* corroborates the scaffold in one gold.

### Step 2 — The witnessing unit

An interaction gold is one of three units (README §"The witnessing unit"). Pick the *smallest* unit that honestly witnesses the claim:

- **`single-card`** — one card's port derivation. Witnesses that a stem / attribute / alias exists, is AST-derivable, and is CR-correct. No cycle, no second card. The cheapest witness for a hole — most sought holes want a single-card witness first.
- **`pairwise`** — one card's emit satisfying another's consume, no cycle closed. Witnesses a subsumption *edge* or a residual rule (e.g. Ruthless Knave → Blood Artist: the `creature ⊆ creature` cover, GREEN).
- **`combo`** — a closed loop. *Additionally* exercises §10 SDF balance (the repetition-vector GREEN/AMBER cycle-tiering), so it carries `loop_tier`. Only reach for a combo when the claim is inherently about a repeating cycle; a stem or edge does not need a loop to be witnessed.

All three climb the same promotion ladder and are `interaction-judge`-gated. **This relaxation is deliberate** (ADR-0003 §8): the corroborated capability holes are rarely combo pieces, so restricting witnesses to combos would make them un-witnessable.

### Step 3 — Author the gold

Write the gold under `Fixtures/Interactions/golds/<id>.json` (`id` stable, name-derived). Full schema + a worked annotated example: [golds/README.md](../../../tests/magic-ast-tests/Fixtures/Interactions/golds/README.md). Three canonical golds worth studying by unit:

| Example | Unit | Witnesses |
|---|---|---|
| `chatterfang-x-pitiless-plunderer.json` | combo (GREEN) | the `removal:creature[manner=sacrificed]` emit, `emit:token` dual port, the exclude-self guard, SDF GREEN cycle |
| `deadeye-x-peregrine-drake.json` | combo (AMBER) | the `manner=blink` facet, the self-watch guard, the untap→mana bridge; **parser-target** (blink currently `emit:gainability`) |
| `ruthless-knave-x-blood-artist.json` | pairwise (GREEN) | the `creature ⊆ creature` cover edge, sacrifice-as-dual (fodder consume + LTB emit) |

Load-bearing rules when authoring:

- **`stem` is the is-a spine** — `side:supergroup:card-type` (e.g. `removal:creature`, `deployment:artifact`). Name by the **event/resource that flows**, not the mechanism — a gold's `stem` is not the same field as a port's `label`; don't conflate them. Sacrifice is folded in as the narrowest `removal`/LTB rung with `manner: sacrificed`, not a separate role.
- **`attrs` is the unordered attribute SET** — facets on the leaf, not nested `:` categories. An attribute value may be a bare value or an object carrying provenance/polarity:
  - `"to": { "value": "graveyard", "provenance": "derived" }` — over-approximated (the parser can't prove the destination) → **caps the edge's Reliability** (a downstream board/Rest-in-Peace prune). Mark it honestly; a bare `"to": "graveyard"` claims the parser proves it.
  - `"color": { "value": "any", "polarity": "producer-choice" }` — an existential/producer-choice match (the §6 polarity layer), not a universal.
  - Only use attribute keys from the closed `attribute_axes` set in `port-topology.cited.json`. A genuinely new axis is a scaffold change (surface it), not an ad-hoc key. **Match the golds' spelling** (`exclude_self`, snake_case) — the axis names are reconciled scaffold↔golds.
- **`edges` name the mechanism** — `subsumption` / `card-defined` / `modifier` are structural (self-certifying); **anything else MUST cite a rule id** that exists in this gold's `declares` or another gold. Each edge carries a `tier` (GREEN/AMBER) and `residuals[]` (guard ids applied).
- **`declares` only NEW rules** — `polarity` / `match_policy` / `guards` (impl in code) / `bridges`, each with a stable `id`, CR citations, and — if it corroborates an existing rule — a `corroborates:` pointer (that is how a rule climbs `observed → corroborated → confirmed`). Reusing an existing rule is a reference, not a re-declaration.
- **`judge`** — set `{ verdict: "PASS", ref: "..." }` only after the `interaction-judge` blesses it; a judge-backed gold's rules may be `confirmed` (a GREEN edge/loop requires `confirmed` rules — ladder coherence).
- **`assertions`** — the machine-checkable claims (tier equalities, reliability caps, `no_loop`); the gold IS its own test.
- **`source`** — provenance (`csb`, `popularity`, `absorbed_from`). **Give it a `popularity`** when known — that is what feeds `demand.witnessed`.

### The parse-inside-engine bridge (any currency)

A projection-slice, precision-fix, or witnessing gold can all discover the same blocker: the AST doesn't yet derive the stem/attribute/facet the work needs (a coarse `emit:unparsed`, a missing `manner`, a missing per-cost span). **Do not work around it.** Stop, spawn a [Parse track](../mast-tdd-loop/SKILL.md) sub-slice against the parser gap (a `mast-worker`, full parse discipline), land it, then return and finish the arm/fix/gold faithfully. A gold whose ports the parser cannot emit is not a witness; it is a wish. The Deadeye gold is exactly this shape — its `source.note` records that blink is the parser target.

## Steps 4-5 — Gate: the rollup flow + the judge

Two gates for a witnessing gold, both required (a flow-arm or precision-fix instead follows its own currency's gates above).

1. **`interaction-judge`** (the CR-correctness / false-GREEN guard) — dispatch `Agent` with `subagent_type: "interaction-judge"`, READ-ONLY, on Opus. It cross-checks each edge's tier against the Comprehensive Rules: is a GREEN genuinely reliable, is an AMBER soundly irreducible vs a fixable gap, is a pruned pair correctly impossible. A FAIL halts. Only a PASS lets the gold's rules be `confirmed`.
2. **The `InteractionRollup` flow** (the structural + ladder gate) — `dotnet run -- --flow InteractionRollup`. It reads all golds + the scaffold, validates, and **regenerates the four `rollup/` artifacts**. It FAILS (the "conflicts fail the build" gate) on any of the README §"What the flow validates" checks:
   - malformed gold / duplicate port or edge ids / an edge `from`/`to` that resolves to no declared port;
   - a non-structural `mechanism` citing a rule that exists nowhere;
   - **tier/ladder incoherence** — a GREEN edge resting on a merely-`observed` rule (GREEN needs `confirmed`);
   - **a rule-union conflict** — the same rule `id` with different content across golds.

The rollup regeneration is deterministic and byte-stable; commit the regenerated `rollup/` artifacts with the gold. **Never hand-edit the `rollup/` files** — they are generated.

## Step 6 — The round-end quality check (span-witness)

Before closing out a round of this track — a wave of flow arms, precision-fixes, or witnessing golds — run the **span-witness check**: does every port's `SourceSpan` actually contain the text its label claims? A port whose span lies about its own text is either a false-positive (fabricates an edge — the "Chatterfang feeds Aang" class) or a mis-attributed span (points at the wrong clause). A wrong port outranks a missing one — the same "untrustworthy GREEN > coverage" logic this track already applies to edges, one layer down at the port. New flow arms, projections, and golds are exactly when a bad span gets introduced, so checking right after a round lands catches it with full context instead of during a later, unrelated sweep.

Run it at the **orchestrator layer**, once per round — never delegate the corpus-wide refresh to a subagent, and never re-run it once per worker. Cost is real (a full `card-ports.json` + `span-witness-report.json` regeneration ran ~40-60s in practice) but is a one-time orchestration-layer expense per round, not a per-subagent multiplier:

```
nx run mast:run --flow CardAtlas   # refresh card-ports.json
dotnet run -- --flow SpanWitness   # refresh span-witness-report.json
```

**You no longer need `--no-cache` (or the `rm parse-records.json + .flowthru/cache.json` dance) after a parser change.** Since ADR 0004 #22 the step cache key folds in the first-party code the step actually executes, so editing anything in `libs/magic-ast/` invalidates `ParseCorpus` and everything downstream automatically. See `tests/magic-ast-tests/Infrastructure/StepCodeIdentity.cs`.

`--no-cache` remains the **escape hatch** — reach for it only when the input file itself was touched out of band. Flowthru fingerprints file items on `mtime:size`, not content, so an in-place edit that preserves both is invisible to the cache. A code change is not that case.

Diagnose and route anything the round newly introduced (pick → diagnose → route, full mechanics in [ERROR-CHECK.md](ERROR-CHECK.md)): fix the parser span mint, tighten a too-permissive gold, or extend the anchor-word vocabulary if the flag was a false alarm. Gate: `nx run mast:test` (span-provenance invariants) + the suspect clearing on the next `span-witness` run. A pre-existing suspect this round didn't touch is not this round's problem — leave it for the backlog.

**Standalone use.** The full-corpus sweep (not scoped to a round) is also directly invocable any time drift is suspected — a corpus update, a Parse-track change with no interaction round attached, or a periodic health check. See [ERROR-CHECK.md](ERROR-CHECK.md) for the full report shape and standing facts (anchor-vocabulary extension, the `derived`/`misalignedDfc` non-suspect buckets, why the gate is NUnit and the report is only a diagnostic).

## Stop conditions

Bail and surface — do not paper over:

- The witness needs an attribute axis or supergroup **not in the scaffold** → that is a taxonomy decision (scaffold change / possibly an ADR amendment), not a gold you can author unilaterally. Surface it with the proposed axis.
- The stem is not AST-derivable and the parse gap is **architectural** (not a family-shaped `mast-worker` slice) → route to the Parse track's stop conditions.
- An edge you believe is GREEN the `interaction-judge` FAILs as an over-approximation → the honest tier is AMBER; record the residual that caps it, do not force GREEN.
- A rule you need **conflicts** with an existing declared rule (same id, different content) → reconcile the taxonomy (is it genuinely a different rule needing a new id, or a real contradiction to resolve?), never silently fork the id.
- The claim is inherently about a repeating cycle you cannot close as a `combo` (missing piece unparsed) → witness the sub-claim as `single-card`/`pairwise` now, leave the loop for when the piece parses.
- A span-witness suspect is neither span-fixable nor gold-tighten-able (the port is just wrong and no clean fix presents itself) → surface it, don't suppress the suspect or force a fix that isn't honest.
