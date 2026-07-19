# Edge-level provenance labeling — retiring the combo-level trust color

## Status

**Accepted (2026-07-18); migration not yet started.** Refines [ADR 0003](0003-taxonomy-redesign.md) (this
proposal operates on the **edge** layer §3/§6 already establishes, and extends its §7/§8 provenance
posture; it does not touch the port/stem taxonomy). Reached via two full rounds of panel review this
session (six independent Opus evaluations — three lens-divided, three answering the complete question
independently for cross-comparison) followed by four explicit owner decisions on the points the panel
left open (see Provenance). **This is now a substantially larger initiative than the original ask**: the
owner elected to build `Source` now rather than defer it, and to fold in the frontend's existing
`Inferred`/`Declared` tier conflation — both require a TDD-loop process change and a full migration of
existing golds/pins, not just an engine/schema addition. Treat this ADR as comparable in scope to ADR
0003, not as a small follow-on.

## Context — why this reopened

MagicAtlas certifies combo reconstructions with a single tier per combo: **Green** (fully certified
reliable infinite loop), **Amber** (a cycle exists but something caps confidence), or **Missed** (no
cycle found). The tier is computed by `PortGraphEngine.Materialize()`/`FindCycles()`
(`libs/mast-interaction/PortGraphEngine.cs`) — never hand-typed on the live path.

The project owner raised three critiques:

> a) the color labelling is confusing out of context for subagents, contributors, and — most
> importantly — users of the frontend seeing the colors out of context, who then must rely on a legend;
> b) it hooks our quality assurance to hand-crafted artifacts, which is precisely what we're trying to
> avoid; and c) it hooks too tightly to combos, which — while processing combos is a good way to derive
> complex inter-port interactions — our priority is to derive individual port:port interactions, and
> combo detection is simply an emergent structure of the graph we're developing.

**Panel audit found the problem is worse than initially stated** — "tier" is overloaded not twice but
**three ways**, in three enums that already coexist in the codebase:

- `CertaintyTier { Green, Amber, Red }` (`libs/mast-interaction/Interaction.cs`) — the engine's per-edge
  epistemic verdict.
- `ReconstructionOutcome { Missed, Amber, Green }` (`ComboRecallRunner.cs`) — the bench recall outcome,
  where `Missed` is a **coverage** fact, not a certainty fact.
- `Tier = "Green" | "Amber" | "Inferred" | "Declared"` (`apps/atlas-web/src/data/mock.ts`) — the
  **shipped frontend** type, where `Inferred`/`Declared` come from an entirely different mechanism (the
  statistical port-backfill pass, `coStrength` 0–1 co-occurrence score) bolted onto the same colored-chip
  vocabulary as engine certainty. **This conflation is now explicitly in scope for this ADR to resolve**
  (see Decision §6), not deferred as originally drafted.

**Critique (a) is live today, with a concrete smoking gun:** `apps/atlas-web/src/views/CardExplorer.tsx`
renders bare filter buttons **literally labeled `"Green"`/`"Amber"`**, no adjacent legend, and the site's
tier legend metadata (`useTiers`) is *still backed by mock data* — even the current color system's own
explainer isn't fully wired up.

