# 06 — Track A: Operator axes (IsSelf + Resource.Subject) — scope & plan

**Status:** scoping/measurement pass (2026-06-15). No engine/operator/parser/AST/gold changes — this
doc + `[Ignore]` target tests only. Branch `alignment/06a-operator-scope`.

**TL;DR for the planner:** the spec's headline — "IsSelf + Resource.Subject handling is incomplete,
blocking ~1,400 self-triggered cards" — is **substantially superseded**. The `IsSelf` axis has
*landed* end-to-end (parser → operator → label → engine) across the round-2/3/4 refinement loops; it
is the thing that *correctly* floored those ~1,344 self-death loops to Amber, not a gap blocking them.
The remaining IsSelf surface is two small, named follow-ons. The genuinely-open half of the axis pair
is **`Resource.Subject` (the positional-counter `@bearer` facet)**, which has **not** landed — but the
measured cohort it blocks is **near zero in today's surfaced corpus and zero in the 33-combo bench**.
The big recall gap (25 bench misses) is **structurally absent flow arms** (life, cast/recursion,
copy/blink, mana-untap), *not* operator-axis tiering — explicitly out of scope per the spec.

---

## 1. Current state — what the two axes do today (exact)

### 1a. `IsSelf` (the scope axis: `self ⊆ controlled ⊆ any`)

`ObjectFilter.IsSelf` is the dual of `ExcludeSelf`: "this creature" / a self-name / a keyword self-event
binds to it. It is read at four layers, all landed:

| Layer | Use | Where |
|---|---|---|
| **Parser** | "this [type]" / self-by-name / 6 keyword self-death paths → `IsSelf` | `TriggeredRuleHelpers.ParseObjectFilter` (`ce6534be`, `ad9c8412`); self-by-name (`efbaa75f`); sac-self (`4d3235b6`) |
| **Operator** | `Subsumes`: a self-only sup is **not** subsumed by a non-self sub → `No` (a cross-card sac→self-death bridge falls to Amber) | `ObjectFilterRelations` (gated `IsSelf` alongside the `ExcludeSelf` exemption) |
| **Label** | `PortLabel.Scope`: `IsSelf==true → ":self"` (narrowest scope) | `PortLabel.cs:108` |
| **Engine** | 4 IsSelf reads, all *prunes/floors*, not gaps: (1) `TokenSatisfiesAtCreation` refuses to refuel a `:self` sac from a created token (`PortGraphEngine.cs:220`); (2) `IsOneShotSelfRemoval` prunes a `ltb:…:to-graveyard:self` loop (via `IsSelfLeavesToGraveyard`, :824); (3) `BridgeFedByIncompatibleToken` excludes a `:self` death (it's the §8-B domain, :754); (4) `CounterGateUnsatisfiable` excludes a one-time self-ETB counter (:810) | `PortGraphEngine.cs` |

**Corpus evidence it's live, not stubbed:** 196 golds carry `"IsSelf": true` (201 occurrences); the
S3b corpus run projects `ltb:creature:to-graveyard:self` on **1,344** self-death triggers, and **every**
cross-card sac→self-death bridge (1,342 edges) is Amber, 0 false-GREEN — a net 598 edges flipped
GREEN→Amber (ADR-0002 §10). The named cases (Doomed Dissenter, Brindle Shoat, Triplicate Titan,
Phyrexian Triniform, Elenda) each carry `:self` with all inbound bridges Amber. This **is** the
~1,400-card cohort the spec named — and it is **already tiered correctly**.

### 1b. `Resource.Subject` (the resource-bearer `@<bearer>` facet)

ADR-0001 declares `Resource.Subject` on the resource axis; ADR-0002 §3 promises positional resources
carry their bearer as a **parameter** facet — `counter:plus-one-plus-one@<bearer-filter>` — to fix the
"counter on Ballista ≡ counter anywhere" collision (`Resource.Subject = null`, Interaction.cs).

**This has NOT landed.** There is **no** `Subject` field on `Interaction.cs`'s resource types, and no
`@bearer` parameter anywhere in the projection. Counters project the coarse
`emit:counter:<type>:<scope>` where scope is just `self`/`target` (`PortGraph.cs:374`) — a control-axis
token, **not** the bearer's `ObjectFilter`. So a counter emit/consume cannot tell *which permanent* the
counter rides, and a counter-flow edge cannot be derived at all (counters are not yet a flow resource —
the §8 counter handling is the `RequiresCounter` gate-prune, not a flow). This is the genuinely-open
operator axis.

### 1c. The CertaintyTier computation (`PortGraphEngine.cs`)

Two tiers stack. **Per-edge** (`PortEdge.Tier`, :28): CardDefined → Green; else Disjoint → Red;
Overlaps ∧ Reliability=Yes → Green; otherwise Amber. **Per-cycle** (`PortCycle.Tier`, :89):
`max(worst hop, Amber-floor)` where the floor fires unless `Firable ∧ CoCostsSatisfied ∧ Balanced ∧
Productive`. IsSelf influences the tier only **indirectly**: via the operator's `Subsumes` (a self sup
demotes Reliability) and via the four engine prunes/floors above. `Resource.Subject`/`@bearer` does not
participate in tiering at all today (no counter-flow edge exists to tier).

