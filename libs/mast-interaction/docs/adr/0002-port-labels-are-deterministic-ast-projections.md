# Port labels are a deterministic ontology projected from the AST

## Status

Proposed (2026-06-03). Refines [ADR 0001](0001-the-interaction-line.md) §2 (the labelling unit is the port) and §3–4 (the grammar). Motivated by the first union-graph judge run (`docs/judgments/interaction-novel-loops-2026-06-03.md`), which showed that hand-labelled ports conflate distinct semantics and manufacture false edges. Precursors landed on `feat/mast-improvements` (hand-labelled recognizers + the union graph + the judge run); no projection engine yet.

## Context

[ADR 0001](0001-the-interaction-line.md) fixed the **port** — an addressable ability sub-tree in a role — as the unit that interacts, but left the label scheme open. The first cut hand-coded one recognizer per role (`DeathTrigger`→`death-payoff`, `CreateToken`/`TokenReplacement`→`token-doubler`, `SacrificeCost`→`sac-outlet`). Materialising the union graph over the corpus surfaced four forces:

- **Coarse labels manufacture false edges.** `death-payoff` collapses "when **this** creature dies" (self) and "whenever **another** creature dies" (others) into one port that consumes any creature death. Over the union graph that produced 644 false-GREEN cycles — the judge's top finding. The label was less precise than the interaction it named, and the operator certified edges the label had already gotten wrong. `token-doubler` collapses Chatterfang (which *emits* extra tokens) with Doubling Season (which only *multiplies*) — a second mislabel, a different role wearing one name.
- **Hand-coded recognizers don't scale or lint.** One C# recognizer per role covers a handful of shapes, collides as a hot file as more are added, and each hand-picks its label string — so labels drift in style and specificity, and nothing checks that a label *matches* the card's actual behaviour.
- **The AST is already typed.** A port's role is a typed sub-tree — a `Trigger{Event,Filter}`, a `Cost{CostType,Filter}`, an `Effect{EffectType,…}`. The facets that decide an interaction (zone event, cost action, effect kind, the filter's type and scope) are structured fields, not prose. A label can be **derived** from them rather than chosen.
- **A label is a query, and fidelity is bounded by the parse.** `ltb:creature:owned` is exactly the mast-query pattern `{Trigger:{Event:Dies, Filter:{CardTypes:[creature], Controller:You}}}` — the same object viewed two ways. But "this creature dies" and "a creature dies" parse identically today (`{CardTypes:[creature]}`), so any AST-derived label is only as precise as the AST. That bound is a thing to surface, not hide.

## Decision

### 1. A port label is a deterministic projection of its AST sub-tree

`PortLabel(node)` is a **pure function**: per node-kind it projects a fixed sequence of facets through fixed vocabulary maps and joins them with colons. Same sub-tree → same label, always — no heuristics, no hand-authored strings. The label is a **build output** (content-hashed, regenerated), never source-of-truth, consistent with [ADR 0001](0001-the-interaction-line.md)'s "derived artifacts are build outputs, not git source."

### 2. The colon-ontology

A label is a most-significant-first colon-path over a controlled, **soft (hierarchical)** vocabulary:

```
<role> : <subject> : <qualifier…>
```

- **role** — the node kind in MTG lingo: triggers `ltb` `etb` `cast` `attacks`; costs `sac` `pay` `tap`; effects `emit` `destroy` `draw` `counter`.
- **subject** — `creature` `artifact` `token` `permanent` `land` …
- **qualifier(s)** — role-appropriate: *scope* (`self` `owned` `any` `opponent`) for events and costs; *subtype/specifics* (`treasure`) for emits.

Examples: `ltb:creature:owned` (Pitiless's trigger), `sac:creature` (Ashnod's Altar's cost), `emit:token:treasure`, `etb:token:emits-tokens` (Chatterfang) vs `etb:token:multiplier` (Doubling Season).

"Soft" means the colon-path is a **hierarchy** — `ltb` ⊃ `ltb:creature` ⊃ `ltb:creature:owned`. A facet is appended only when a distinction begins to matter, and shorter labels stay valid prefixes. The facet vocabularies and the per-node-kind projection order are the **curated, judge-reviewed ontology** — the interaction analogue of the type ontology, and where the modelling judgment lives.

### 3. An ability decomposes into consume + emit ports

A port is no longer a whole ability carrying `Emits`/`Consumes` sets (ADR 0001 §2's first cut). An ability decomposes structurally into single-role ports:

- **consume ports** — its trigger (`ltb:creature:owned`) or cost (`sac:creature`).
- **emit ports** — each effect (`emit:token:treasure`).

joined by an **internal edge** (the trigger or cost drives the effect). Pitiless's "Whenever another creature you control dies, create a Treasure" is two ports — `ltb:creature:owned` → `emit:token:treasure` — not one `death-payoff`. The **combo graph connects emit→consume across cards** when the colon-types align: Carrion Feeder's `sac:creature` emits an `ltb:creature` event that Pitiless's `ltb:creature:owned` consumes.

### 4. The label is the readable type; the operator stays the edge oracle

The label is a **lossy** projection — it summarises, it does not decide edges. The join stays with the `ObjectFilter` `Intersects`/`Subsumes` operator ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)): `owned ⊆ any`, `creature ⊆ creature`, the certainty tier. Label and operator agree because both derive from the node, but **the string never drives the join** — it names and groups; the operator rules. [ADR 0001](0001-the-interaction-line.md) §4's per-port-pair evaluation is unchanged.