**Critique (b), on inspection, is real but narrower than it first reads, with a genuine self-consistency
problem the panel flagged:** `PortCycle.Tier`/combo `Outcome` is **100% computed**, never hand-typed on
any production path. What's hand-maintained is the `narrative` prose, the pinned `expectedTier` value,
and — the irreducible seed — the interaction **golds** themselves (ADR 0003 §8's "only hand-authored
artifact"). The panel's sharpest finding: **the proposed `Scope: Fixture` axis is *defined by* those same
hand golds** — adopting it does not itself reduce hand-artifact dependence, it makes visible how much the
system still leans on them. What genuinely reduces hand-dependence is generalization past the fixtures
(ADR 0003 §8's accretion loop), which already exists. Reframing adopted: fixtures are **witnesses/seeds**,
not a liability; `Scope` is a maturity signal for the accretion loop.

**Critique (c) is largely already solved at the serving layer, concentrated at the QA layer.** The
corpus-scale `port_edges` table is already edge-first by design; `ComboRow` is a downstream derived
dataset. The genuinely combo-centric surface is the bench/QA harness — the same artifact critique (b)
targets.

## Decision

**Retire the single combo-level trust color as a *display* artifact. Keep the underlying certainty
computation. Add an edge-level (and, per owner decision, a unified port-level) provenance layer alongside
it, on three facets plus a temporal stamp — not the three axes exactly as originally proposed, but a
version the panel process materially corrected:**

### 1. Keep the certainty tier, relabeled for humans, unchanged in computation

`PortEdge.Tier`/`PortCycle.Tier` stay exactly as computed today (`Firable`, `CoCostsSatisfied`,
`Balanced`, `LifeBalanced`, `Productive`, worst-hop `Reliability`/`Overlap`). This answers *"can we prove
this loop actually works,"* which no provenance label can express. Retired only as a bare color — present
as a plain-language verdict ("Certified infinite" / "Conditional — see why" / "No loop reconstructs").

**Concrete bug to fix as part of this work, independent of the rest:** `PortEdge.Tier`'s current
computation (`Provenance == CardDefined ? Green : ...`) conflates provenance into the certainty
computation — an intra-card edge is declared certain *by construction* rather than *by proof*. Once
`Connector` (below) is its own facet, this line must derive certainty purely from `Overlap`/`Reliability`
/§8, never from `Provenance`.

### 2. `Connector` — the full 6-valued mechanism, not a Card/Rules binary (owner decision)

Card vs. Rules isn't a trust ordering, and PROV has no relation for it — it's a structural classification
of *what kind of link this is*. **Owner decision: surface the golds' existing 6-valued `mechanism` field
directly** (`card-defined` / `subsumption` / `modifier` / `polarity` / `match_policy` / `bridge`) rather
than collapsing to a binary — it's already computed, already richer, and collapsing it loses information
for no real savings. Aggregate to a combo as a descriptive summary ("spans N cards via K mechanisms"),
never a strictest-wins min — that operation is close to vacuous on a nominal axis (any real multi-card
combo aggregates to "cites a rule" almost by construction).

### 3. `Scope` — reuse the existing promotion ladder, not a new binary (owner decision)

**Owner decision: reuse `observed → corroborated → confirmed`** (already generated by the interaction
rollup, with `witnesses:` per rule) rather than inventing a parallel Fixture/Derived binary. An edge whose
mechanism is self-certifying (`card-defined`/`subsumption`/`modifier` — ADR 0003 §6's "structural" class)
gets a `structural` scope value (no rule citation needed, by construction); an edge whose mechanism cites
a declared rule inherits that rule's ladder rung directly. Aggregates within a cycle by strictest-wins
(the semiring product for a conjunction — see Prior art) — this **is** a genuine ordered trust lattice, so
the operation is valid here, unlike on `Connector`. An edge justified multiple independent ways takes the
*best* available rung (the semiring sum), not an arbitrary pick.

### 4. `Source` — graded, combo-level, built now (owner decision — reverses the panel's deferral recommendation)

Commander Spellbook attests **card sets in prose**, never individual port:port edges — there is no way to
point at one hop and say "CSB attests this." `Source` is computed at the **combo/reconstruction level**
and pushed down to display on constituent edges; it is never computed by aggregating up from edges the
way `Scope`/`Connector` are. **Owner decision: build this now, graded**, extending the existing
`CycleEdgeRow.match` template (`verified | partial | derived`) to the fuller ladder:

```
OfficialRuling > CsbVerified > CsbListed > CommunityAttested > MastOnly
```

CSB ingestion already exists (`CsbVariantsRaw`, a live HTTP catalog item; combo ids in
`combo-expected-tiers.json` already **are** CSB variant ids) — the new work is the combo→edge
back-annotation pass (already stubbed elsewhere as `PortEdgeRow.Popularity`, "0 until the back-annotation
pass lands") and, per the owner's explicit direction, **formalizing attestation-grade capture into the
interaction-gold-authoring process itself** (see Migration §2 — this is the TDD-loop change). Rulings
ingestion has no existing corpus; land the ladder's structure now with `OfficialRuling` reachable but
initially always-empty, rather than blocking the rest of this ADR on building rulings ingestion.

### 5. Add a temporal/version stamp — metadata, not a fifth facet

Oracle text and the Comprehensive Rules change; an edge established against stale text or an outdated
ruling is a silent-invalidation risk. Model as bitemporal metadata on the provenance layer — *valid-time*
(which rules/oracle epoch makes this edge true) and *transaction-time* (which engine commit/corpus
snapshot derived it) — not as a classification facet. The raw signals already exist scattered
(`narrativeVerifiedAt`, CSB timestamps, dated judge verdicts, `oracle-text-quarantine.json`); this
unifies them.

### 6. Unify port-level and edge-level provenance vocabulary; retire `Inferred`/`Declared` (owner decision — folds in previously-deferred scope)

The frontend's `Inferred`/`Declared` port tiers are the **same conflation** this ADR fixes for edges, one
layer down: a port's "how sure are we this exists" (the statistical `coStrength` score — a certainty
concern) is currently bolted onto the same enum as "how did we learn this" (declared from AST parsing vs.
statistically backfilled — a provenance concern, structurally identical to edge `Scope`). **Decision:**
apply the same split to ports. A port's declaration status becomes a `Scope`-shaped value (`structural`
for AST-parsed ports, on the same ladder vocabulary as edges) separate from its certainty
(`coStrength`, presented in plain language, not a color). This gives the frontend **one consistent
provenance vocabulary across ports and edges**, and one consistent certainty presentation, retiring the
4-value `Green|Amber|Inferred|Declared` enum entirely.

## Prior art

The owner asked this be checked against graph-theory and taxonomy literature rather than evaluated from
memory. Sources below were independently found by multiple panelists (noted where convergent); a
[curated reading list with hyperlinks](#reading-list) is at the end of this document.

### Data/knowledge provenance models

- **Why and Where: A Characterization of Data Provenance** (Buneman, Khanna, & Tan, 2001, ICDT); survey:
  **Provenance in Databases: Why, How, and Where** (Cheney, Chiticariu, & Tan, 2009). The classic
  **why-provenance** (witness set) / **where-provenance** (source location) / **how-provenance**
  (derivation structure) taxonomy. **Convergent finding (3+ panelists):** the proposed facets are *not* a
  clean why/where/how cut. `Scope` maps most cleanly onto why-provenance's witness notion and onto the
  base-tuple-vs-derived-tuple (EDB/IDB) distinction how-provenance is built on. `Source` doesn't map to
  why/where/how at all — it's **attribution** (see PROV, below). `Connector` maps to none of the three —
  confirming it isn't really a provenance axis at all.
- **Provenance Semirings** (Green, Karvounarakis, & Tannen, 2007, PODS); **The Semiring Framework for
  Database Provenance** (Green & Tannen, 2017, PODS, tutorial survey). Conjunction/join ↦ semiring
  multiplication (×); alternative derivations ↦ semiring addition (+). The **access-control (security)
  semiring** `(C, min, max, 0, P)` over ordered clearance levels: joint use requires the max/strictest
  clearance; alternative use requires the min. **Convergent finding (essentially every panelist):**
  "strictest property wins" for a combo (a closed cycle — every edge must simultaneously hold, a pure
  conjunction) is the *provably correct* semiring operation — but **only on axes forming a genuine
  ordered trust lattice** (`Scope`, `Source`). Applying it to nominal `Connector` is a category error.
  The framework also supplies the **+ (OR) direction** the original proposal never specified: an edge
  with multiple independent justifications takes the *best*, not an arbitrary pick.
- **Knowledge Vault: A Web-Scale Approach to Probabilistic Knowledge Fusion** (Dong et al., 2014, KDD),
  and general knowledge-graph trust/confidence propagation literature (Wikidata's rank system, YAGO's
  per-fact confidence). Confirms weakest-link propagation is standard for *conjunctive* derivations, but
  distinguishes this sharply from *probabilistic* confidence scoring (multiplicative/decaying, noisy-OR
  for corroboration) — independent confirmation that the provenance facets (categorical) and the engine's
  certainty tier (closer to the probabilistic case) should stay separate systems.
- **PROV-O: The PROV Ontology** (W3C Recommendation, 2013) — `Entity`, `Activity`, `Agent`,
  `wasGeneratedBy`, `wasDerivedFrom`, `wasAttributedTo`, `generatedAtTime`, `invalidatedAtTime`,
  `wasRevisionOf`. `Scope` ≈ `wasGeneratedBy`(fixture-authoring)/`wasDerivedFrom`(a rule); `Source` ≈
  `wasAttributedTo` an external Agent vs. the MAST engine Agent. Its bitemporal relations are the
  ready-made model for §5's temporal stamp.
- **The Anatomy of a Nanopublication** (Groth, Gibson, & Velterop, 2010). **Convergent finding (3 of 6
  panelists, unprompted):** the golds are already structurally isomorphic to a nanopublication —
  **assertion** (`edges[]`) + **provenance** (`mechanism`/rollup `witnesses`) + **publication-info**
  (`narrativeVerifiedAt`/judge verdict). This ADR promotes that structure, already present informally in
  the golds, to a first-class property of every materialized edge.

### Taxonomy / classification design

- **Ranganathan's Colon Classification / faceted classification theory (PMEST)**, with supporting
  principles on facet design (mutual exclusivity within a facet, independence across facets). The move
  from one enumerative axis to orthogonal facets is the same analytico-synthetic correction ADR 0003
  §O14/§O15 already made once for the port label itself. The panel's 8-cell walk of
  Scope×Connector×Source found most combinations meaningful, with one correlation worth naming:
  `Connector: Card` occurs freely under `Scope: Derived` (intra-card edges are projected for *every*
  corpus card, not just fixtured ones) — contrary to a naive assumption that Card implies Fixture.
  Faceted-design canon's sharpest critique landed on **Source**: collapsing an inherently graded
  authority dimension to one bit both loses information and breaks the chain the min/max aggregation
  needs — hence the graded ladder in Decision §4.

### Already-adopted prior art in this codebase (ADR 0003, for continuity)

ADR 0003 grounds the port taxonomy in typed feature structures/HPSG, description logics (the tractable EL
profile), FRP (**Functional Reactive Animation**, Elliott & Hudak, 1997, ICFP — the Event/Behavior kinds),
event sourcing, pub-sub/interceptor patterns, and synchronous dataflow (**Synchronous Data Flow**, Lee &
Messerschmitt, 1987, *Proceedings of the IEEE* — the §8 balance equations). This ADR's proposal sits on
top of that vocabulary and stays terminologically consistent with it.

**Naming collision avoided:** ADR 0003 §7 already uses "derived" for a different concept — per-attribute
over-approximation provenance (`to=graveyard•derived`, capping `Reliability`, not `Scope`). This ADR's
axis names stay literal (`Scope`/`Connector`/`Source`) rather than reusing "provenance"/"derived" loosely
across both documents.

## Migration (staged, gated — modeled on ADR 0003's own migration)

Given the now-expanded scope (Source built now; frontend unification in scope), this is a genuine
multi-stage initiative, not a single PR.

### Stage 0 — Schema definitions (the target shape)

Define, without yet wiring into the live engine:
- `PortEdge` gains `Connector` (the existing 6-valued `mechanism`), `Scope` (`structural | observed |
  corroborated | confirmed`), and a deterministic edge identity
  (`hash(fromCard, fromLabel, toCard, toLabel, mechanism)` — the recipe is already documented in
  `PortEdgeRow.cs`'s own comment, just never computed).
- Combo/cycle level gains `Source` (`officialRuling | csbVerified | csbListed | communityAttested |
  mastOnly`), extending `CycleEdgeRow.match`.
- The interaction gold schema's `source` field is formalized from today's `{csb: true, popularity}` into
  the graded enum above.
- The port row schema (frontend-facing) drops `Inferred`/`Declared` in favor of the same `Scope`
  vocabulary, `structural` for AST-parsed ports.

*Gate:* a design review (interaction-judge or equivalent) confirms the schema is internally consistent
and doesn't reopen the ADR 0003 §7 naming collision.

### Stage 1 — Engine + report plumbing

- Surface `EdgeProvenance`/`mechanism` (`Connector`) through the `card-edges.json` serialization boundary
  — cheap, already computed.
- Mint the deterministic edge identity; persist it on `PortEdgeRow` as its documented natural key.
- Build the static `FlowArm`/guard → rollup-rule-id map (gate-checked against the rollup's rule universe
  so it can't silently drift) — this is what lets `Scope` be computed for engine-materialized edges, not
  just gold-declared ones.
- Build the combo→edge `Source` back-annotation pass: join reconstructed cycles' card-sets against the
  CSB snapshot; extend the `PortEdgeRow.Popularity` stub.

*Gate:* `TopologyRollupContractTests`-style regeneration contract — the new fields must be reproducible
from committed golds + engine state, never hand-typed. Full CORE ring stays green.

### Stage 2 — TDD-loop process change (the owner's explicit addition to scope)

- **Gold schema**: `source.attestation` (the graded enum) becomes a required field on every *new*
  interaction gold, not an optional boolean.
- **`interaction-judge`**: add attestation-grade verification to its standard review checklist — does the
  claimed grade match a live CSB/rulings join — with the same rigor as its existing CR-citation and
  oracle-text checks.
- **`mast-loop/INTERACTION.md`**: update Currency C (witnessing) authoring steps to require
  `Connector`/`Scope`/`Source` determination per new gold; note Currency A (flow-arm) and B
  (precision-fix) work also needs these tagged at merge time, not just Currency C.
- **New gate**: a `FidelityRiskGateTest`-shaped check asserting every gold's declared `Source` grade is
  consistent with a live CSB join — catches a stale/wrong attestation claim the same way the existing
  gate catches stale oracle text.

*Gate:* a small pilot batch of new golds authored under the updated process, judge-reviewed, before the
full migration in Stage 3.

### Stage 3 — Migration/backfill (retroactive tagging)

- All ~63 existing interaction golds: `Connector`/`Scope` backfill is close to free (computed from
  existing `mechanism` + rollup `witnesses`); `Source` needs the CSB join, run once across the corpus.
- All 33 `combo-expected-tiers.json` pins: regenerate mechanically via a `--regenerate-expected-tiers`
  -shaped pass once Stage 1's plumbing exists — never hand-typed.
- Run this as a loop round in the same shape as this session's Currency-B/C fan-out (dispatch workers per
  batch of golds, judge-verify, serial-merge), not as one large hand-authored pass.

*Gate:* full bench suite + CORE ring green; a sampled judge re-review of backfilled `Source` grades
(spot-check, not exhaustive) confirming the CSB join produced honest grades, not defaults.

### Stage 4 — Frontend unification

- Replace the 4-value `Green|Amber|Inferred|Declared` tier with: a plain-language certainty label (from
  the unchanged computed tier) + a provenance badge cluster (`Connector`/`Scope`/`Source`), using the same
  vocabulary for ports and edges.
- Kill the literal `["Green","Amber"]` control words in `CardExplorer.tsx`; wire the tier legend to real
  (non-mock) data.

*Gate:* the Chatterfang and Deadeye card pages (ADR 0003's own worked examples) render the new badges
correctly against their known golds.

## Remaining open questions (narrow — most were resolved by the four owner decisions above)

- **Exact wording/UX for the frontend's replacement legend** — no concrete design exists yet; Stage 4
  work item.
- **Where `OfficialRuling` attestation data comes from** — no rulings-to-edge corpus exists; the ladder
  reserves the rung but Stage 2/3 won't be able to populate it without a separate rulings-ingestion
  effort. Land the ladder now; treat rulings ingestion as a later, independent initiative feeding into it.
- **Exact judge criteria for verifying a `Source` grade claim** — Stage 2's pilot batch is where this gets
  concretely specified, not this document.

## Consequences

- `combo-expected-tiers.json` / `ComboDiagnostics` / `--explain` / `FidelityRiskGateTest` (this session's
  work) gain new fields (`Connector`, `Scope`, `Source`) rather than being reworked — their core mechanism
  is unaffected.
- This is a real, multi-stage engineering initiative (Stages 0–4 above), comparable in size to ADR 0003's
  own migration, not a quick follow-on.
- The `mast-loop` skill and `interaction-judge` doctrine both change (Stage 2) — every future interaction
  gold authored after this lands carries stricter, required provenance metadata than golds do today.

## Provenance

- **Round 1 panel** (2026-07-18, 3× Opus, lens-divided: current-system audit / prior-art grounding /
  implementation feasibility).
- **Round 2 panel** (2026-07-18, 3× Opus, each independently answering the full question for
  cross-comparison, no visibility into each other's work or round 1's).
- **Convergence across all 6:** compose-don't-replace the certainty tier; `Connector` is a mechanism
  facet, not provenance; `Source` is combo-grained and should be graded; the golds are structurally
  nanopublications; a temporal axis is missing; "strictest wins" is semiring-correct for the ordinal axes
  specifically, not `Connector`.
- **Owner decisions** (2026-07-18, resolving the panel's open questions): keep the 6-valued `Connector`
  (not binary); reuse the existing promotion ladder for `Scope` (not a new binary); build `Source` now,
  at combo level, graded — reversing the panel's deferral recommendation, with an explicit TDD-loop
  process change and full fixture migration as part of this ADR; fold the frontend's `Inferred`/`Declared`
  conflation into this ADR's scope rather than treating it as separate follow-up work.
- **Origin:** project owner's critique of `combo-expected-tiers.json`'s hand-maintained `narrative` field
  (session continuation of the reporting-pipeline hardening initiative), generalized to the combo-tier
  system as a whole.

---

## Reading list

A curriculum toward the concepts above, in a suggested reading order:

1. **[Ranganathan and the faceted classification theory](https://www.redalyc.org/journal/3843/384357586006/html/)**
   — the conceptual seed: why decompose one enumerative label into independent facets.
2. **[Provenance in Databases: Why, How, and Where](https://homepages.inf.ed.ac.uk/jcheney/publications/provdbsurvey.pdf)**
   (Cheney, Chiticariu & Tan, 2009) — accessible survey of the why/where/how taxonomy.
3. **[Why and Where: A Characterization of Data Provenance](https://pdfs.semanticscholar.org/93d8/afac59687bb21bf698751890dee2fd86e11a.pdf)**
   (Buneman, Khanna & Tan, 2001) — the original paper.
4. **[The Semiring Framework for Database Provenance](https://dl.acm.org/doi/10.1145/3034786.3056125)**
   (Green & Tannen, 2017) — gentler tutorial entry into the semiring formalism.
5. **[Provenance Semirings](https://web.cs.ucdavis.edu/~green/papers/pods07.pdf)**
   (Green, Karvounarakis & Tannen, 2007) — the original, denser paper; where "strictest wins" is proven,
   not just asserted.
6. **[PROV-O: The PROV Ontology](https://www.w3.org/TR/prov-o/)** (W3C, 2013) — the standardized
   vocabulary layer.
7. **[The Anatomy of a Nanopublication](https://www.w3.org/wiki/images/c/c0/HCLSIG$$SWANSIOC$$Actions$$RhetoricalStructure$$meetings$$20100215$cwa-anatomy-nanopub-v3.pdf)**
   (Groth, Gibson & Velterop, 2010) — the closest structural template to "one edge with its own
   provenance."
8. **[Knowledge Vault](https://research.google/pubs/knowledge-vault-a-web-scale-approach-to-probabilistic-knowledge-fusion/)**
   (Dong et al., 2014) — these ideas applied at web scale.

Optional substrate prerequisites (ADR 0003's own citations, not part of this ADR's core argument):
**[Functional Reactive Animation](https://users.cs.northwestern.edu/~robby/courses/395-495-2009-winter/fran.pdf)**
(Elliott & Hudak, 1997) and
**[Synchronous Data Flow](https://ptolemy.berkeley.edu/publications/papers/87/synchdataflow/synchdataflow.pdf)**
(Lee & Messerschmitt, 1987).
