# Copy-token inheritance — SCOPE / design

**Status:** DESIGN ONLY. No engine/parser/schema code lands with this doc. Every decision below is
marked **PROPOSED — pending ratification**. The acceptance tests are `[Ignore]`-pinned (they skip,
they don't run) and document "done."

**Companion reading:** `adding-a-flow-arm.md` (the projection↔connection split this feature must
respect), ADR-0002 (single-role port model, §5 card-defined vs §7 operator tiering, §8 firability),
`docs/scratch/alignment-session/05_destring-ast-leaves.md` (the relationship is pinned in §7).

---

## 1. The gap (confirmed against the code)

A "create a token that's a copy of [a creature]" effect makes a token that carries the **copied**
card's abilities (CR 707.2 — the copy takes the copiable values of the original, including its
abilities). Kiki-Jiki taps to copy a partner; the copy inherits the partner's untap/blink ability,
which untaps Kiki, which taps again — an infinite loop. The interaction engine cannot see the
inherited abilities, so the loop never closes.

Each diagnosed fact, verified:

| Claim | Verified | Evidence |
|---|---|---|
| `CopyEffect.Target` is an `ObjectReference` | YES | `libs/magic-ast/AST/Effects/TokenCopy/CopyEffect.cs:16` |
| Kiki projects a coarse `emit:copy` | YES | `PortGraph.cs:411` fallback `emit:{other}`; `copy` is in `known-coarse-projections.json` (effectType.copy, "no flow rule consumes it yet") |
| `PredefinedTokens` handles only fixed-spec tokens, NOT copies | YES | `PredefinedTokens.cs` Registry = Treasure/Gold/Powerstone/Clue/Food/Blood/Map; the docstring explicitly excludes creature tokens; `ResolvePredefinedTokens` keys off a fixed subtype string |
| Nothing grafts the copied card's port graph onto the copy emit | YES | `PortWalk.Project` walks only the card's own `Oracle.Abilities`; `Materialize` only joins emit/consume by label — no card-to-card ability inheritance anywhere |
| `TapGatesRenewed` requires `emit:untap:self` ON the tap-gated card | YES | `PortGraphEngine.cs:734` — the renewal predicate hard-matches `e.To.Label == "emit:untap:self"` and `e.From.Card == card` (the tap-gated card itself) |

The `CopyEffect` is faithfully **parsed** today (Kiki's gold carries the target filter
`creature / ExcludedSupertypes:[Legendary] / Controller:You` and the `abilityAdder: haste`
modification — `KikiJikiMirrorBreaker.json:54-71`). The gap is **purely in the interaction layer**:
the copy emit has no inherited ports, and the renewal predicate is too narrow.

---

## 2. CRITICAL scoping refinement — copy-inheritance vs blink/flicker

The prior analysis conflated two distinct mechanisms. They are **separate flow arms**. Only the
first is in scope here.

### Copy-inheritance (THIS feature)
A copier (Kiki-Jiki, Helm of the Host) creates a **token that is a copy** of a partner. The token is
a *new object* (CR 707.2) carrying the partner's abilities. The loop closes because the **copy's
inherited ability** acts back on the copier (untaps Kiki) or is itself an engine-of-value (a copy of
a value ETB). The original partner is untouched.

### Pure blink/flicker (SEPARATE arm — OUT OF SCOPE)
A flicker spell/ability (Ghostly Flicker, Displacer Kitten) **exiles the original card and returns
it**. The *same object* leaves and re-enters: it re-enters untapped (refunding a tap) and re-triggers
its own ETB. There is no copy and no inheritance. This is its own arm — an
`exile-and-return` / `returnToBattlefield(self)` → `etb` + `untap` edge — and is explicitly **not**
built by this feature.

