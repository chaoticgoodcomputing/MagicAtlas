# Artifact derivation discipline — no hand-maintained derived state

## Status

**Proposed (2026-07-19; revised 2026-07-20 after design workshop).** **Replaces the previous ADR 0004
("Edge-level provenance labeling — retiring the combo-level trust color"), withdrawn.** That proposal was
accepted 2026-07-18 after a six-panelist review but never started migration; on re-reading against the
mast-loop retro that generated it, it was aimed at the *display* problem (the confusing Green/Amber chip)
while carrying an anti-drift mandate it did not structurally deliver — by its own admission, its `Scope` axis
"is **defined by** those same hand golds … adopting it does not itself reduce hand-artifact dependence." The
full withdrawn text is in git history (`0004-edge-provenance-labeling.md`).

**Two pieces are salvaged:**
1. the real bug where `PortEdge.Tier`'s `Provenance == CardDefined ? Green : …` lets **provenance leak into
   the certainty computation** (an intra-card edge declared certain *by construction* rather than *by
   proof*) — independent, cheap, should land regardless;
2. the withdrawn ADR's §1 — *keep the certainty computation, retire the bare colour* — which turned out not
   to be a detachable UX fix at all. It is **absorbed into §5**, where retiring the stored tier in favour of
   its computed attribute vector is simultaneously the fix for the opaque frontend chip *and* the fix for the
   hand-maintained tier pin. The withdrawn version got the display half right and left the hand-maintenance
   untouched; §5 does both.

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

- **Evidence** (hand-authored, irreducible): interaction golds, parse gold fixtures, **and combo
  expectations** — an *expectation is not an output* (§5). Judge-gated; must cite primary sources; immutable
  to workers per the existing fixture-immutability gate.
- **Derived** (generated, never hand-edited): the rollup (`port-topology.json` / `port-interactions.json` +
  `.cited` twins), all `_08_Reporting` outputs, the D1–D4 dumps, backlog/demand reports.
  Derived artifacts are gitignored by default; the rollup is the one committed exception, and pays for it
  with a regeneration gate (§3).

A third input class is neither: **external source data** (Scryfall, the Commander Spellbook snapshot) is
ingested, not authored, and versioned by its fetch. The complete derivation base is therefore
`Derived = f(external sources, Evidence, code)` — three inputs, nothing else.

**Derivation is Flowthru's job; NUnit's job is gates.** Every artifact, census, join, and report this ADR
introduces is produced by a **Flowthru flow** landing in `_08_Reporting` — Flowthru is the analytics and
data-processing backbone, and a derivation that lives anywhere else is itself a hand-rolled artifact in
disguise. NUnit is reserved for the **gates** that assert over those outputs (unclassified-artifact fails
build, regeneration byte-identity, `no_arm`, the bijection). Where this ADR says "compute X," read "a
Flowthru step computes X"; where it says "gate," read "an NUnit test asserts it."

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