---

## 2. The remaining gap (precise)

After the round-2/3/4 work, what is *actually* still incomplete in these two axes:

1. **`Resource.Subject` / `@bearer` — unimplemented (the real open axis).** No bearer facet → no
   positional-counter resource → no counter-flow edge and a latent `counter` collision (ADR-0002 §3,
   the second of the "two upstream prerequisites" in §10/Consequences). This is the design's named-but-
   unbuilt piece.

2. **IsSelf follow-on (1) — keyword self-triggers on *non-death* events** still bypass self-binding
   (Melee→attacks, Evoke→enters). ADR-0002 §10 *Residual*. Not a death-bridge, so no current false-GREEN;
   it would matter once attacks/ETB self-loops are modeled.

3. **IsSelf follow-on (2) — return-label normalisation invariant** for the Persist/Undying carve-out
   (the carve-out keys the exact `emit:returntobattlefield` label). ADR-0002 §8-B *Follow-on (2)*. No
   `:self` cycle surfaces a self-return today → not a present miss.

Everything else the spec listed under IsSelf ("self-referencing trigger must not consume its own
emission", "subject filters must participate in overlap tiering not just labels") is **done**: the
self-sac/self-death one-shot prunes implement the first; `Subsumes`/`Intersects` reading
`PortNode.Subject` (incl. IsSelf) implements the second.

---

## 3. The measured blocked cohort (verified, not the spec's estimate)

| Cohort | Spec estimate | **Verified today** | Blocking axis |
|---|---|---|---|
| Self-triggered cards "stuck at Amber" on IsSelf | ~1,400 | **0 stuck** — the 1,344 self-death labels are *correctly* Amber (sound), not a tierable gap; IsSelf landed | n/a (resolved) |
| Bench combos floored by IsSelf | — | **0 / 33** | n/a |
| Bench combos floored by `Resource.Subject`/`@bearer` | — | **0 / 33** | n/a |
| Bench **Amber** (8) — tier-floored | — | **8, floored on the Squirrel⊄creature subtype straddle** (`Subsumes`→Unknown→"Types"), NOT IsSelf/bearer | type-operator Reliability |
| Bench **Missed** (25) — no cycle | — | **25 structurally absent**: 7 aristocrat-recursion (Gravecrawler `cast` / drain), 6 Kiki copy-untap, 4 life-drain mirror (`life` flow), 4 spell-copy (`emit:cast`), 2 mana-untap blink, 1 blink-copy, 1 spell-copy | missing flow arms (out of scope) |

**Conclusion: the IsSelf/Resource.Subject cohort that is *blocked and fixable by completing these axes*
is effectively empty in the surfaced corpus and bench.** The 8 Amber are a **type-operator** matter
(Squirrel⊄creature `Subsumes` straddle — initiative 06's quantity/operator work could revisit it, but
it is not IsSelf and not the resource-bearer); the 25 Missed are **structural** (initiative 04/05
flow-arm work). The headline ~1,400 is *already certainty-tiered correctly by IsSelf*.

### Named blocked exemplars (from round-2 + bench)
- **Self-sac one-shot** (round-2 D#61/D#62/D#111): Chromatic Star, Dromar's Attendant, Barrels of
  Blasting Jelly — **resolved** by `4d3235b6` (self-bind + `TokenSatisfiesAtCreation` refusal). Pin them
  to confirm they stay pruned/Amber (regression guard, not new work).
- **Self-death loops**: Doomed Dissenter, Brindle Shoat, Elenda — **resolved**; pin Amber.
- **The only remaining axis exemplar (Resource.Subject):** a `+1/+1`-counter modular/proliferate loop
  (e.g. Arcbound Worker → Arcbound Ravager, or a Walking Ballista counter-doubler) needs the `@bearer`
  facet to derive a counter-flow edge — there is **no such loop in the 33-combo bench**, so this
  exemplar is corpus-sourced from the counter cards in the golds, pinned `[Ignore]` as the definition of
  "Resource.Subject done."

---

## 4. Is a new AST node genuinely needed? (corpus evidence)

**No new AST node is needed for IsSelf** — `ObjectFilter.IsSelf` already exists and is populated in 196
golds. **No new AST node is needed for `Resource.Subject` either**: the bearer is an *existing*
`ObjectFilter` (the counter's host), so the change is a **projection/engine** change (carry the counter
emit/consume's bearer `ObjectFilter` onto the port as a `@bearer` parameter facet + an `Intersects`-tiered
counter-flow edge), reusing the same operator that already tiers `PortNode.Subject`. The work is in
`Interaction.cs` (a `Subject` on the resource record / a bearer parameter on the counter port) and
`PortGraph.cs`/`PortGraphEngine.cs` (project it, derive the edge), **not** in `magic-ast`'s node schema.
(Track B's quantity-expression node — `Literal/Variable/CountOf/Product` — is a separate, genuinely-new
AST node, but that is the spec's other track, out of this scope.)

---

## 5. Implementation plan (focused) — for the *next* (build) initiative, not this pass

Ordered by value, smallest-first:

1. **Resource.Subject / `@bearer` (the real axis work).** (a) Add the bearer `ObjectFilter` to the
   counter emit/consume projection in `PortGraph.cs` as a `@bearer` parameter facet (ADR-0002 §3c —
   parameter, not colon, so it never pollutes prefix-match). (b) Add a counter-flow edge in
   `PortGraphEngine.Materialize` (`emit:counter:<kind>@<bearer> → consume on a +N/+N gate / proliferate`)
   tiered by `ObjectFilterRelations.Intersects` on the bearer. (c) Keep the §8 `RequiresCounter` prune.
   Scope: only the counter forms in the golds.
2. **IsSelf follow-on (1):** extend keyword self-binding to non-death events (attacks/enters) — one
   parser path + golds; only matters once those flow arms exist.
3. **IsSelf follow-on (2):** normalise every Persist/Undying return to `emit:returntobattlefield` so the
   §8-B carve-out's key is invariant.

Each lands behind the `interaction-judge` GREEN-sample gate (§6).

---

## 6. False-positive risk — newly-GREEN edges are the danger

**The risk surface for this track is tiny because almost nothing newly turns GREEN.** IsSelf is already
landed and *demotes* (GREEN→Amber); a demotion can never introduce a false-positive (it is a strict
subset of the prior GREEN set — the same argument the phase-10 conjunction tightening relied on,
ADR-0002 §8). So **steps 2–3 carry no false-GREEN risk by construction.**

The *only* newly-GREEN risk is **step 1 (Resource.Subject)**: a derived counter-flow edge that wrongly
certifies a counter loop (e.g. a proliferate "engine" that is actually finite, or a bearer-mismatch the
coarse projection lets through). This is the one place to size the judge.

- **interaction-judge sample size:** the spec's bar is *≥20 newly-GREEN edges or all, whichever is
  smaller*, with **zero confirmed false positives**. Today's surfaced corpus has **0** counter-flow
  GREENs (the edge type doesn't exist yet); once step 1 lands, sample **all** newly-GREEN counter edges
  if <20, else 20 adversarially toward bearer-mismatch boundaries (a counter doubler whose bearer filter
  is broader than the gate's). For steps 2–3 (demotions only): a confirmatory sample of **all** affected
  edges (likely <20) to verify no real combo was wrongly demoted (false-Amber), the safe direction.
- **Initiative-03 sentinel snapshots — coverage:** the sentinel snapshots
  (`PortWalkSentinelSnapshotTest.cs` + `Snapshots/`) pin **label projections** of representative cards,
  not engine *tiers*. They **do** cover the IsSelf label projection (the `:self` scope facet appears in
  the snapshotted self-death/self-sac cards), so steps 2–3 would surface as deliberate snapshot diffs
  (label changes for the newly-self-bound attacks/enters cards). They **do NOT** cover the
  Resource.Subject counter-flow edges: a `@bearer` facet is a *new* projection not in any current
  snapshot, and the snapshots are label-level (not edge-level / tier-level), so they cannot guard the
  newly-GREEN counter edges. **The sampled interaction-judge is the ONLY guard on step 1's GREENs** —
  hence sizing it (all-if-<20) is load-bearing. Recommend adding counter-bearer cards to the sentinel
  set when step 1 lands so the label diff is at least pinned, even though the tier guard remains the
  judge.

---

## 7. Plan shape (summary)

A **measurement pass that reframes the spec**: the IsSelf half is *done* (and is precisely what tiers
the ~1,400-card self cohort correctly — not a blocker); the open axis is `Resource.Subject`/`@bearer`,
whose blocked cohort is ~0 in today's bench/surfaced corpus (so it is a *correctness/precision* item,
not a recall unblock). The bench's measured gap (8 Amber + 25 Missed) is **not** an
IsSelf/Resource.Subject gap — 8 are the type-operator Squirrel straddle, 25 are structurally-absent flow
arms. The `[Ignore]` target tests below pin: (a) the IsSelf exemplars stay correctly pruned/Amber
(regression definition of "still done"), and (b) the one Resource.Subject exemplar that defines "axis
complete" (a counter-flow loop that must tier on the bearer). They skip, not fail — the suite stays
green at 3310 passed.