> Felidar Guardian and Restoration Angel are **blink cards, not copy cards** (their golds carry
> `exile` + `returnToBattlefield`, not `copy` — `FelidarGuardian.json:33-66`,
> `RestorationAngel.json:65-100`). They appear in this feature's combo table ONLY as **copy targets**
> (Kiki copies them; the copy's blink ETB untaps Kiki). When the same two cards are paired *without a
> copier* (Felidar + Restoration Angel, combo 1090-2781) the mechanism is pure blink and is out of
> scope.

### Per-combo mechanism table (from `tools/bench/MagicAtlas.Bench/bench-report.json`, Missed only)

| Combo (id) | Cards | Mechanism | Blocked on |
|---|---|---|---|
| 618-4404 | Kiki-Jiki + Corridor Monitor | Kiki copies Corridor Monitor; the copy's ETB `untap target` aims at Kiki → renews Kiki's tap | **copy-inheritance** (+ TapGatesRenewed generalization for target-untap) |
| 618-2781 | Kiki-Jiki + Felidar Guardian | Kiki copies Felidar; the copy's ETB blinks Kiki (exile+return) → Kiki re-enters untapped | **copy-inheritance** + **blink** (the inherited ability is itself a blink) |
| 618-1090 | Kiki-Jiki + Restoration Angel | as Felidar (Resto's ETB blinks Kiki) | **copy-inheritance** + **blink** |
| 618-1692 | Kiki-Jiki + Helm of the Host | Both are copiers; Helm copies Kiki, the Kiki-copy taps to copy Helm's-equipped… (engine-of-copies). Helm's own copy trigger is phase-gated (`at:combat`). | **copy-inheritance** (low priority — multi-copier, gated) |
| 618-4222--140 | Kiki-Jiki + Freed from the Real | Freed is an Aura (`{U}: Untap enchanted`); it does NOT untap Kiki by being copied (a copied Aura is a *token Aura* attached to nothing — it does not function). This combo is **mislabeled / not a copy-inheritance loop**; the real Freed loop is Freed + a mana-creature, not Kiki. | **other** (out of scope; likely a bench false-positive or a different line) |
| 618-7624 | Kiki-Jiki + Sea-Dasher Octopus | Sea-Dasher's payoff is **Mutate** (`Whenever this mutates, draw`). A *copy* of Sea-Dasher does not mutate on entry, so the copy yields no draw and no untap. **Not a copy-inheritance loop.** | **other** (out of scope) |
| 1090-2781 | Felidar Guardian + Restoration Angel | each blinks the other on ETB — pure two-card blink, no copier | **blink** (out of scope) |
| 1170-* | Displacer Kitten + Peregrine Drake + X | Displacer flickers Drake; Drake re-enters untapped → net mana | **blink** (out of scope) |
| 147-1987, 1987-*, 147-1810 | Ghostly Flicker / Dualcaster / Cackling Counterpart families | Ghostly Flicker = blink two permanents; Dualcaster/Cackling Counterpart copy a **spell** (not a token-copy-of-a-creature) | **blink** and/or **spell-copy** (both out of scope — see §8) |
| 11-3368 | Narset's Reversal + Reiterate | spell-copy + bounce (copy a *spell*, return it to hand) | **spell-copy** (out of scope) |
| 1385-*, 2034-*, 2511-*, 2577-*, 261-* | Warren Soultrader / Gravecrawler / aristocrats | death/sac/recursion aristocrat loops | **other arms** (out of scope — not copy) |

**Genuinely copy-inheritance and in scope:** 618-4404 (Corridor Monitor), 618-2781 (Felidar),
618-1090 (Restoration Angel), 618-1692 (Helm). Of these, 618-4404 is the **purest** (the inherited
ability is a plain `untap` — no second arm needed) and is the lead acceptance combo. 618-2781 /
618-1090 require the **blink arm too** to fully close, so copy-inheritance alone makes them
*recognizable* but the loop only certifies once blink lands; this feature targets them at AMBER (the
copy is grafted, but the inherited blink hop is not yet a flow arm). 618-1692 is multi-copier and
phase-gated (low priority).

---

## 3. The five decisions

### Decision 1 — Parse layer: is `CopyEffect` faithful? — **PROPOSED: NO CHANGE NEEDED (one optional sharpen)**

**Finding:** `CopyEffect` already captures everything the interaction graft needs:
- the copy **target filter** (`Target.Filter` — Kiki's `creature / !Legendary / Controller:You`);
- the **modifications** (`Modifications` — `abilityAdder:haste`, `supertypeRemover:[Legendary]`,
  `PowerToughnessOverride`, `TypeAdder`).

The one soft spot is `AbilityAdder.AbilityText` is **free text** (`"haste"`), a known initiative-05
sink (`destring-worklist.json` → `AbilityText_keyword_as_string_old_form` lists exactly
`HelmOfTheHost` + `KikiJikiMirrorBreaker`). This does **not** block copy-inheritance: the graft reads
the **copied card's** abilities, and the *added* keyword (haste) is irrelevant to the untap loop
(haste only matters for the copy attacking, which the engine doesn't model). So:

- **PROPOSED:** No parse change is required for copy-inheritance to land. The `AbilityAdder` free-text
  → typed-keyword conversion is owned by **05**, tracked there, and is *orthogonal* (it does not gate
  this feature; this feature does not gate it).
- **Rationale / soundness:** the graft's correctness depends only on the *copied* card's parsed
  abilities (already structured) and on the copy *target filter* (already structured). The modifications
  matter only insofar as `supertypeRemover` / `PowerToughnessOverride` could change which combo cards
  the filter admits — but they remove/override, they never *add* abilities the partner lacks, so they
  cannot manufacture a false inherited ability. No false-positive surface in parse.

### Decision 2 — Interaction layer: the inheritance graft — **PROPOSED: combo-aware graft at `Materialize` time**

The copy effect is the *only* port whose meaning depends on **the other cards in the combo set**
(every other port is intra-card). So the graft must run where the card set is known — `Materialize`
(`PortGraphEngine.cs:134`), not `PortWalk.Project` (which sees one card).

**PROPOSED mechanism (a new `Materialize` pass, after card-defined edges, before/with flow):**

1. **Surface copy emits.** `PortWalk` currently projects `emit:copy` (coarse). Promote it to a
   *typed* copy emit carrying the copy **target filter** as the port `Subject` and the **modifications**
   as port metadata — e.g. `emit:copy` with `Subject = CopyEffect.Target.Filter` and a new
   `PortNode.CopyMods` facet. (This is the parse↔projection step from `adding-a-flow-arm.md` recipe
   step 1/2 — remove `copy` from `known-coarse-projections.json`, add it to `PortWalkProjection`.)
   The copy emit's `Subject` is what the operator tiers on.

2. **Resolve "copy of target creature" to concrete combo cards.** For each copy emit, scan the OTHER
   `PortGraph`s in the set; a card C is a **graft candidate** iff C's card characteristics satisfy the
   copy emit's target `Subject` filter (via the existing `ObjectFilterRelations` operator against C's
   type line — `Subsumes`, not merely `Intersects`; see Decision 3).

3. **Graft C's ports onto the copy.** For each candidate C, clone C's `PortNode`s and
   `CardDefinedEdge`s under a synthesized **copy identity** (`Card = "<copier> copy of <C>"`), applying
   the copy's `Modifications`:
   - `abilityAdder` → add the inert keyword port (no effect on flow);
   - `supertypeRemover` / `typeAdder` / `powerToughnessOverride` → adjust the cloned ports' `Subject`
     type facets (Legendary stripped, etc.). These never *add abilities*, so they cannot widen the
     graft.
   The cloned card-defined edges stay GREEN (they are C's own causality, CR 707.2 preserves it). The
   copier's `emit:copy → <copy's etb>` is a **card-defined GREEN** edge (the copier definitely creates
   that object).

4. **Let the normal arms run.** Once the cloned ports exist, the existing flow grammar (`FlowFeasible`)
   and the generalized `TapGatesRenewed` (Decision 4) close the loop with **no new arm** — the graft is
   a *projection* expansion, the loop closure is the *connection* layer doing its normal job. This
   respects the split in `adding-a-flow-arm.md`.

**Where it lives:** a new private method `GraftCopyInheritance(graphs)` invoked at the top of
`Materialize`, returning an augmented graph list the rest of `Materialize` consumes unchanged.

### Decision 3 — Certainty / soundness: the tiering + false-positive guard — **PROPOSED: GREEN only when the operator *certifies* the copy admits the partner; else AMBER; never null-Subject GREEN**

**THE KEY RISK.** "A copy of target creature" can copy **any** legal creature. A naive graft
("for every copy emit, graft every creature in the set") manufactures an edge between **any** copier
and **any** creature — a false-positive explosion (it would claim Kiki + every vanilla creature is a
combo). The guard must make the graft *target-filter-aware* and *reliability-tiered*.

**PROPOSED tiering (mirrors the life-arm "operator decides" model, ADR-0002 §7):**

- The copy emit carries a **non-null Subject** = the copy target filter (Kiki: `creature / !Legendary
  / Controller:You`). Never null — a null Subject would hit the scalar null-default GREEN in
  `AddRulesEdge` (anti-pattern 3 in `adding-a-flow-arm.md`), which would be a false-positive vector
  (it would graft unconditionally). The floor is `{CardTypes:[creature]}` (the broadest a copy can
  ever be), never narrower-by-omission.
- **Graft admissibility = `Subsumes`, not `Intersects`.** Candidate C is grafted only if the copy
  target filter **subsumes** C's characteristics — i.e. the operator can *certify* C is a legal copy
  target (C is a non-legendary creature you control). `Intersects` is not enough: "could be a legal
  target" is not "is reliably a legal target." (Restoration Angel is a non-legendary creature → Kiki's
  filter subsumes it → graftable; a legendary creature is excluded by `!Legendary` → not grafted.)
- **Tier of the graft edge:**
  - **GREEN** — when the operator certifies (a) the copy filter `Subsumes` C **and** (b) the specific
    *inherited ability* that closes the loop is one C definitely has (it is in C's parsed
    `Oracle.Abilities`, not behind an optional/conditional the engine can't certify). Corridor Monitor's
    untap is unconditional → GREEN-eligible.
  - **AMBER** — when the copy is admissible but the closing hop is itself uncertain: the inherited
    ability is **optional** ("you *may* exile…" — Felidar/Restoration are `optional`), or the closing
    hop needs a not-yet-built arm (blink), or the target filter only `Intersects` C (can't certify).
- **The false-positive guard, spelled out:** the graft is gated on **`Subsumes(copyFilter, C)`**, and
  the *resulting* loop must still pass every existing §8 firability/balance/productivity check **and**
  the closing hop must be a real flow arm. A copier + a vanilla creature produces a grafted copy with
  **no ability that acts back on the copier** → no closing edge → no cycle → no false combo. The guard
  is therefore two-layered: (i) admissibility by `Subsumes` (don't graft creatures the copy can't legally
  be), (ii) closure by the existing arms (don't *report* a combo unless the grafted ability actually
  loops). A grafted port that doesn't close a cycle is harmless — it is dead weight the cycle finder
  ignores.
- **interaction-judge gate:** every new GREEN copy graft is dispatched to the `interaction-judge`
  (copy grafts are a prime false-positive surface). A GREEN the judge can't justify is a FAIL — stop,
  drop to AMBER, fix in the parse/operator layer (never fudge the GREEN — anti-pattern 2).

### Decision 4 — `TapGatesRenewed` generalization — **PROPOSED: accept (a) inherited self-untap and (b) target-untap aimed at the tap-gated source**

`TapGatesRenewed` (`PortGraphEngine.cs:708-749`) currently renews a tap-gated card only via a
**self**-untap (`emit:untap:self`) **on that same card**. For copies, the untap lives on the *copy*
(a different card identity), and Corridor Monitor's untap is `emit:untap` aimed at a **target**, not
self. Two precise extensions:

- **(a) Inherited self-untap.** Treat a grafted copy's `emit:untap:self` as renewing the tap-gated
  **copier** when the copier's own tap-cost ability is what *created* the copy (the copy's ETB fires in
  the copier's loop). Concretely: relax the `e.From.Card == card` constraint to also accept a card whose
  identity is a *copy grafted by* `card`.
- **(b) Target-untap at the source.** Accept `emit:untap` (not just `emit:untap:self`) when its
  **Subject/target filter subsumes the tap-gated source** — Corridor Monitor's "untap target artifact
  or creature" subsumes Kiki (a creature), so it renews Kiki's tap. This requires the untap emit to
  carry a target **Subject** (today `emit:untap` is bare — a projection sharpen: give the non-self
  untap a Subject = its target filter, then `TapGatesRenewed` matches it against the tap-gated card via
  the operator, same `Subsumes` discipline as Decision 3).
- **Soundness:** target-untap renewal is GREEN only when the operator certifies the target filter
  subsumes the source (Corridor Monitor's "artifact or creature" subsumes a creature — certain). A
  vague "untap target permanent" subsumes the source too (a creature is a permanent) → still GREEN.
  A narrower "untap target land" does **not** subsume a creature source → no renewal → tap stays
  floored. No false renewal.

### Decision 5 — PredefinedTokens: retire vs extend — **PROPOSED: do NOT extend for copies; relationship to 05 is "sibling, not parent"**

`PredefinedTokens.Registry` is a fixed lookup of CR 111.10 tokens (Treasure/Clue/Food) keyed by a
literal subtype string with a hand-coded intrinsic ability `Spec`. It is structurally the **wrong
home** for copies:

- A copy's abilities are **not fixed** — they are whatever the copied card has, resolved per combo.
  The Registry's "fixed spec" model cannot express "the abilities of whatever creature this turns out
  to be."
- **PROPOSED:** copies are served by the new `GraftCopyInheritance` pass (Decision 2), **not** the
  Registry. The Registry is left untouched by this feature.
- **Relationship to 05:** 05 retires the Registry by giving predefined-token golds a **nested token
  `Abilities` sub-AST** and projecting *that* (a token parses its **own** ability text). Copy-inheritance
  grafts **another card's already-resolved port graph**. These are **distinct but related**: both are
  "a created object has ports beyond `emit:token`," and both ultimately want `PortWalk` to recurse into
  a created object's abilities. The clean factoring is a shared helper `ProjectCreatedObjectPorts(...)`
  that 05's nested-AST path and this feature's graft path both call — but **neither blocks the other**:
  05 supplies the token's *own* parsed abilities; copy supplies the *partner's* abilities. **Pin:
  copy-inheritance is a SIBLING of 05's nested-ability work (shared recursion target), NOT a special
  case of it and NOT dependent on it.** Copy can land before, after, or alongside 05.

---

## 6. Sized implementation plan (three parallel tracks)

| Track | Work | Size | Files |
|---|---|---|---|
| **A — Parse/projection sharpen** (∥) | Promote `emit:copy` from coarse to typed: carry the target filter as Subject + modifications as a `PortNode` facet; remove `copy` from `known-coarse-projections.json`, add to `PortWalkProjection`; give non-self `emit:untap` a target Subject | **S** | `PortGraph.cs`, `PortWalkProjection.cs`, `known-coarse-projections.json`, `PortLabel.cs` |
| **B — Interaction graft** (∥, depends on A's typed emit) | `GraftCopyInheritance` pass in `Materialize`: resolve copy filter → candidate cards by `Subsumes`, clone their ports under a copy identity with modifications applied, GREEN card-defined edge copier→copy-ETB | **M** | `PortGraphEngine.cs` (new pass), maybe `PortGraph.cs` (clone helper) |
| **C — Engine renewal generalization** (∥) | Generalize `TapGatesRenewed`: accept inherited self-untap (relax `card` match to copies grafted by it) and target-untap that `Subsumes` the tap-gated source | **S** | `PortGraphEngine.cs:708` |

Tracks A and C are independent; B depends on A's typed copy emit. Then the integration step
(close Kiki + Corridor Monitor) + the gate sequence below. Estimated: ~1 focused batch for A+C, ~1 for
B, ~1 for integration + judge.

### Acceptance (what flips, to which tier, recall target)

| Combo | Current | Target tier | Why |
|---|---|---|---|
| 618-4404 Kiki + Corridor Monitor | Missed | **GREEN** | inherited untap is unconditional; target-untap subsumes Kiki — operator certifies |
| 618-2781 Kiki + Felidar Guardian | Missed | **AMBER** | copy grafts, but the inherited blink ability is `optional` AND needs the blink arm (out of scope) |
| 618-1090 Kiki + Restoration Angel | Missed | **AMBER** | same as Felidar |
| 618-1692 Kiki + Helm of the Host | Missed | (stretch) AMBER | multi-copier, phase-gated; low priority |

**Recall target:** copy-inheritance alone lands **+1 GREEN** (618-4404) → recall@Green 0.121→~0.152
(4→5 of 33), and **+2 AMBER** (Felidar, Resto) → recall@Amber 0.364→~0.424. The blink arm (separate
feature) would later flip Felidar/Resto from AMBER to GREEN and unblock the 1170-* / 147-* / 1987-*
flicker combos.

### Gate sequence (the safety net, from `adding-a-flow-arm.md`)

1. `PortWalkExhaustivenessTests` (03 ratchet) **shrinks** — `copy` and non-self `untap` left the
   coarse allowlist.
2. `PortWalkSentinelSnapshotTest` regenerated with a justified diff (the new typed copy/untap labels).
3. `nx run bench:recall` — recall **did not decrease** (it rose; +1 GREEN, +2 AMBER).
4. **`interaction-judge` PROCEED** on every new GREEN copy graft — the false-positive guard. A GREEN
   it can't justify is a FAIL: stop, drop to AMBER, fix in parse/operator (never fudge — anti-pattern 2).
5. `nx run mast:test` green.

### Judge-gating plan

Copy grafts are the prime false-positive surface (a naive graft manufactures edges between any copier
and any creature). Dispatch the **`interaction-judge`** on:
- every new **GREEN** copy graft (618-4404) — confirm the inherited ability is genuinely unconditional
  and the copy filter genuinely subsumes the partner;
- a **negative control** — Kiki + a vanilla creature (e.g. a grafted bear) must produce **no cycle**
  (the guard's two layers hold);
- the **AMBER** Felidar/Resto grafts — confirm the AMBER is soundly irreducible (optional ability +
  missing blink arm), not a fixable operator gap.

---

## 7. Pinned relationships (one-liners)

- **05 (de-string leaves):** SIBLING. Shared recursion target (`ProjectCreatedObjectPorts`); neither
  blocks the other. 05 = a token's OWN abilities; copy = a partner's resolved port graph.
- **Blink/flicker arm:** SEPARATE FEATURE, out of scope. Unblocks 1090-2781, 1170-*, 147-1987,
  1987-*, and upgrades Kiki+Felidar / Kiki+Resto from AMBER to GREEN.
- **Spell-copy (Dualcaster/Cackling Counterpart/Narset's Reversal):** SEPARATE, out of scope (a copy
  of a *spell on the stack*, not a token-copy-of-a-permanent — `CopyEffect.MayChooseNewTargets` is its
  parse home, but the interaction model for stack-copies is a different arm).

---

## 8. Acceptance pins

See `tests/magic-ast-tests/Tests/Interaction/CopyInheritanceScopeTest.cs` — `[Ignore]`-pinned tests
defining the target tiers for Kiki + Corridor Monitor (GREEN) and Kiki + Restoration Angel (AMBER),
plus the negative control (Kiki + vanilla creature → no cycle). They skip until the feature lands.