> **Correction (2026-07-20, issue #28 — read with Appendix C).** Two factual errors in the paragraph above.
> (a) The file does not hold three entries; it holds **288** (220 `effectType`, 16 `costType`,
> 46 `triggerEvent`, 6 `restriction`). (b) Under the inverted prior — *an unserved projection is backlog
> until a CR argument proves no consume could ever exist* — **285 of them are backlog, 1 is already a
> decision (`anyNumberInDeck`), and 2 are not port candidates at all** (`unparsed` / `unstructured`, which
> denote parse failure rather than any Magic concept). Converting the file to golds is therefore not the
> disposition; **deriving it away is** (§2's formula computes the same names from
> `all discriminators − PortWalkProjection − asserted-unarmable(golds)`), which is issue #32's job. The file
> survives until #32 ships, because `PortWalkExhaustivenessTests` is currently the only executable statement
> of the blind-spot set. Adjudication and evidence: **Appendix C**.

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

### 3. Derived artifacts are build outputs — with one deliberate exception class

Everything downstream derives from exactly three inputs: **external source data** (the Scryfall corpus, the
Commander Spellbook snapshot), **Evidence** (the loop-authored fixtures), and **code**. Nothing else is an
input, so nothing else needs to be stored — running the Flowthru pipeline reproduces the rest.

Given the Context sharpening — regeneration-as-a-gate would have caught none of the four failures — the
default is structural rather than procedural: **Derived artifacts are gitignored.** An artifact that does not
exist in the repository cannot go stale in it.

**The exception, and the rule for granting it.** One property is lost by not committing: the **inter-run
diff**. The retro's actual working mechanism was not a gate — it was *independent re-verification compounding
on itself*, humans and agents reading changes and refusing to take a layer's word for it. A committed derived
artifact serves that mechanism directly: "this loop run added three stems and changed two rules" becomes
visible in review. Where that diff is genuinely informative, the artifact stays committed.

**The rollup** (`port-topology.json`, `port-interactions.json`, and their `.cited` twins) is that exception.
It is the accretion loop's visible output — the surface on which taxonomy drift is legible to a reviewer —
and it is small. The `.cited` diff is the highest-value of the four: it is where **witness attribution**
changes surface. (Per ADR 0003 §8 the lean pair is a projection of the verbose pair, generated in one pass,
so the two cannot drift from each other.)

Granting the exception has a price — and this is where the byte-identity check earns its place after all,
narrowly scoped: **a committed Derived artifact carries a regeneration gate** (regenerate, assert
byte-identity), because a committed file can be hand-edited or left stale. An uncommitted one needs no gate.
The decision procedure for any future artifact is therefore a single question: **is its diff worth a gate?**
If yes, commit and gate it; if no, gitignore it.

**Cache correctness is a hard prerequisite either way.** Flowthru's cache is **code-blind** — keyed on
inputs/config, not on the code that transforms them. With artifacts committed this produced silent staleness;
with artifacts generated on demand it produces silently *stale builds*, which is worse. Code-aware keying is
a prerequisite of this section, not an optimization.

**Downstream consumers need a publishing step, not a commit.** The API seed and `atlas-diag` read committed
dumps today; for gitignored artifacts they move to consuming a published build output. See Open Questions.

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

### 5. Retire the stored tier; pin the derived attribute vector

`combo-expected-tiers.json` bundles two different jobs, and both are broken in the same way — an **opaque
rollup value is stored where computed attributes belong.**

**The tier is already derived; it is just stored as if it weren't.** `Green`/`Amber` is a lossy summary of
attributes the engine computes anyway — `Firable`, `CoCostsSatisfied`, `Balanced`, `LifeBalanced`,
`Productive`, worst-hop `Reliability`/`Overlap`, `LimitingReason`. Collapsing them to a colour discards
exactly the information a reader needs, which is why the vocabulary is opaque out of context — most acutely
in the frontend, where a bare `"Green"`/`"Amber"` chip is unreadable without a legend.

**Decision:** the tier stops being a stored value and becomes a **presentation** of the attribute vector.

1. **What is pinned is the attribute vector, not a colour.** The expectation becomes judge-set **Evidence**
   (§1): *this cycle is Firable, Balanced, CoCosts-satisfied, worst-hop Reliability = Yes.* A change then
   fails the gate naming **which attribute moved**, instead of "Green → Amber, reason: ⟨stale prose⟩."
2. **This is why the pin cannot be Derived.** Regenerating an expectation from the engine's own output makes
   the gate assert *that the engine agrees with itself* — it can never fail, and a GREEN silently degrading
   would regenerate its own pin and stay green. That is the same vacuity failure as a regeneration gate read
   from a stale cache (§3), one layer up.
3. **The prose `reason` disappears rather than being rendered.** The attribute vector *is* the reason.
   Plain-language text is generated from it at display time and stored nowhere. Human narrative, where
   genuinely useful, moves to a `note` field that **no gate and no report treats as truth**.
4. **`Green`/`Amber` retires as a stored and user-facing vocabulary**, surviving only as an internal
   shorthand if convenient. The frontend renders the attributes in plain language ("certified infinite";
   "loop closes, but its mana cost is not covered"), which is strictly more informative than the colour and
   needs no legend.

This deliberately readopts the one part of the withdrawn ADR 0004 that was right — *"keep the certainty tier,
relabeled for humans; retired only as a bare colour"* — in a form that also fixes the hand-maintenance the
withdrawn version left untouched, because what is pinned is now a computed vector rather than prose.

The general rule this generalizes: **prose that asserts a fact must be generated; prose that adds narrative
must be marked non-authoritative.** No prose is both.

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
| `reject` (terms deemed non-AST-derivable) | ~~Becomes asserted-absence golds, one per term.~~ **Superseded by issue #28 (2026-07-20): deleted outright, ruled NOT-A-PORT-CANDIDATE (35/35).** An absence gold is `no_arm` over a *projected port*; none of these terms is a port, a stem, or an AST discriminator, so the assertion is unstatable and would be vacuous. Not backlog either — they are absent from `projected(corpus)`, so §2's formula never yields them. A scope ruling about the taxonomy's vocabulary is architectural, hence ADR prose: **Appendix C**, plus the scaffold's own `$deleted` record. |
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
where a view need not be materialized, **don't materialize it** — the cheapest stale view is the one that
doesn't exist. Materialization is then re-adopted deliberately, for the one view whose *diff* is a product
in its own right (the rollup), and only with the refresh check that materialization has always required.

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

### Stage 1 — Derived artifacts become build outputs — **LANDED** (2026-07-20, issue #23)
Fix Flowthru cache keying to include code (hard prerequisite). Gitignore Derived artifacts **except the
rollup**; stand up a publishing step for the consumers that need the untracked ones (API seed, `atlas-diag`).
Wire the rollup's regeneration gate — the price of its committed exception (§3).

*Gate:* every Derived artifact is either untracked, or tracked **and** covered by a byte-identical
regeneration check run against a busted cache. A clean checkout can produce all of them. CORE ring green.

**As landed.** The gitignore half turned out to be nearly complete already — `Data/**` and `/dumps/`
cover the whole reporting surface — so the substance of #23 is the two halves that were *not* in place:
a single regeneration target, and consumers that stop pretending a missing artifact is normal.

| Piece | Shape |
|---|---|
| The target | `nx run flowthru:dumps` — `CorpusParse → FetchCombos → CardAtlas → ComboAnchors`, then publishes `_08_Reporting/dumps/*.json` into the gitignored repo-root `dumps/`. Publishes **leaves only**; no upstream artifact is rewritten (see below). |
| API seeder | `AtlasSeeder.RequireFile` **throws** with the runbook line, replacing a `FileReady` that logged "skipping" and continued. A half-seeded database is indistinguishable from a healthy one until a field comes back empty. |
| `atlas-diag` | exits 2 with the same message shape, naming the MAST-side targets for the datasets it reads. |
| The gate | `DerivedArtifactTrackingGateTests` (CORE ring) — the census's Derived set × git's index. Stateless membership, with liveness on the exception table in both directions, and a teeth check that force-adds a probe past `.gitignore`. |

**The committed exceptions, and why the list is longer than "the rollup".** §3 names the rollup, but
two artifacts already in the tree meet §3's own test (*is its diff worth a gate?*) more strongly than
the rollup does, because for them the committed copy **is** the gate's expectation:
`Tests/Interaction/Snapshots/` and `libs/magic-ast/schema/ast-schema.json`. Gitignoring either would
make its gate compare the engine to itself — the identical vacuity §5.2 rejects for the expected-tier
pin, one layer down. They are recorded as exceptions rather than quietly tolerated.

Two further entries are honest carve-outs rather than endorsements.
`libs/mast-interaction/known-coarse-projections.json` is in transit (Stage 4 deletes it; #28 was in
flight concurrently). `libs/mtg-rules/Data/_03_Primary/Datasets/type-ontology.json` is the one place
**the three-input derivation base does not close**: its input is the raw comprehensive-rules text,
which that project deliberately does not redistribute, so no clean checkout can rebuild it and there is
nothing for a gate to re-derive it against. The exception table makes that admission explicit — an
entry must carry *either* a gate that re-derives it *or* a stated reason nothing can, never neither.

**The `mtime:size` fingerprint (flowthru#148) shaped the target, not just its docs.** Because Flowthru
fingerprints file items on `{LastWriteTimeUtc.Ticks}:{Length}` rather than content, regenerating an
upstream artifact busts the cache and cascades a full re-run downstream even when the bytes are
identical (measured: 42 s for a byte-identical `card-inputs.json` with an unchanged SHA-256). The
target therefore never writes back into `_01_Raw`/`_02_Intermediate`/`_07_ModelOutput` — the publish
step copies leaves outward only. This is a documented constraint, not a blocker; the third open
question below ("does the API seed run the full pipeline or a scoped flow?") is answered by it: the
full graph, because cold is affordable (48.8 s, issue #22) and scoping it would mean touching upstream
files to force partial re-derivation.

### Stage 2 — Dissolve the scaffold
Execute §7's four-way split: delete the subsumed sections, promote P1–P6 to sweeps, delete `reject`
(adjudicated not-a-port-candidate — Appendix C, *not* converted to golds), move F1/F2/F3 into ADR 0003.

*Gate:* the sweeps run and pass (or produce a real, judged falsification); `topology-scaffold.json` is gone;
no `status` field survives anywhere it could be hand-typed.

### Stage 3 — Cross-track joins
Materialize §4's join set with gates. Start with quarantine → gold → tier: it has a known historical failure
(Suture Priest) that serves as the gate's acceptance test.

*Gate:* the quarantine join demonstrably fails on a reconstructed Suture-Priest-shaped input.

### Stage 4 — Retire the stored tier; dissolve the coarse-projection whitelist
Replace `combo-expected-tiers.json`'s stored tier + prose with a judge-set **attribute-vector expectation**
(Evidence); render plain-language text from the vector at display time and store none of it; retire
`Green`/`Amber` as a user-facing vocabulary. Convert `known-coarse-projections.json`'s entries to
asserted-absence golds (§1) and delete the file.

*Gate:* no gate or report consumes a hand-written field; the attribute expectation fails naming **which
attribute moved**; a judge-sampled subset confirms the rendered plain-language text carries the same meaning
the hand-written `reason` did (a correctness check, not a diff).

### Stage 5 — The invariant layer — **LANDED** (2026-07-20)
Land §6: sibling-path metamorphic properties, and modeled-dependency completeness derived from AST condition
nodes.

*Gate:* the composite-recursion asymmetry is caught when deliberately reintroduced; "which GREENs rest on
unmodeled conditions" is answerable by query.

**As landed:**

| §6 half | Artifact | Kind |
|---|---|---|
| Sibling-path consistency | `PortWalkSiblingPathConsistencyTest` — the six composite-capable `Effects()` paths (`optional.Inner`, `composite.Effects`, `conditional.Then`/`.Else`, `rollResultsTable.Rows`, `replacement.Replacement`) must project the same body emits as the top-level ability-effects path | **Gate** (CORE ring) |
| Modeled-dependency completeness | `ConditionConsumption` (the delta, derived by **ablation** — delete the condition node, re-project, compare) + `ConditionConsumptionTest` (machinery + Gravecrawler witness, hermetic) + the `OverApproximation` Flowthru flow → `over-approximation-report.json` | **Gate** + **report** |

The asymmetry §6 describes was itself already repaired in `9e319ea7`; the metamorphic test is what makes the
repair non-regressible and forces any FUTURE composite-capable path to prove itself on arrival. Falsified at
authoring time by reverting `Effects(e["Replacement"], …)` to a direct `EmitPort` call — 4 red, and the
`single-createToken` case correctly stayed green (a non-composite body is not collapsed).

"Consumed" is **derived, not declared**: a condition node is consumed iff ablating it moves the projection's
fingerprint. So there is no register to maintain, and a slice that starts reading a condition removes it from
the report with no edit. First corpus run (6,921 parse-ready union cards): **612 AST condition nodes, 335
consumed, 277 dropped across 250 cards, underwriting 329 GREEN ports** — led by `other` (104), `count` (59,
Gravecrawler's clause among them) and `keywordCostPaid` (47).

This is adjacent to, and must not be conflated with, `known-coarse-projections.json`: that whitelist names
**discriminators projected coarsely** (a resolution loss — hand-authored, gate-enforced, Stage 4 dissolves
it); this report enumerates **condition nodes dropped entirely** (a guard loss — fully derived, diagnostic,
nothing to dissolve).

#### Stage 5b — widened attributes: the third class — **LANDED** (2026-07-20)

Dropped conditions are not the only over-approximation. A **widened attribute** is a narrowing FACET the AST
carries that the projection did not put on the port — the port is rightly projected, but it names more of the
game than the card does. `AttributeConsumption` + `AttributeConsumptionTest` + the `WidenedAttributes` flow →
`widened-attribute-report.json`. This is the gap left by removing ADR-0003 §7's `asserted`/`derived` marker
(`5e1b2f0e`): detection replaces annotation.

**Ablation transfers directly**, and that was the design question. Deleting an attribute *is* widening it —
an absent facet is the broadest value, since `PortLabel`'s facet join simply drops the segment — so "the
projection ignores this facet" and "the AST without it projects identically" are the same statement. The
rejected alternative, comparing AST filter facets against projected label facets, needs a hand-maintained
AST-property → label-segment correspondence table (the exact drift surface this ADR exists to remove) and is
blind to facets riding the port `Subject` rather than the label.

Two boundaries make the three classes non-launderable:

| | Unit | Derivation | Loss |
|---|---|---|---|
| `known-coarse-projections.json` | a discriminator NAME | hand-authored, gate-enforced | **resolution** |
| over-approximation report | a condition NODE instance | ablation, diagnostic | **guard** |
| widened-attribute report | an attribute SITE | ablation, diagnostic | **scope** |

The node/attribute split is *structural*, not agreed: an attribute site is a subtree containing no
polymorphic node (registry read in-process from `SchemaExport.Build`), and a `Condition` **is** a node — so
neither report can ever contain the other's rows. Reading registered discriminator *values*, not just key
names, is load-bearing: `Kind` is a discriminator key on `Ability`, but `{"Kind":"You"}` is an
`ObjectReference` facet.

A second derived filter keeps the report a burn-down list rather than a field dump. A facet name is
**narrowing** iff ablating it *somewhere* shed a label facet (`replace:token-creation` is a proper
facet-prefix of `replace:token-creation:controlled`). Filtering on mere readership instead was measured at
**58,306** rows dominated by `SourceSpan`/`OracleLineIndex` provenance; the narrowing filter yields **6,435**
rows over 3,714 cards underwriting **7,110 GREEN** ports — led by `evasion.CardTypes` (845), `mana.Kind`
(822), `drawCards.Player` (623), `gainAbility.Target` (478), `modifyPT.Target` (415).

**First finding, closed in the same commit.** The report flagged **26 replacement-event `Controller` facets
across 25 cards, every one GREEN**, spanning six event kinds — `lifeChange` (12: Rhox Faithmender, Boon
Reflection, Angel of Vitality…), `tokenCreation` (8: Doubling Season, Parallel Lives, Anointed Procession,
Mondrak, Elspeth Storm Slayer, Adrix and Nev, Exalted Sunborn, Peregrin Took), `counterPlacement` (3),
`mill`, `spellCopy`, `diceRoll`. CR 614.1 replaces a *specific* event, so a scoped event must project a
scoped intercept; unscoped, Bruvac the Grandiloquent was modelled as doubling the controller's **own** mill,
and Vorinclex's two opposed clauses ("you … twice", "an opponent … half") collapsed onto one identical
`replace:counterplacement` port. `PortGraph`'s replacement branch now reads the event controller onto both
the label and the `Subject`; the 26 rows cleared to **0** with no edit to the report — which is the property
being claimed.

**Not found, and correctly so:** Chatterfang, Squirrel General. It prints the same "under your control" as
its four siblings, but `TokenAugmentationReplacementRule` alone omits `Controller` from the `tokenCreation`
event it builds. A facet the AST never states cannot be a facet the projection dropped — that is a **parse**
gap, not a widening, and ablation is blind to it by construction. The one-line rule fix re-points
`Fixtures/HandParsedCards/MH2/Chatterfang.json`, so it is orchestrator back-prop rather than worker work; the
projection already carries the facet, so the port is born correct the moment the rule states it.

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
  judgment to be re-derived rather than silently outliving its premise. **Triage is a hard build failure,
  judge-resolved:** either the new arm is correct (amend or delete the gold, judge-gated) or the arm is wrong
  (fix it). It is never resolved by weakening the assertion, and never deferred to a report — a soft signal
  here would reproduce exactly the "quiet artifact nobody reconciles" failure this ADR exists to remove.
- **Some reports get worse before better.** Generated prose is blunter than a rigorous worker's hand-written
  `reason`. Correct trade: an honest blunt sentence beats an eloquent one that stopped being true.
- **This does not reduce the golds' load-bearing role — it makes it exact.** The system leans on hand
  evidence exactly as much as today; the difference is that the amount becomes *measurable* (§2) and nothing
  else can masquerade as it.

## Open questions

*(The three questions carried by the 2026-07-19 draft were resolved on 2026-07-20 — see Provenance. What
remains are the implementation details those decisions opened.)*

- **Which attributes constitute the pinned vector (§5.1)?** `Firable` / `CoCostsSatisfied` / `Balanced` /
  `LifeBalanced` / `Productive` / worst-hop `Reliability` is the obvious set, but whether `Overlap` and
  `LimitingReason` are pinned or merely reported is a Stage-4 call — pin too much and every engine refinement
  churns 33 expectations; pin too little and the gate goes slack.
- **Plain-language wording for the retired colours (§5.4).** No concrete copy exists yet. Needs a pass that a
  non-player can read without a legend, which is the bar the colour failed.
- ~~**Does the API seed run the full pipeline or a scoped flow?**~~ **Closed (2026-07-20, issue #23):
  the full graph.** Cold is 48.8 s and warm is 0.14 s (issue #22), so the ergonomic saving from scoping
  is negligible — and scoping is actively harmful under flowthru#148, since forcing a partial
  re-derivation means touching an upstream file, which busts its `mtime:size` fingerprint and re-runs
  everything below it anyway. `nx run flowthru:dumps` runs all four flows and publishes leaves only.

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
- **Refinement** (2026-07-20, same session): the derivation base stated as exactly three inputs (external
  source data, Evidence fixtures, code) — everything else is reproducible by running the Flowthru pipeline;
  and the gitignore default **rolled back for the rollup**, whose inter-run diff is genuinely valuable drift
  information. That exception generalized into §3's decision procedure (*is its diff worth a gate?*) and
  restored the byte-identity check in narrow scope, as the price of committing.
- **Open questions closed** (2026-07-20), owner decisions: **(a)** Derived artifacts are generated on demand,
  and every derivation/census/join/report is a **Flowthru flow** in `_08_Reporting` — Flowthru is the
  analytics backbone, and NUnit is reserved for gates; **(b)** the expected-tier pin was **misclassified as
  Derived** in the 2026-07-19 draft — regenerating an expectation from the engine makes the gate vacuous, so
  it is Evidence, and the deeper fix is retiring the stored tier in favour of its computed attribute vector
  (§5), since `Green`/`Amber` is an opaque rollup — especially in the frontend; **(c)** an absence assertion
  firing is a **hard build failure, judge-resolved**, never a soft report entry.

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

The pattern §1 replaces `known-coarse-projections.json` with.

> **Correction (2026-07-20).** This appendix originally worked `emit:attach` as the example. **That example was
> wrong, and its wrongness is instructive enough to keep.** The claim was that attaching "emits no event a
> trigger subscribes to" (CR 301.5/701.3). It does: **13 corpus cards carry `becomes attached` triggers**,
> several as explicit triggered abilities — *"Whenever this Equipment becomes attached to a creature, tap that
> creature"* (Enormous Energy Blade), *"Whenever an Aura you control becomes attached to a creature you
> control, create a 1/1 …"* (Siona, Captain of the Pyleas). Per CR 603.2 those are ordinary event triggers, so
> `emit:attach` has a printed counterparty and is **backlog, not a decision** — blocked on a missing
> `BecomesAttached` trigger event, exactly as `known-coarse-projections.json`'s own 2026-07-18 investigation
> note says. (That note additionally miscites CR 701.3d, which defines becoming *un*attached.)
>
> **The lesson, which is the point of §1.** An asserted absence that is false is *worse* than the whitelist
> prose it replaces: prose is inert, but a false `no_arm` gold actively subtracts a real port from the backlog
> formula in §2 and gates the build in favour of the error. The falsification prompt now in the
> `interaction-judge` checklist — *"what change would make this absence wrong?"* — exists to catch precisely
> this, and it caught it. **Absence golds are only as good as the adversarial pass over them.**
>
> The worked example below is replaced by the shipped gold `rat-colony-deck-construction-terminal`
> (`emit:anynumberindeck`), which is terminal *by the rules*: a deck-construction static ability (CR 604.1)
> functions only before the game begins (CR 113.6n), raises no event, and grants no action, so no consume can
> observe it. Note the near-miss the judge pinned there: **companion** (CR 702.139) also functions before the
> game begins, yet carries an in-game special action (CR 116.2g) — so "functions before the game begins" is
> *not* itself sufficient for terminality.

The schema below remains the correct **shape**; read it for the mechanics, not for the ruling.

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
    "note": "ILLUSTRATIVE SHAPE ONLY — this card's ruling is FALSE; see the correction above. Retained because the field layout, the assertion pair, and the structured:false discipline are all still exactly right. For a TRUE worked example read Fixtures/Interactions/golds/rat-colony-deck-construction-terminal.json."
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

## Appendix C — the `reject` / `known-coarse-projections` adjudication (issue #28, 2026-07-20)

### The prior this appendix applies

> **An unserved projection is backlog until proven otherwise.** An asserted-absence gold is the rare
> exception. The bar for a decision is not "no consume exists today" — that is the *definition* of backlog —
> but a **CR argument that no consume could ever exist**.

The asymmetry that makes the prior correct: a false absence gold is **worse** than the whitelist prose it
replaces. Prose is inert; a false `no_arm` gold subtracts a real port from §2's backlog formula *and* gates
the build in favour of the error. Over-reporting the backlog costs nothing. Under-reporting hides work.

Forced by three findings: Appendix B's own worked example (`emit:attach`) was **wrong**; the hit rate on
absence claims was 1-wrong-of-2; and a full taxonomy sweep found exactly **one** genuinely terminal port
(`emit:anynumberindeck`).

### Outcome

| Population | Terms | Ruling |
|---|---|---|
| `topology-scaffold.json` `reject` | 35 (11 archetypes, 17 judgments, 7 social) | **not-a-port-candidate ×35** — deleted, recorded in the scaffold's `$deleted` (5) |
| `known-coarse-projections.json` | 288 (220 `effectType`, 16 `costType`, 46 `triggerEvent`, 6 `restriction`) | **backlog ×283**, **decision ×1** (`anyNumberInDeck`, already golded), **not-a-port-candidate ×4** (`effectType.unparsed`, `effectType.unstructured`, `triggerEvent.Other`, `restriction.Other`) |

**New absence golds authored: zero.** That is the expected outcome, not a shortfall.

### `reject` — why 35 category errors, not 35 golds

`no_arm[P]` asserts that `SelectArm` returns null for a **port the engine projects**. `aggro`, `value` and
`netdeck` are not ports, stems, `EffectType`s, `CostType`s, `TriggerEvent`s or restrictions; they are labels
players apply to decks, lines of play and each other. There is nothing for the matcher to be null over, so
the assertion is unstatable — and asserting the absence of a port that was never projected is vacuous in
exactly the way Appendix B's "the trap this design avoids" describes. Nor are they backlog: §2's formula
starts from `projected(corpus)`, and no card text yields them, so no parser or engine work would ever
surface them. The scaffold's original `$note` was already correct — *a rejected archetype's mechanisms map;
the label does not* — and needed conversion into nothing.

### `known-coarse-projections` — the three tempting terminality candidates, all falsified against the corpus

Every entry was tested against step 1 of the prior (*does any printed card demand a counterparty?*). The
only entries where a terminality argument was even plausible are the ones that function **outside the
game** — the class `anyNumberInDeck` belongs to. All were checked against
`Data/_01_Raw/Datasets/External/oracle-cards.json` (38,279 oracle rows, 2026-07-18 snapshot):

| Entry | Counterparty found | Ruling |
|---|---|---|
| `attach` | **13** cards print `becomes attached` — **10** as triggered abilities (*"Whenever this Equipment becomes attached to a creature, tap that creature"* — Enormous Energy Blade; *"Whenever an Aura you control becomes attached to a creature you control, create a 1/1 white Human Soldier creature token"* — Siona, Captain of the Pyleas; plus Assimilation Aegis, Eriette the Beguiler, Animate Spell, Blade of Shared Souls, Bramble Elemental, Brood Keeper, Inchblade Companion, Killer Cosplay), **3** as *"**As** this … becomes attached"* choices (Psychic Paper, Sanctuary Blade, Paleontologist's Pick-Axe), which CR 603.1 excludes from triggered abilities but which need the same undiscriminated event. CR **603.2e** names the event outright: *"Some trigger events use the word 'becomes' (for example, 'becomes attached' or 'becomes blocked')…"* — so Appendix B's premise is not unsupported, it is **contradicted**. | **backlog** — blocked on a missing `BecomesAttached` `TriggerEvent` (the enum has `BecomesUnattached` only, itself coarse) |
| `commanderDesignation` | **39** cards say *"… can be your commander"*, and the designation is read back in game — **9** cards carry a *"Whenever your commander …"* trigger, **134** reference *"your commander"* at all (*"Whenever your commander enters, you become the monarch"* — Nakia, Wakandan Operative; *"Whenever your commander deals combat damage to a player …"* — Jocasta, Automaton Avenger). CR 903.8 (commander tax) and 903.9a (command-zone replacement) consume it too. | **backlog** |
| `openingHand` | **23** cards say *"begin the game with"* and **9** *"reveal this card from your opening hand"* — the Chancellor cycle hangs an explicit delayed in-game trigger off it (*"… at the beginning of the first upkeep …"*), and Leylines start on the battlefield with ordinary in-game abilities. | **backlog** |
| `partner` | **143** rows carry `Partner` as a Scryfall keyword (**152** by case-insensitive oracle-text match); *"Partner with …"* carries an in-game ETB tutor (Lore Weaver / Ley Weaver). | **backlog** |
| `wish` | **104** rows say *"outside the game"*, every one resolving into an in-game action (Spike, Tournament Grinder). | **backlog** |

The contrast that makes `anyNumberInDeck` the sole survivor: of the **10** deck-construction cards (Rat
Colony, Relentless Rats, Shadowborn Apostle, Persistent Petitioners, Dragon's Approach, Hare Apparent, Slime
Against Humanity, Templar Knight, Tempest Hawk, Cid Timeless Artificer), **not one** reads another card's
deck-construction ability in game — the in-game text ("count Rats", "sacrifice six creatures named
Shadowborn Apostle") is always a *separate ability with its own ports*. (Judge correction: an earlier
`any number of cards named` regex returned 13 by also catching three *search* effects — Battalion Foot
Soldier, Gathering Throng, Legion Conquistador — which carry no deck-construction ability. The same 13, plus
Seven Dwarves and Nazgûl — CR 100.2a-style *raised* caps, a different rule — propagated into the
`rat-colony-deck-construction-terminal` gold's `source.note`; that gold is immutable to workers, so the
correction is flagged for orchestrator back-prop. The ruling is unaffected.) CR 113.6n gives the
deck-construction static no in-game window in which a consume could observe it. (And per the judge's
near-miss on that gold, "functions before the game begins" is **not** sufficient — companion, CR 702.139,
also does, yet carries the CR 116.2g special action.)

Every remaining entry fails step 2 by inspection: prohibition statics (`cantGainLife`, `cantDrawCards`,
`cantPreventDamage`, …), permission grants (`grantAlternativeCost`, `canAttackIgnoringDefender`, …), keyword
actions (`amass`, `connive`, `explore`, `forage`, …), choice declarations (`chooseColor`, `choosePlayer`, …)
and the 46 unread trigger events all describe things that demonstrably happen *during* a game and change its
outcome. "No flow rule reads it **yet**" — which is what ~150 of the reasons literally say — is the textbook
statement of backlog.

**Four** entries are the genuine third category — recognition-failure escape hatches, not Magic concepts:
`effectType.unparsed` and `effectType.unstructured` (`IUnparsed` residue nodes carrying `RawText`, the
parser's record of its own failure), plus `triggerEvent.Other` (*"Unrecognized trigger event"*) and
`restriction.Other` (*"Other restriction captured as raw text"*), which the interaction-judge's falsification
pass correctly pulled out of the backlog under this same criterion. No card can print "unparsed" or "other",
so no consume could key on the label itself and no CR argument applies either way. They belong on the
**parse** ledger (the fidelity ladder / L2 coverage), never on the interaction backlog — #32 must exclude
them or the backlog double-counts parse debt as interaction debt.

### Why the file is still on disk

Its **names** are already derivable — `PortWalkExhaustivenessTests.Regenerate_coarse_projection_whitelist`
computes exactly `all discriminators − PortWalkProjection`, which is §2's backlog before the
asserted-unarmable subtrahend. What is *not* derivable is (a) the loud gate that a new discriminator is
neither projected nor consciously accepted, and (b) the handful of reasons that are genuine investigation
notes rather than the boilerplate default. So deletion is gated on **issue #32**:

1. #32 emits the derived backlog as a `_08_Reporting` Flowthru output: `projected − served − asserted-unarmable`, keyed by discriminator and dimension.
2. `PortWalkExhaustivenessTests` re-points at that output — the invariant becomes *every unprojected discriminator appears in the derived backlog, and every asserted-unarmable one has a gold* — preserving the loud signal without a hand-maintained name list.
3. The ~15 substantive investigation notes (`attach`, `exert`, `forage`, `TapsForMana`, `ExcessNoncombatDamageDealt`, …) move to wherever #32 keeps per-item annotation, or are dropped as re-derivable.
4. Then, and only then, the file is deleted.

Deleting it before #32 exists would trade a hand-maintained list for **no** statement at all, and would take
the exhaustiveness gate red or vacuous. Issue #28 therefore leaves it in place deliberately, having
adjudicated its contents.
