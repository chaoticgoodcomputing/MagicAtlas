# Artifact derivation discipline — no hand-maintained derived state

## Status

**Proposed (2026-07-19).** **Replaces the previous ADR 0004 ("Edge-level provenance labeling — retiring the
combo-level trust color"), withdrawn.** That proposal was accepted 2026-07-18 after a six-panelist review but
never started migration; on re-reading against the mast-loop retro that generated it, it was aimed at the
*display* problem (the confusing Green/Amber chip) while carrying an anti-drift mandate it did not
structurally deliver — by its own admission, its `Scope` axis "is **defined by** those same hand golds …
adopting it does not itself reduce hand-artifact dependence." The full withdrawn text is in git history
(`0004-edge-provenance-labeling.md`).

**Three pieces are salvaged** and survive as scoped follow-ups, not as this ADR's core:
1. the real bug where `PortEdge.Tier`'s `Provenance == CardDefined ? Green : …` lets **provenance leak into
   the certainty computation** (an intra-card edge declared certain *by construction* rather than *by
   proof*) — independent, cheap, should land regardless;
2. the **`Source`/CSB live-join gate** pattern, which is literally a cross-track join and is absorbed here
   as §4;
3. **plain-language tier labels** in the frontend (retiring the bare colored chip) — a standalone UX fix,
   unrelated to derivation discipline.

Refines [ADR 0003](0003-taxonomy-redesign.md): this ADR governs the *artifacts* ADR 0003's accretion loop
(§8) produces and consumes. It does not touch the port/stem taxonomy or the certainty computation.

## Context — the retro that generated this

A five-batch `mast-loop` interaction run (43 commits, CORE ring green at every merge boundary) shipped real
engine capability — a life-cost-balance dimension, the Chatterfang × Pitiless AMBER→GREEN promotion, two
latent false-GREENs caught *before* landing. Its retrospective produced a sharper finding than any of those
fixes:

> Reporting was good at ranking **where to look**; it was essentially blind to **what was actually wrong**.

The defects were found by *independent re-verification compounding on itself* — workers auditing siblings of
already-"confirmed" combos, judges re-deriving claims from primary sources instead of trusting worker
reports, a fix's own author catching their own near-miss mid-task. Not by reports.

Four artifacts were named as the concrete failure surface. **Every one is a computation or a join that
someone froze into text:**

| Artifact | The frozen computation | How it failed |
|---|---|---|
| `topology-scaffold.json` `holes{}` | (declared stems) − (witnessed stems) | Hardcoded `status` said `sought` when the truth was `witnessed`. **The report *was* the bug.** Found twice, at two different layers — a hardcoded status field, then a stale `proposed_stem` placeholder one layer deeper. |
| `known-coarse-projections.json` | a claim about current parser/corpus state | Hand-written prose "justification", edited three times in one session; nothing checks whether the prose is still true as the corpus/parser move under it. |
| `combo-expected-tiers.json` `reason` | the engine's own `LimitingReason` / `Gated` / `CoCostsSatisfied` | 100% hand-written prose per combo. Honest today because rigorous workers wrote it; free to drift the moment the engine changes and nobody rewrites the sentence. |
| `oracle-text-quarantine.json` ↔ combo tiers | a **join** between the Parse and Interaction tracks | Suture Priest sat on the fidelity quarantine the whole time while underwriting a shipped GREEN. **The artifact was never wrong — it was merely disconnected.** |

The owner's principle, stated in that retro: *the code and data are ultimately the source of truth — anything
hand-rolled or hand-updated is evil.*

### The distinction that makes the principle actionable

"No hand-maintained artifacts" is too blunt to be true: ADR 0003 §8 makes per-gold interaction fixtures **the
only hand-authored artifact** *by design*, and they are the irreducible seed of the whole accretion loop. The
operative split is not hand-written vs. generated, it is:

- **Evidence** — hand-authored *primary observation* (an interaction gold; a parse gold fixture). Correct to
  be hand-authored. Judge-gated; cites primary sources (CR / oracle text).
- **Derived claims** — a status, a justification, a reason-string, a whitelist membership: an assertion
  *about* the data. **This is the enemy.** Every failure above is one of these.

### Two distinct blindness classes (both in scope)

The retro exposed two failure modes needing *different* remedies; conflating them is why the withdrawn ADR
missed:

- **Drift** — an artifact that stopped matching reality (all four above). Caught by **regeneration**.
- **Absence** — something never made consistent in the first place, which no report format can see. The
  composite-recursion **asymmetry between sibling code paths** (the top-level ability-effects path recursed
  into composite sub-effects; the sibling replacement-effects path did not — "nothing about that
  inconsistency is visible from outside; it only surfaces by reading the two code paths side by side"), and
  Gravecrawler's unmodeled "as long as you control a Zombie" recast condition, which four of six life-cost
  promotions route through and which is "permanently invisible to any report." Caught only by **invariants**.

## Decision

### 1. Every artifact is Evidence or Derived. Nothing in between.

Every artifact in the repository is classified into exactly one kind, and the kind determines its rule:

- **Evidence** (hand-authored, irreducible): interaction golds, parse gold fixtures. Judge-gated; must cite
  primary sources; immutable to workers per the existing fixture-immutability gate.
- **Derived** (generated, never hand-edited): the rollup (`port-topology.json` / `port-interactions.json` +
  their `.cited` twins), all `_08_Reporting` outputs, the D1–D4 dumps, expected-tier pins, hole/backlog
  status, census reports.

Two things that look like artifacts are **not** artifacts, and move out of data files entirely:

- **Judgments** (a decision *not* to do something — e.g. "we will not arm `emit:attach`") belong in an **ADR
  or decision record**, as prose owned by a human, never in a data file that reporting reads as truth.
- **Predictions** (falsifiable hypotheses — the scaffold's connectivity claims) belong in **tests**. A
  prediction that is not executable is not falsifiable, and therefore does no epistemic work.

*A hybrid is allowed in exactly one shape, split at the field level:* a judgment's **content** is Evidence,
but its **membership and liveness are Derived**. `known-coarse-projections.json` becomes: the
decision-not-to-arm is a cited judgment; the *set* of coarse projections is computed from the corpus; and a
whitelist entry whose projection no longer exists is flagged **dead** by a gate.

### 2. The closure condition — what "derived from the fixtures" means, testably

The goal "the interaction set is derived fully from the loop-generated fixtures" cannot mean *no knowledge
outside fixtures*: ADR 0003 §6 deliberately keeps the residual layer's guards/bridges/identity-checks **in
code** ("implementations stay in code"), and a pure reconstruction of its own fixtures could never generalize
to novel pairs — which is MAST's entire purpose. The reachable, checkable form is a **bijection**.

Let `G` = interaction golds (Evidence), `R` = rollup rules, `C` = engine guards/arms/prunes:

1. **Soundness — no unwitnessed behavior.** `∀ r ∈ R ∪ C : witnesses(r) ∩ G ≠ ∅`.
2. **Completeness — no inert evidence.** `∀ g ∈ G :` every claim `g` makes is realized by some `r ∈ R ∪ C`.
3. **Derivation — no hand-maintained middle.** `∀` artifact `A ∉ G : regenerate(A) ≡ A`, byte-for-byte.

ADR 0003 §6 already *states* (1) — "every guard is registered with its witnessing golds" — but never
**gates** it. This ADR gates it. (1) + (2) is the bijection; (3) is §3. Together they are the precise
statement of the end-state, and each is a test rather than an aspiration.

### 3. Regeneration is a no-op — the primary anti-drift gate

For every Derived artifact, CI regenerates it from (code + Evidence) and asserts **byte-identity** with the
committed copy. This makes hand-editing a Derived artifact structurally unable to land, which is strictly
stronger than review discipline: it removes the failure mode instead of policing it.

> **⚠️ The gate must force re-derivation.** Flowthru's cache is **code-blind** — keyed on inputs/config, not
> on the code that transforms them. A regeneration gate that reads a cached artifact passes **vacuously** and
> proves nothing; it would have been green through every failure in the retro. Each Derived artifact's gate
> must bust its step's cache (the established recipe: remove the step's records, re-run with a targeted
> `--only <Step>`) or the gate is theater. **This is the single most likely way this ADR gets implemented
> wrong.**

An artifact that *cannot* be regenerated is either misclassified (it is actually Evidence or a judgment) or
is itself the bug. There is no third answer.

### 4. Cross-track joins are first-class Derived artifacts

Suture Priest is the sharpest case in the retro **because the artifact was correct**.
`oracle-text-quarantine.json` was accurate, current, and doing its job — it was simply a Parse-track artifact
with no edge to Interaction-track combo tiering. Drift gates verify a fact against itself; only a
**materialized join** verifies a fact against a *different track's* claims.

Therefore: any fact known in track A that can invalidate a claim in track B must exist as a Derived join
artifact with its own gate. Minimum set:

- **quarantined-oracle-text → gold → shipped combo tier** — fails if quarantined text underwrites a GREEN.
- **gold → rollup rule → engine guard** — §2's bijection, materialized.
- **declared attestation → live external source** — the salvaged `Source`/CSB join: a declared attestation
  grade must be reconcilable against a live CSB snapshot, never trusted as typed.

### 5. Derived prose

`combo-expected-tiers.json`'s `reason` is **rendered** from the engine's actual limiting state
(`LimitingReason` / `Gated` / `CoCostsSatisfied` / `Balanced` / worst-hop `Reliability`), not written. Human
color, where genuinely useful, moves to a separate `note` field that **no gate and no report treats as
truth**, and that may never justify a tier.

The general rule: **prose that asserts a fact must be generated; prose that adds narrative must be marked
non-authoritative.** No prose is both.

### 6. The invariant layer — catching absence, not just drift

Regeneration and joins cannot catch something never made consistent. A second, distinct gate family asserts
**properties** rather than reproducing values:

- **Sibling-path consistency** — where two code paths handle the same construct, a property test asserts they
  agree (the composite-recursion asymmetry: every composite-capable path recurses into sub-effects). This is
  a *metamorphic relation*, not an oracle: we cannot say what the right answer is, but we can say the two
  paths must give the same one.
- **Modeled-dependency completeness** — every GREEN's dependency set is fully modeled; a certification riding
  an unmodeled condition (Gravecrawler's Zombie clause) must be **structurally surfaced**, not left to a
  gold's own footnote. An accepted over-approximation stays legal — but it must be **declared and
  enumerable**, so "which GREENs rest on unmodeled conditions" is a query, not an act of memory.

### 7. Dissolve `topology-scaffold.json`

Measured 2026-07-19 (Appendix), the scaffold is **strictly subsumed** by witnessed reality: 0 of its 18
declared stems are unwitnessed, reality has witnessed 11 stems it never predicted, its aliases are identical,
its axes are a subset, and **all 6 declared capability holes are now `witnessed`** — the targeted-witnessing
backlog is empty. Meanwhile its `connectivity_predictions` (P1–P6) — its entire epistemic justification per
ADR 0003 Stage 0a, *"falsifiable connectivity predictions … the null hypotheses for the topology sweeps"* —
**exist only in the scaffold and are checked by nothing.**

The file therefore is not demoted; it **dissolves**, each part to its correct home under §1:

| Section | Disposition |
|---|---|
| `stems_representative`, `aliases`, `attribute_axes`, `holes` | **Delete.** Fully subsumed; the rollup already derives these from golds. Retaining a hand-maintained "declared half" is pure drift surface — the exact surface that failed twice. |
| `connectivity_predictions` P1–P6 | **Promote to executable topology sweeps.** The first time they would actually be falsifiable. |
| `reject`, `$forks_resolved` (F1/F2/F3) | **Move into ADR 0003's prose** as the decision record they already are. |

`port-topology.json` becomes 100% witness-derived, and `holes.status` ceases to exist as a stored field — it
was always a set difference. Future backlog items are declared **with the golds as targeted-witnessing
tasks** (declaration = Evidence, status = Derived), never as a section of a topology file.

## Prior art

### Already-adopted in this codebase (continuity)

The withdrawn ADR 0004 cited **provenance semirings** (Green, Karvounarakis & Tannen, 2007) and the
why/where/how provenance taxonomy (Buneman, Khanna & Tan, 2001; Cheney, Chiticariu & Tan, 2009). One
observation from that work is this ADR's formal backbone: the **EDB/IDB distinction** (extensional vs.
intensional database) that how-provenance is built on. §1's Evidence/Derived split *is* EDB/IDB — base facts
asserted by hand, derived relations computed by rules — and the discipline imposed here is simply the refusal
to let an IDB relation be hand-asserted.

**Nanopublication** (Groth, Gibson & Velterop, 2010) — the convergent finding of three withdrawn-ADR
panelists that the golds are already structurally assertion + provenance + publication-info — remains the
right model for the **Evidence** kind and needs no change.

### Materialized views and view maintenance

A Derived artifact is exactly a **materialized view** over Evidence + code; drift is a **stale view**; §3's
regeneration gate is **view maintenance**; §4's cross-track joins are the view's **join dependencies** made
explicit. Standard reference: Gupta & Mumick, *Maintenance of Materialized Views: Problems, Techniques, and
Applications* (1995). The relevant lesson is the one the retro learned the hard way: a view goes stale not
when its own contents change but **when any of its inputs move** — which is why a gate keyed on the artifact
alone (or on a code-blind cache, §3) is insufficient.

### Single source of truth

Hunt & Thomas, *The Pragmatic Programmer* (1999), state DRY as a knowledge property, not a code property:
*"Every piece of knowledge must have a single, unambiguous, authoritative representation within a system."*
Each failed artifact is a second representation of knowledge that already had an authoritative one — the
holes list duplicating the gold witness set; the `reason` prose duplicating the engine's limiting state.

### Reproducible builds

§3's **byte-identical regeneration** gate is the reproducible-builds discipline
([reproducible-builds.org](https://reproducible-builds.org/)) applied to data artifacts rather than binaries:
given the same source, the build must produce bit-for-bit identical output, and any divergence is a defect
*by definition* rather than a judgment call. Adopting it removes the need to *decide* whether a diff is
acceptable.

### Property-based and metamorphic testing (for §6)

The absence class is a **test-oracle problem**: we lack a ground-truth answer for "should this GREEN exist,"
so example-based tests cannot express it. **Property-based testing** (Claessen & Hughes, *QuickCheck*, ICFP
2000) supplies the invariant form; **metamorphic testing** (Chen, Cheung & Yiu, 1998) supplies exactly the
sibling-path relation — when an oracle is unavailable, assert a *relation between executions* ("these two
code paths must agree on this construct") rather than an absolute expected value. That is the precise formal
shape of the composite-recursion asymmetry check.

### Falsifiability (for §7)

Popper's criterion (*The Logic of Scientific Discovery*) is the argument for promoting the scaffold's
connectivity predictions to tests rather than deleting them or keeping them as data: a hypothesis that cannot
in practice be refuted does no epistemic work. P1–P6 have been unfalsifiable-in-practice since Stage 0a
because nothing executes them.

## Migration (staged, gated)

### Stage 0 — Census and classification

Enumerate every artifact under `tests/**/Fixtures`, `**/Data/_08_Reporting`, `dumps/`, and `libs/**/*.json`;
classify each Evidence / Derived / judgment / prediction into a manifest.

*Gate:* an artifact that is not classified **fails the build** — a stateless invariant (every artifact must
be classified), never a shrink-only count. This is what stops new hand-state accreting while the rest of the
migration runs. Expect forgotten artifacts: `known-families.json`, `families.json`, and
`blood-artist-engine.json` are still referenced by the InteractionTriage flow despite ADR 0003 Stage 0b
calling for their retirement once witnessed.

### Stage 1 — The regeneration contract

Give every Derived artifact a `--regenerate` path and a CI gate asserting byte-identical regeneration, **with
forced cache invalidation per §3's warning**. Anything that cannot be regenerated is reclassified or fixed.

*Gate:* regeneration is a no-op for every Derived artifact, proven against a busted cache. CORE ring green.

### Stage 2 — Dissolve the scaffold

Execute §7's three-way split. `port-topology.json` becomes witness-only; P1–P6 become executable sweeps;
`reject` / `$forks_resolved` move into ADR 0003.

*Gate:* the sweeps run and pass (or produce a real, judged falsification); `topology-scaffold.json` is gone;
no `status` field survives anywhere it could be hand-typed.

### Stage 3 — Cross-track joins

Materialize §4's join set with gates. Start with quarantine → gold → tier, because it has a known historical
failure (Suture Priest) that the gate must retroactively catch as its acceptance test.

*Gate:* each join is a Derived artifact under Stage 1's contract; the quarantine join demonstrably fails on a
reconstructed Suture-Priest-shaped input.

### Stage 4 — Derived prose

Render `reason` from engine state; demote human prose to a non-authoritative `note`; split
`known-coarse-projections` per §1 (judgment cited, membership/liveness derived, dead entries flagged).

*Gate:* no gate or report consumes a hand-written field; the rendered reasons reproduce the current
hand-written meaning on a judge-sampled subset (a correctness check, not a diff).

### Stage 5 — The invariant layer

Land §6: sibling-path metamorphic properties, and modeled-dependency completeness with an enumerable register
of accepted over-approximations.

*Gate:* the composite-recursion asymmetry is caught by a property test when deliberately reintroduced;
"which GREENs rest on unmodeled conditions" is answerable by query.

### Stage 6 — Close the bijection

Gate §2's soundness and completeness: no rule or guard without a witnessing gold; no gold whose claims are
unrealized.

*Gate:* the bijection holds, or every exception is explicitly registered with a cited reason.

## Consequences

- **The mast-loop and interaction-judge doctrine change.** A worker may no longer "update" a status,
  justification, or reason; they author Evidence and regenerate. The judge's checklist gains: *is this claim
  derived or asserted?*
- **Some reports get worse before they get better.** Generated prose is blunter than the rich hand-written
  `reason` strings a rigorous worker produces. This is the correct trade: an honest blunt sentence beats an
  eloquent one that silently stopped being true.
- **The three salvaged items** from the withdrawn ADR proceed independently; only the CSB join is scoped into
  this ADR (§4).
- **This does not reduce the golds' load-bearing role — it makes it exact.** The system will lean on hand
  evidence exactly as much as it does today; the difference is that the amount becomes *measurable* (§2), and
  nothing else can masquerade as it.

## Open questions

- **Cache-busting ergonomics.** Forced re-derivation per §3 makes the gate honest but slow. Whether the full
  contract runs per-PR or on a nightly / pre-merge ring is unresolved; settle empirically once Stage 1
  measures the cost.
- **Granularity of `witnesses(r)` for code guards (§2.1).** A guard → gold-id registry must be authored once;
  whether it lives beside the guard in code or in the rollup is a Stage-6 decision.
- **What replaces `holes` as a backlog surface.** §7 puts targeted-witnessing tasks with the golds; the
  concrete shape (task file? gold frontmatter? issue tracker?) is deferred to Stage 2.

## Provenance

- **Origin:** the five-batch `mast-loop` interaction run retro (2026-07-18) — 43 commits, CORE ring green
  throughout — whose finding was that reporting ranked well but was blind to actual defects, and whose four
  named stale artifacts are this ADR's Context table.
- **Owner principle** (2026-07-18): "the code and data are ultimately the source of truth — we should
  consider anything being hand rolled or hand-updated evil."
- **Supersession** (2026-07-19): the previous ADR 0004 (edge-provenance-labeling; accepted 2026-07-18 on a
  six-panelist review, migration never started) withdrawn as mis-aimed at the retro that generated it; three
  components salvaged (see Status).
- **Measured evidence** (2026-07-19): the scaffold-vs-rollup divergence in the Appendix, computed directly
  from the committed artifacts, is what converted §7 from "demote the scaffold" to "dissolve it."

---

## Appendix — measured scaffold divergence (2026-07-19)

Computed from `tests/magic-ast-tests/Fixtures/Interactions/topology-scaffold.json` against the generated
`.../rollup/port-topology.json` (63 golds):

```
STEMS          scaffold declared 18 · rollup witnessed 29
  declared but NOT witnessed ...... 0
  witnessed but NOT predicted ..... 11
      cards:search, cards:select, cast, combat-presence, copy, damage-dealt,
      dice-rolled, modification:prevention, modification:restriction,
      modification:spell, recur
ATTRIBUTE_AXES scaffold 21 · rollup 22   (rollup-only: subject)
ALIASES        10 / 10                   identical
HOLES          6 / 6 — ALL status=witnessed (backlog empty)
      cost-modification  <- stone-calendar, thorn-of-amethyst
      library-search     <- diabolic-tutor, vampiric-tutor
      library-selection  <- serum-visions
      non-play-zone-move <- archaeomancer, cloudstone-curio, …
      prevention         <- fog, glacial-chasm
      restriction-grant  <- moat, pacifism
CONNECTIVITY_PREDICTIONS  P1–P6 present in scaffold · ABSENT from rollup — never checked
CARRIED NOWHERE           reject, event_verbs_no_supergroup, stems_representative
```

The five `modification:*` / `cards:*` stems in the witnessed-but-unpredicted list are precisely the capability
holes being filled — the accretion loop closing its own backlog, which is the behaviour ADR 0003 §8 predicted
and which the scaffold no longer adds information to.
