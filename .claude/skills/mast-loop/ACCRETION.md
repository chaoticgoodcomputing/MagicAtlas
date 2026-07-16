# Accretion track — witnessing the ADR-3 taxonomy

The ADR-3 track (`mast-interaction-loop`, folded here). Its unit of work is an **interaction gold** — the only hand-authored artifact in the interaction layer — and its objective is to **witness** that a port stem / attribute / edge / residual rule exists, is AST-derivable, and is CR-correct, then let the rollup accrete it. This is *schema-by-accretion* (ADR-0003 §8): the taxonomy grows only by golds that pay for each new term with a concrete witness.

> **Shadow-mode reality.** The live engine emits ADR-2 ports; this track does **not** move `bench:recall` today (see the umbrella's migration seam). Its product is the topology rollup + rule ladder + the sought-hole backlog burn-down — the ADR-3 replacement being built toward Stage-4 cutover. Author golds in ADR-3 vocabulary regardless; the engine catches up at Stage 3.

## Step 1 — Pick from the demand-ranked reporting layer

Three ADR-3 surfaces, all under `Data/_08_Reporting/` and `Fixtures/Interactions/rollup/`. Refresh them first: `nx run mast:combo-anchors` (the corpus demand source), then `dotnet run -- --flow InteractionRollup` (the hermetic topology), then `dotnet run -- --flow TopologyDemand` (the value overlay).

- **`port-topology-demand.json`** (PRIMARY — value-ranked pick surface). Sections `witnessed_stems` / `declared_stems` / `holes` / `supergroups`, each entry `{ concept, kind, status, demand:{ witnessed?, corpus? }, priority?, matched_payoffs[], note? }`.
  - **`holes[]`** (the targeted-witnessing backlog) ranked by `demand.corpus DESC, priority ASC` — the combo-popularity mass of the payoffs whose slang/concept-word matches the hole. This is where to pick: the top sought hole with real corpus demand is the highest-value taxonomy gap.
  - **Honest caveat, encoded in the report.** `demand.corpus` measures *payoff*-side demand, so **enabler** holes (`cost-modification`, `restriction-grant`) read low/zero even when structurally important — their `note` says "enabler-side; payoff-invisible — panel priority governs". For those, `priority` (panel corroboration) is the real signal, not the corpus number. Do not down-rank a priority-1 enabler hole because its corpus demand is 0; that is the metric's known blind spot, not a verdict. (The `library`-token over-count on `library-search`/`-selection` is the same coarseness — cross-check `matched_payoffs`.)
  - **`witnessed_stems[]`** ranked by `demand.witnessed` (Σ popularity of the golds that witness the stem). Thin today — only golds carrying `source.popularity` contribute — so it corroborates rather than drives; enrich it by giving new golds a `source.popularity`.
- **`port-topology.cited.json`** (STRUCTURE — what exists and its status). The full port universe: `stems` (each `status: declared | witnessed`, `witnesses[]`, `unpredicted?`), `supergroups`, `event_verbs`, `aliases`, `holes` (`status: sought`), `attribute_axes` (the closed licensed-attribute set + lattices). Read this to see the shape you are adding to, and to spot **`unpredicted` witnessed stems** — a stem a gold projects that the scaffold never declared (a taxonomy surprise worth a scaffold update). The lean `port-topology.json` strips `witnesses`; use `.cited` when you need them.
- **`port-interactions.cited.json`** (RULES — the residual layer + promotion ladder). `polarity` / `match_policy` / `guards` / `bridges`, each rule with its promotion status (`observed → corroborated → confirmed`) and CR citations. Read this before declaring a rule — **an existing rule you can reuse must not be re-declared** (the rollup unions by id and FAILS on a same-id/different-content conflict). Declaring a *corroborating* witness for an `observed` rule is how it climbs the ladder.

**Pick heuristic.** Prefer a top-demand sought hole whose witness is a single card or a known pairwise/combo (cheapest to author), or a stem whose `status` is `declared` but never `witnessed` (turn a scaffold prediction into a witnessed fact). A hole that a popular, already-parsing card witnesses is the highest-leverage pick — it burns down the backlog *and* corroborates the scaffold in one gold.

## Step 2 — The witnessing unit

An interaction gold is one of three units (README §"The witnessing unit"). Pick the *smallest* unit that honestly witnesses the claim:

- **`single-card`** — one card's port derivation. Witnesses that a stem / attribute / alias exists, is AST-derivable, and is CR-correct. No cycle, no second card. The cheapest witness for a hole — most sought holes want a single-card witness first.
- **`pairwise`** — one card's emit satisfying another's consume, no cycle closed. Witnesses a subsumption *edge* or a residual rule (e.g. Ruthless Knave → Blood Artist: the `creature ⊆ creature` cover, GREEN).
- **`combo`** — a closed loop. *Additionally* exercises §10 SDF balance (the repetition-vector GREEN/AMBER cycle-tiering), so it carries `loop_tier`. Only reach for a combo when the claim is inherently about a repeating cycle; a stem or edge does not need a loop to be witnessed.

All three climb the same promotion ladder and are `interaction-judge`-gated. **This relaxation is deliberate** (ADR-0003 §8): the corroborated capability holes are rarely combo pieces, so restricting witnesses to combos would make them un-witnessable.

## Step 3 — Author the gold (the ADR-3 grammar)

Write the gold under `Fixtures/Interactions/golds/<id>.json` (`id` stable, name-derived). Full schema + a worked annotated example: [golds/README.md](../../../tests/magic-ast-tests/Fixtures/Interactions/golds/README.md). The three seed golds are the canonical migrated examples — study them by unit:

| Example | Unit | Witnesses |
|---|---|---|
| `chatterfang-x-pitiless-plunderer.json` | combo (GREEN) | the `removal:creature[manner=sacrificed]` emit, `emit:token` dual port, the exclude-self guard, SDF GREEN cycle |
| `deadeye-x-peregrine-drake.json` | combo (AMBER) | the `manner=blink` facet, the self-watch guard, the untap→mana bridge; **parser-target** (blink currently `emit:gainability`) |
| `ruthless-knave-x-blood-artist.json` | pairwise (GREEN) | the `creature ⊆ creature` cover edge, sacrifice-as-dual (fodder consume + LTB emit) |

The load-bearing ADR-3 rules when authoring:

- **`stem` is the is-a spine** — `side:supergroup:card-type` (e.g. `removal:creature`, `deployment:artifact`). Name by the **event/resource that flows**, never the mechanism (`sac`/`ltb`/`etb` are ADR-2 — do not use them in a gold). Sacrifice is folded in as the narrowest `removal`/LTB rung with `manner: sacrificed`, not a separate role.
- **`attrs` is the unordered attribute SET** — facets on the leaf, not nested `:` categories. An attribute value may be a bare value or an object carrying provenance/polarity:
  - `"to": { "value": "graveyard", "provenance": "derived" }` — over-approximated (the parser can't prove the destination) → **caps the edge's Reliability** (a downstream board/Rest-in-Peace prune). Mark it honestly; a bare `"to": "graveyard"` claims the parser proves it.
  - `"color": { "value": "any", "polarity": "producer-choice" }` — an existential/producer-choice match (the §6 polarity layer), not a universal.
  - Only use attribute keys from the closed `attribute_axes` set in `port-topology.cited.json`. A genuinely new axis is a scaffold change (surface it), not an ad-hoc key. **Match the golds' spelling** (`exclude_self`, snake_case) — the axis names are reconciled scaffold↔golds.
- **`edges` name the mechanism** — `subsumption` / `card-defined` / `modifier` are structural (self-certifying); **anything else MUST cite a rule id** that exists in this gold's `declares` or another gold. Each edge carries a `tier` (GREEN/AMBER) and `residuals[]` (guard ids applied).
- **`declares` only NEW rules** — `polarity` / `match_policy` / `guards` (impl in code) / `bridges`, each with a stable `id`, CR citations, and — if it corroborates an existing rule — a `corroborates:` pointer (that is how a rule climbs `observed → corroborated → confirmed`). Reusing an existing rule is a reference, not a re-declaration.
- **`judge`** — set `{ verdict: "PASS", ref: "..." }` only after the `interaction-judge` blesses it; a judge-backed gold's rules may be `confirmed` (a GREEN edge/loop requires `confirmed` rules — ladder coherence).
- **`assertions`** — the machine-checkable claims (tier equalities, reliability caps, `no_loop`); the gold IS its own test. Structural at Stage 0b, engine-executed at Stage 3.
- **`source`** — provenance (`csb`, `popularity`, `absorbed_from`). **Give it a `popularity`** when known — that is what feeds `demand.witnessed`.

### The parse-inside-engine bridge (Step 3, common)

If the stem/attribute the gold needs is **not yet AST-derivable** — the parser projects `emit:unparsed` / a coarse label, or lacks the facet (the `manner`, `Sacrificed` trigger, per-cost spans, blink cases) — **do not author around it**. Stop, spawn a [Parse track](../mast-tdd-loop/SKILL.md) slice against the parser gap (a `mast-worker`, full parse discipline), land it, then return and witness the gold faithfully. A gold whose ports the parser cannot emit is not a witness; it is a wish. The Deadeye gold is exactly this shape — its `source.note` records that blink is the parser target.

## Steps 4-5 — Gate: the rollup flow + the judge

Two gates, both required.

1. **`interaction-judge`** (the CR-correctness / false-GREEN guard) — dispatch `Agent` with `subagent_type: "interaction-judge"`, READ-ONLY, on Opus. It cross-checks each edge's tier against the Comprehensive Rules: is a GREEN genuinely reliable, is an AMBER soundly irreducible vs a fixable gap, is a pruned pair correctly impossible. A FAIL halts. Only a PASS lets the gold's rules be `confirmed`.
2. **The `InteractionRollup` flow** (the structural + ladder gate) — `dotnet run -- --flow InteractionRollup`. It reads all golds + the scaffold, validates, and **regenerates the four `rollup/` artifacts**. It FAILS (the "conflicts fail the build" gate) on any of the README §"What the flow validates" checks:
   - malformed gold / duplicate port or edge ids / an edge `from`/`to` that resolves to no declared port;
   - a non-structural `mechanism` citing a rule that exists nowhere;
   - **tier/ladder incoherence** — a GREEN edge resting on a merely-`observed` rule (GREEN needs `confirmed`);
   - **a rule-union conflict** — the same rule `id` with different content across golds.

The rollup regeneration is deterministic and byte-stable; commit the regenerated `rollup/` artifacts with the gold. **Never hand-edit the `rollup/` files** — they are generated. The loop is: read the rollup → derive a gold, declaring only new rules → regenerate → gate.

## Step 6 — Re-report and loop

Regenerate the demand overlay (`dotnet run -- --flow TopologyDemand`) so the next pick sees the updated backlog: a witnessed hole drops off `holes[]` (its stem moves to `witnessed_stems[]`), and any `unpredicted` stem you introduced surfaces in `port-topology.cited` for a possible scaffold update. Put in the batch report: which hole/stem was witnessed, the unit, the judge verdict, the new rules + their ladder status, and whether a scaffold update is warranted (an `unpredicted` witnessed stem, or a new attribute axis).

## Stop conditions (accretion-specific)

Bail and surface — do not paper over:
- The witness needs an attribute axis or supergroup **not in the scaffold** → that is a taxonomy decision (scaffold change / possibly an ADR amendment), not a gold you can author unilaterally. Surface it with the proposed axis.
- The stem is not AST-derivable and the parse gap is **architectural** (not a family-shaped `mast-worker` slice) → route to the Parse track's stop conditions.
- An edge you believe is GREEN the `interaction-judge` FAILs as an over-approximation → the honest tier is AMBER; record the residual that caps it, do not force GREEN.
- A rule you need **conflicts** with an existing declared rule (same id, different content) → reconcile the taxonomy (is it genuinely a different rule needing a new id, or a real contradiction to resolve?), never silently fork the id.
- The claim is inherently about a repeating cycle you cannot close as a `combo` (missing piece unparsed) → witness the sub-claim as `single-card`/`pairwise` now, leave the loop for when the piece parses.
