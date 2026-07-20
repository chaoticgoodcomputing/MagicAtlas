# Artifact derivation discipline — no hand-maintained derived state

## Status

**Proposed (2026-07-19; revised 2026-07-20 after design workshop).** **Replaces the previous ADR 0004
("Edge-level provenance labeling — retiring the combo-level trust color"), withdrawn.** That proposal was
accepted 2026-07-18 after a six-panelist review but never started migration; on re-reading against the
mast-loop retro that generated it, it was aimed at the *display* problem (the confusing Green/Amber chip)
while carrying an anti-drift mandate it did not structurally deliver — by its own admission, its `Scope` axis
"is **defined by** those same hand golds … adopting it does not itself reduce hand-artifact dependence." The
full withdrawn text is in git history (`0004-edge-provenance-labeling.md`).

**Two pieces are salvaged** as scoped follow-ups, not as this ADR's core:
1. the real bug where `PortEdge.Tier`'s `Provenance == CardDefined ? Green : …` lets **provenance leak into
   the certainty computation** (an intra-card edge declared certain *by construction* rather than *by
   proof*) — independent, cheap, should land regardless;
2. **plain-language tier labels** in the frontend (retiring the bare colored chip) — a standalone UX fix,
   unrelated to derivation discipline.

*(The withdrawn ADR's third component — the `Source`/CSB attestation join — is absorbed here as §4, in
reduced form: see §4's note on why `mastOnly` does not exist as a value.)*

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
| `topology-scaffold.json` `holes{}` | (declared stems) − (witnessed stems) | Hardcoded `status` said `sought` when the truth was `witnessed`. **The report *was* the bug.** Found twice, at two layers — a hardcoded status field, then a stale `proposed_stem` placeholder one layer deeper. |
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

### Two blindness classes, and what each actually needs

The retro exposed two failure modes needing *different* remedies; conflating them is why the withdrawn ADR
missed:

- **Drift** — an artifact that stopped matching reality.
- **Absence** — something never made consistent in the first place, which no report format can see. The
  composite-recursion **asymmetry between sibling code paths** (the top-level ability-effects path recursed
  into composite sub-effects; the sibling replacement-effects path did not — "nothing about that
  inconsistency is visible from outside; it only surfaces by reading the two code paths side by side"), and
  Gravecrawler's unmodeled "as long as you control a Zombie" recast condition, which four of six life-cost
  promotions route through and which is "permanently invisible to any report."

**A sharpening that reshaped this ADR (2026-07-20):** the obvious anti-drift remedy — regenerate every
derived artifact and assert byte-identity — was checked against the four failures above and **would have
caught none of them.** `holes{}` was a *hand-typed input* faithfully reproduced by regeneration; the
coarse-projections and `reason` prose were never derived artifacts at all; Suture Priest was a missing
*join*. Regeneration-as-a-gate was solving a failure mode that did not occur. The real remedies are
structural (§3), relational (§4), and invariant-based (§6) — see each.

## Decision

### 1. Every artifact is Evidence or Derived. Judgments are Evidence.

Every artifact is classified into exactly one kind, and the kind determines its rule:

- **Evidence** (hand-authored, irreducible): interaction golds, parse gold fixtures. Judge-gated; must cite
  primary sources; immutable to workers per the existing fixture-immutability gate.
- **Derived** (generated, never hand-edited): the rollup (`port-topology.json` / `port-interactions.json` +
  `.cited` twins), all `_08_Reporting` outputs, the D1–D4 dumps, expected-tier pins, backlog/demand reports.

**Domain judgments are Evidence, not ADR prose.** An earlier draft of this ADR routed judgments ("we will
not arm `emit:attach`") into ADRs. That was wrong, and contradicted ADR 0003 §8's central commitment —
*"There is no centrally-authored rule file"* — by trading a drifting data file for a drifting ADR section.
A judgment about the domain is a **witnessable claim**, so it is a gold:

- The machinery already exists. `no_loop` is an established assertion namespace (41 uses across the current
  golds), `no_parser_work_needed` another; the interaction-judge's remit already includes *"is a pruned
  (Disjoint) pair correctly impossible."* Asserted absence is existing practice, not new schema.
- It is **strictly better than a whitelist**, because the justification becomes *executable*. A whitelist's
  prose can rot silently (and did — edited three times in one session with nothing checking it). An
  asserted-absence gold fails the moment its premise stops holding. The liveness property needs no gate; the
  assertion **is** the check.

Only **architectural** decisions — ones no gold could witness, because they are about representation rather
than about Magic ("ports are stem + attribute-set rather than colon-chains"; the F1/F2/F3 kind/supergroup
rulings) — belong in ADRs.

Consequently `known-coarse-projections.json` **dissolves**: its entries (`attach`, `preventable`,
`keywordability`) each become an asserted-absence gold. See Appendix B for a fully worked example.

### 2. The closure condition — what "derived from the fixtures" means, testably

The goal "the interaction set is derived fully from the loop-generated fixtures" cannot mean *no knowledge
outside fixtures*: ADR 0003 §6 deliberately keeps the residual layer's guards/bridges **in code**
("implementations stay in code"), and a pure reconstruction of its own fixtures could never generalize to
novel pairs — which is MAST's entire purpose. The reachable, checkable form is a **bijection**.

Let `G` = interaction golds (Evidence), `R` = rollup rules, `C` = engine guards/arms/prunes:

1. **Soundness — no unwitnessed behavior.** `∀ r ∈ R ∪ C : witnesses(r) ∩ G ≠ ∅`.
2. **Completeness — no inert evidence.** `∀ g ∈ G :` every rule `g` declares is realized in `R ∪ C`.

ADR 0003 §6 already *states* (1) — "every guard is registered with its witnessing golds" — but never
**gates** it. This ADR gates it.

**Neither half needs a hand-authored registry**, which was the danger:

- **`witnesses(r)` is derived from the golds themselves.** The gold schema already carries
  `declares: { polarity, match_policy, guards, bridges }`. Golds declare the guards they witness; the rollup
  aggregates that into the guard → gold map. No new hand state, and the map lives in the rollup (regenerated,
  queryable) rather than beside the guard in code.
- **Completeness is already enforced by the loop.** The mast-loop is TDD: a worker authors the gold, drives
  the targeted test green, and the orchestrator merges only on green + judge PASS. **A gold whose claims are
  unrealized cannot land.** So (2) needs no exception list — it reduces to the one case merge discipline
  does not cover: a rollup rule with no implementing guard, which is a set difference.

**The backlog is derived from corpus demand, not from gold status.** Because golds cannot carry unrealized
claims (above), the backlog cannot be sourced from them. It is:

```
backlog = projected(corpus) − served(rollup ∪ guards) − asserted-unarmable(golds)
```

All three terms derived — projection from the corpus, service from the rollup, and the subtrahend from §1's
asserted-absence golds. This replaces `holes{}` entirely, and formalizes what the existing
`port-topology-demand` / combo-anchor surfaces already approximate. **An unserved projection with no gold is
backlog; an unserved projection with an asserted-absence gold is a decision.** That distinction is the whole
job, and it is computed, never stored.

### 3. Derived artifacts are build outputs, not repository contents

Given the Context sharpening — regeneration-as-a-gate would have caught none of the four failures — the
byte-identity gate is demoted. The structural fix is stronger and simpler: **derived artifacts are
gitignored.** An artifact that does not exist in the repository cannot go stale in the repository. Drift
becomes impossible by construction rather than detected after the fact.

Two consequences must be handled rather than assumed:

- **Cache correctness is the prerequisite.** Flowthru's cache is **code-blind** — keyed on inputs/config, not
  on the code that transforms them — so a code change does not invalidate downstream artifacts. With
  artifacts committed this produced silent staleness; with artifacts generated on demand it produces silently
  *stale builds*, which is worse. Code-aware cache keying is therefore a hard prerequisite of this section,
  not an optimization.
- **Downstream consumers need a publishing step, not a commit.** The API seed and `atlas-diag` currently read
  committed dumps. They move to consuming a published build output. See Open Questions.

### 4. Cross-track joins are first-class Derived artifacts

Suture Priest is the sharpest case in the retro **because the artifact was correct**.
`oracle-text-quarantine.json` was accurate, current, and doing its job — it was simply a Parse-track artifact
with no edge to Interaction-track combo tiering. Neither regeneration nor gitignoring touches this: both
verify a fact against itself. Only a **materialized join** verifies a fact against a *different track's*
claims.

Any fact known in track A that can invalidate a claim in track B must exist as a Derived join with its own
gate. Minimum set:

- **quarantined-oracle-text → gold → shipped combo tier** — fails if quarantined text underwrites a GREEN.
- **gold `declares` → rollup rule → engine guard** — §2's bijection, materialized.
- **declared external attestation → live source** — a claimed Commander Spellbook (or ruling) attestation
  must be reconcilable against a live snapshot, never trusted as typed.

*Note on attestation values:* the withdrawn ADR proposed a ladder ending in `mastOnly`. That value is
redundant — **if a claim is in the system, it is MAST-derived by definition.** Attestation therefore becomes
an **optional** field meaning "an external authority also attests this," with absence carrying the default.
Only externally-attested claims need the join; the common case declares nothing.

### 5. Derived prose

`combo-expected-tiers.json`'s `reason` is **rendered** from the engine's actual limiting state
(`LimitingReason` / `Gated` / `CoCostsSatisfied` / `Balanced` / worst-hop `Reliability`), not written. Human
color, where genuinely useful, moves to a separate `note` field that **no gate and no report treats as
truth**, and that may never justify a tier.

The general rule: **prose that asserts a fact must be generated; prose that adds narrative must be marked
non-authoritative.** No prose is both.

### 6. The invariant layer — catching absence, not drift

Nothing in §3 or §4 can catch something never made consistent. A distinct gate family asserts **properties**
rather than reproducing values:

- **Sibling-path consistency** — where two code paths handle the same construct, a property test asserts they
  agree (every composite-capable path recurses into sub-effects). This is a *metamorphic relation*, not an
  oracle: we cannot say what the right answer is, but we can say the two paths must give the same one.
- **Modeled-dependency completeness** — a certification riding an unmodeled condition (Gravecrawler's Zombie
  clause) must be structurally surfaced. **This needs no register of accepted over-approximations:** an
  over-approximation is *detectable* as an AST condition node the projection dropped, and ADR 0003 §7 already
  mandates per-attribute **asserted vs. derived** provenance. "Which GREENs rest on unmodeled conditions"
  is `AST condition nodes − conditions the projection consumed` — a derived report.

### 7. Dissolve `topology-scaffold.json`

Measured 2026-07-19 (Appendix A), the scaffold is **strictly subsumed** by witnessed reality: 0 of its 18
declared stems are unwitnessed, reality has witnessed 11 it never predicted, aliases are identical, axes are
a subset, and **all 6 declared capability holes are now `witnessed`** — the backlog is empty. Meanwhile its
`connectivity_predictions` (P1–P6) — its entire epistemic justification per ADR 0003 Stage 0a, *"falsifiable
connectivity predictions … the null hypotheses for the topology sweeps"* — **exist only in the scaffold and
are checked by nothing.**

The file is not demoted; it **dissolves**, each part to its correct home under §1:

| Section | Disposition |
|---|---|
| `stems_representative`, `aliases`, `attribute_axes`, `holes` | **Delete.** Fully subsumed; the rollup derives these from golds. A hand-maintained "declared half" is pure drift surface — the exact surface that failed twice. |
| `connectivity_predictions` P1–P6 | **Promote to executable topology sweeps.** The first time they would actually be falsifiable. |
| `reject` (terms deemed non-AST-derivable) | **Becomes asserted-absence golds**, one per term — a domain judgment, so Evidence per §1, not ADR prose. Converts a 4-line data block into 4 authored + judged fixtures; that cost buys executable justification. |
| `$forks_resolved` (F1/F2/F3) | **Move into ADR 0003's prose** — these are architectural rulings about kinds/supergroups that no gold could witness. |

`port-topology.json` becomes 100% witness-derived, and `holes.status` ceases to exist as a stored field — it
was always a set difference (§2).

## Prior art

### Already-adopted in this codebase (continuity)

The withdrawn ADR 0004 cited **provenance semirings** (Green, Karvounarakis & Tannen, 2007) and the
why/where/how taxonomy (Buneman, Khanna & Tan, 2001; Cheney, Chiticariu & Tan, 2009). One observation from
that work is this ADR's formal backbone: the **EDB/IDB distinction** (extensional vs. intensional database)
that how-provenance is built on. §1's Evidence/Derived split *is* EDB/IDB — base facts asserted by hand,
derived relations computed by rules — and the discipline here is the refusal to let an IDB relation be
hand-asserted. **Nanopublication** (Groth, Gibson & Velterop, 2010) — three withdrawn-ADR panelists
independently found the golds already structurally assertion + provenance + publication-info — remains the
right model for **Evidence**.

### Materialized views, and why we stopped materializing

A Derived artifact is a **materialized view** over Evidence + code; drift is a **stale view** (Gupta &
Mumick, *Maintenance of Materialized Views*, 1995). The literature's lesson is that a view goes stale not
when its own contents change but **when any of its inputs move** — which is why a gate keyed on the artifact
alone, or on a code-blind cache, is insufficient. §3 takes the stronger option the same literature implies:
where a view need not be materialized, **don't materialize it**. The cheapest stale view is the one that
doesn't exist.

### Single source of truth

Hunt & Thomas, *The Pragmatic Programmer* (1999), state DRY as a knowledge property, not a code property:
*"Every piece of knowledge must have a single, unambiguous, authoritative representation within a system."*
Each failed artifact is a second representation of knowledge that already had an authoritative one — the
holes list duplicating the gold witness set; the `reason` prose duplicating the engine's limiting state.

### Property-based and metamorphic testing (for §6)

The absence class is a **test-oracle problem**: we lack a ground-truth answer for "should this GREEN exist,"
so example-based tests cannot express it. **Property-based testing** (Claessen & Hughes, *QuickCheck*, ICFP
2000) supplies the invariant form; **metamorphic testing** (Chen, Cheung & Yiu, 1998) supplies the
sibling-path relation — when an oracle is unavailable, assert a *relation between executions* rather than an
absolute expected value. That is the formal shape of the composite-recursion asymmetry check.

### Falsifiability (for §1 and §7)

Popper's criterion (*The Logic of Scientific Discovery*) underwrites both the promotion of P1–P6 to tests and
§1's preference for asserted-absence golds over prose whitelists: a hypothesis that cannot in practice be
refuted does no epistemic work. P1–P6 have been unfalsifiable-in-practice since Stage 0a because nothing
executes them; a whitelist justification is unfalsifiable for the same reason.

## Migration (staged, gated)

### Stage 0 — Census and classification
Enumerate every artifact under `tests/**/Fixtures`, `**/Data/_08_Reporting`, `dumps/`, `libs/**/*.json`;
classify each Evidence / Derived / architectural-decision into a manifest.

*Gate:* an unclassified artifact **fails the build** — a stateless invariant (every artifact must be
classified), never a shrink-only count. This stops new hand-state accreting during the migration. Expect
forgotten artifacts: `known-families.json`, `families.json`, `blood-artist-engine.json` are still referenced
by the InteractionTriage flow despite ADR 0003 Stage 0b calling for their retirement.

### Stage 1 — Derived artifacts become build outputs
Fix Flowthru cache keying to include code (hard prerequisite). Gitignore Derived artifacts; stand up a
publishing step for the consumers that need them (API seed, `atlas-diag`).

*Gate:* no Derived artifact is tracked in git; a clean checkout can produce every one of them; CORE ring
green.

### Stage 2 — Dissolve the scaffold
Execute §7's four-way split: delete the subsumed sections, promote P1–P6 to sweeps, convert `reject` to
asserted-absence golds, move F1/F2/F3 into ADR 0003.

*Gate:* the sweeps run and pass (or produce a real, judged falsification); `topology-scaffold.json` is gone;
no `status` field survives anywhere it could be hand-typed.

### Stage 3 — Cross-track joins
Materialize §4's join set with gates. Start with quarantine → gold → tier: it has a known historical failure
(Suture Priest) that serves as the gate's acceptance test.

*Gate:* the quarantine join demonstrably fails on a reconstructed Suture-Priest-shaped input.

### Stage 4 — Derived prose and the coarse-projection dissolution
Render `reason` from engine state; demote human prose to a non-authoritative `note`; convert
`known-coarse-projections.json`'s entries to asserted-absence golds (§1) and delete the file.

*Gate:* no gate or report consumes a hand-written field; the rendered reasons reproduce the current
hand-written meaning on a judge-sampled subset (a correctness check, not a diff).

### Stage 5 — The invariant layer
Land §6: sibling-path metamorphic properties, and modeled-dependency completeness derived from AST condition
nodes.

*Gate:* the composite-recursion asymmetry is caught when deliberately reintroduced; "which GREENs rest on
unmodeled conditions" is answerable by query.

### Stage 6 — Close the bijection
Gate §2: no rule or guard without a witnessing gold (via `declares` → rollup); no declared rule unrealized.

*Gate:* the bijection holds, or every exception is registered **as a gold**, never as a list.

## Consequences

- **The mast-loop and interaction-judge doctrine change.** A worker may no longer "update" a status,
  justification, or reason; they author Evidence and regenerate. The judge's checklist gains two lines: *is
  this claim derived or asserted?*, and for asserted-absence golds a **falsification prompt** — *what change
  would make this absence wrong, and would the assertion catch it?* — so an absence gold is not reviewed as a
  degenerate positive.
- **Asserted-absence golds strengthen over time, by design.** `no_arm` is evaluated against the *current*
  witnessed stem universe, not the universe at authoring time. A gold can therefore go red because a new stem
  appeared and unexpectedly matched — with no one touching the card. This is intended: it forces the
  judgment to be re-derived rather than silently outliving its premise.
- **Some reports get worse before better.** Generated prose is blunter than a rigorous worker's hand-written
  `reason`. Correct trade: an honest blunt sentence beats an eloquent one that stopped being true.
- **This does not reduce the golds' load-bearing role — it makes it exact.** The system leans on hand
  evidence exactly as much as today; the difference is that the amount becomes *measurable* (§2) and nothing
  else can masquerade as it.

## Open questions

- **How do the API seed and `atlas-diag` consume Derived artifacts once gitignored (§3)?** They read
  committed dumps today. Options: a published build artifact, a seed-on-boot generation step, or an artifact
  store. Unresolved; blocks Stage 1's completion, not its start.
- **How is a topology change reviewed without a committed diff?** Committed derived artifacts served as a PR
  review surface ("this change moved 12 edges"). Likely mitigation: CI emits the derived diff as a build
  artifact or PR comment. Needs a concrete mechanism before Stage 1 lands.
- **Does `no_arm` need a bounded evaluation universe for cost?** Semantics are settled (current taxonomy,
  growing), but evaluating every absence gold against every witnessed consume stem on every run has a cost
  curve worth measuring at Stage 2.

## Provenance

- **Origin:** the five-batch `mast-loop` interaction run retro (2026-07-18) — 43 commits, CORE ring green
  throughout — whose finding was that reporting ranked well but was blind to actual defects, and whose four
  named stale artifacts are this ADR's Context table.
- **Owner principle** (2026-07-18): "the code and data are ultimately the source of truth — we should
  consider anything being hand rolled or hand-updated evil."
- **Supersession** (2026-07-19): the previous ADR 0004 withdrawn as mis-aimed at the retro that generated it.
- **Measured evidence** (2026-07-19): the scaffold divergence in Appendix A, computed from the committed
  artifacts, converted §7 from "demote the scaffold" to "dissolve it."
- **Design workshop** (2026-07-20), owner decisions: judgments are Evidence (golds), not ADR prose —
  correcting a draft that contradicted ADR 0003 §8; derived artifacts are gitignored rather than
  regeneration-gated, after the finding that regeneration would have caught none of the four failures; the
  guard→witness map is derived from golds' `declares` and lives in the rollup; the backlog is derived from
  corpus demand; `mastOnly` attestation dropped as redundant; absence assertions strengthen with the
  taxonomy; `reject` converts to golds.

---

## Appendix A — measured scaffold divergence (2026-07-19)

Computed from `topology-scaffold.json` against the generated `rollup/port-topology.json` (63 golds):

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
CONNECTIVITY_PREDICTIONS  P1–P6 in scaffold · ABSENT from rollup — never checked
CARRIED NOWHERE           reject, event_verbs_no_supergroup, stems_representative
```

The five `modification:*` / `cards:*` stems in the witnessed-but-unpredicted list are precisely the capability
holes being filled — the accretion loop closing its own backlog, which is the behaviour ADR 0003 §8 predicted
and which the scaffold no longer adds information to.

## Appendix B — worked asserted-absence gold

The pattern §1 replaces `known-coarse-projections.json` with. `emit:attach` is a real, recurring surface —
**141 ports corpus-wide** (Accorder's Shield, Aettir and Priwen, Amorphous Axe, Animate Dead, …).

**The trap this design avoids.** The naive form asserts *"this card produces zero edges."* For a single-card
gold that is **vacuous** — with no partner cards, zero edges is trivially true, and the assertion would keep
passing after someone armed attach. The fix is to assert against the **matcher**, not a materialized edge
set: *for every witnessed consume stem, `SelectArm` returns null*. Total, cheap, and non-vacuous.

The second subtlety is what makes the gold load-bearing: `emit:attach` has **no `IPortFamily` recognizer**, so
it carries no `PortStructure` at all. A backlog item is *also* unstructured — so the gold's real job is to
**disambiguate intentional terminality from an unfilled gap**, which is exactly the subtrahend in §2's
backlog formula.

```json
{
  "id": "accorders-shield-attach-terminal",
  "unit": "single-card",
  "cards": ["Accorder's Shield"],
  "source": {
    "note": "Asserted-ABSENCE witness for the coarse emit:attach projection (141 ports corpus-wide). The claim under review is NOT 'attach is unparseable' — it parses — but 'attaching raises no flowing resource, so no flow arm follows from it'. Attachment changes an object's attached-to relation (CR 301.5 / 701.3); it emits no event a trigger subscribes to and decrements no store, so it has no ADR-0003 §4 supergroup placement and no consume it could refuel. Terminality is the correct modeling, not a gap."
  },
  "judge": { "verdict": "PASS", "ref": "interaction-judge <date> — attach terminality, CR 301.5/701.3" },
  "ports": {
    "Accorder's Shield": [
      {
        "id": "P0", "side": "emit", "stem": "attach", "structured": false,
        "note": "THE WITNESS — the equip ability's attach effect. Deliberately carries no PortStructure: no family recognizes it, because no stem in the §4 root would be honest for it."
      }
    ]
  },
  "edges": [],
  "declares": { "polarity": [], "match_policy": [], "guards": [], "bridges": [] },
  "assertions": [
    {
      "claim": "no_arm[P0]",
      "because": "For every witnessed consume stem in the rollup, SelectArm(P0, consume) is null — attach connects to nothing by construction. Sibling of the existing no_loop claim. Evaluated against the CURRENT taxonomy, so the assertion strengthens as stems accrete; if a future change arms attach, THIS GOLD GOES RED, forcing the judgment to be re-derived rather than silently outdated."
    },
    {
      "claim": "P0.structured == false",
      "because": "Intentional structural terminality, not an unconverted family. This is what removes emit:attach from the derived backlog (projected − served − asserted-unarmable); without this gold it would correctly appear as unserved demand."
    }
  ],
  "cr": ["301.5", "701.3"]
}
```