### 5. A label collision is an AST-precision lint

Because the label is deterministic, two sub-trees that *should* differ but project the **same** label expose an AST that conflates them. "When this creature dies" and "another creature dies" colliding on `ltb:creature` — instead of `ltb:self` vs `ltb:creature:owned` — is exactly the parser gap the judge found. The projection therefore yields, for free, a **ranked queue of parser-precision targets** (collisions weighted by how many false edges each manufactures). This folds label-linting and the self/other-death fix into one mechanism; the **interaction-judge enforces** it — a label asserting a role the card's text does not support is a FAIL.

### 6. The ontology is the feedforward

The colon-vocabulary and projection rules are documented as the canonical convention (`libs/mast-interaction/PORT-LABELS.md` / the lib's `CONTRIBUTING.md`) — the soft guide an `interaction-tdd-loop` worker reads before adding a facet, exactly as the parser's `CONTRIBUTING.md` guides parser workers. A consistent, specific vocabulary chosen up front keeps future port additions coherent by construction.

## Considered options

- **Keep hand-labelled recognizers (status quo).** Rejected: doesn't scale (a hot file), labels drift in style/specificity, and the conflation that manufactures false edges is invisible — there is no determinism to lint against.
- **Opaque/hashed labels (e.g. `CanonicalJson.Hash`).** Rejected: deterministic but neither human-readable nor semantic nor hierarchically matchable — it identifies a sub-tree without naming its role, so it can neither seed the grammar nor be linted.
- **Let the label drive edges (drop the operator).** Rejected: the label is a lossy projection; self/other, type subsumption, and the certainty tier need the actual filter. The string groups; the operator decides.
- **One port per ability (keep `Emits`/`Consumes` sets, no consume/emit split).** Rejected: cannot model the resource flow (a trigger consumes, an effect emits), so it cannot connect Carrion Feeder's sac to Pitiless's trigger, and it reproduces the coarse `death-payoff` that hid the self/other distinction.
- **A rigid enum of labels.** Rejected: every new distinction would be a breaking schema change. The soft colon-hierarchy is extensible (append a facet) and supports graded matching (match at the resolved depth).

## Consequences

- **`PortProjector` becomes a generic AST-walk + `PortLabel` projection**, retiring the hand-coded recognizers (`DeathTrigger`, `CreateToken`, `SacrificeCost`, `TokenReplacement`). Every ability projects ports automatically — a large coverage gain over the four hand-coded shapes — and the port model changes from one-port-with-resource-sets to single-role consume/emit ports joined by internal edges.
- **The family grammar largely derives** from emit→consume colon-type matching, shrinking the hand-authored `known-families.json` toward "the edges the type-matching cannot infer."
- **Existing labels migrate**: `sac-outlet`→`sac:creature`/`sac:artifact`/…, `death-payoff`→`ltb:self`/`ltb:creature:owned`/`ltb:creature:any`, `token-doubler`→`etb:token:emits-tokens`/`etb:token:multiplier`. The two reconstruction golds, the grammar, and the viz palette update with them.
- **A new curated artifact** — the colon-ontology (facet vocabularies + per-node-kind projection rules) — ADR-reviewed, judge-enforced, content-hashed/deterministic.
- **Label collisions become triage**: a ranked queue of parser-precision targets. The first is `ltb:self` vs `ltb:creature:owned`, blocked on the parser carrying `ObjectReference{Kind:Self}` for "this creature" (today collapsed to `{creature}`) — the prerequisite the union-graph judge run identified.
- **Fidelity stays bounded by the AST** — the label system *surfaces* parse gaps as collisions rather than papering over them; some labels (`ltb:self`) cannot be projected until the corresponding parser distinction lands.
