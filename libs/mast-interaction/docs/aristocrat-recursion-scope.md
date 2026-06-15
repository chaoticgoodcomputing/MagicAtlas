# Aristocrat recursion (cast-from-graveyard) — SCOPE / design

**Status:** ✅ **BUILT (2026-06-15)** — graveyard-recursion arm implemented; interaction-judge-verified
(`docs/judgments/verdict-2026-06-15-aristocrat-recursion.json`, PROCEED). All 3 acceptance pins are now
LIVE and pass. **recall@(Green+Amber) 0.39 → 0.61** (all 7 Gravecrawler aristocrat combos reconstruct).

> **Two corrections this doc got wrong (the build + judge fixed them; trust the verdict over the prose below):**
> 1. **Gravecrawler recasts for `{B}`, not `{1}{B}`.** Its `alternativeCast` has no stated cost → cast for
>    its own mana cost (CR 601.3e). Every "`{1}{B}` = 2 mana" / mana-shortfall claim below is **wrong**.
> 2. **The Warren variants are AMBER for a *different* reason** than the mana shortfall claimed in
>    Decision 4. With the true `{B}` cost, one Treasure fully pays the recast — **no mana shortfall**. The
>    real floor is Warren Soultrader's **unfed `Pay 1 life` co-cost**: `ConjunctionHolds` requires it
>    loop-fed, but there is no `(life, pay)` flow arm (only `(life, trigger)`), so `CoCostsSatisfied=false`
>    → AMBER (CR 118.3/119.4 — life is a real resource MAST can't certify the loop refills). Sound AMBER,
>    not a false negative, not Red.
>
> The GREEN (Gravecrawler + Pitiless Plunderer + Ashnod's Altar) **is a genuine infinite loop** (mana-positive:
> Ashnod's `{C}{C}` + a Pitiless Treasure cover the single `{B}` each iteration, with a surplus Treasure).
> It is a 7-hop cycle, so the product/bench `LengthBound=5` reads it AMBER while the engine + the unit pin
> (unbounded `FindCycles`) tier it GREEN — making it product-visible is a separate `LengthBound` decision.

**Companion reading:** `adding-a-flow-arm.md` (the projection↔connection split this feature must
respect, + the four anti-patterns), `copy-inheritance-scope.md` (the structure this doc mirrors),
ADR-0002 (single-role port model; §5 card-defined vs §7 operator tiering; **§8 firability/balance**;
**§8-B the one-shot-self-removal prune + Persist/Undying carve-out** — directly load-bearing here).

This is the **highest-yield missed-combo cluster**: 7 eligible-but-Missed combos all built on
**Gravecrawler** + a free/cheap sac outlet + a death/ETB payoff. Each card already has a gold fixture
(so each combo is already bench-**eligible** — the gap is purely interaction-layer, plus one parse
sharpen), and the popular ones are large (Warren+Gravecrawler+Blood Artist = 38 733; Gravecrawler+
Pitiless+Ashnod's = 32 582; Warren+Gravecrawler+Zulaport = 34 860).

---

## 1. The family (from `tools/bench/MagicAtlas.Bench/bench-report.json`, `Outcome: Missed`)

Combo ids in the report are **positional**, not stable card ids (the same card appears under several
numeric ids — Warren Soultrader is 261/1385/2741/2842/4231). The table below is keyed by **card
content**, which is canonical. All seven contain Gravecrawler and are eligible-but-Missed.

> The prompt's id→card guesses were partly transposed (e.g. `1385-2577-5670` is actually
> Warren+Gravecrawler+**Zulaport**, not Blood Artist; the Night-of-Souls combo is `2577-3983-4871`).
> The card-content list below is the ground truth read out of the report.

### Per-combo arm map (the deliverable table)

Each row: the loop mechanism, the arms it needs (✓ = exists, **NEW** = the recursion arm, ⟂ = a
distinct hop), and what blocks GREEN today.

| # | Combo (cards) | Pop | Loop mechanism | Arms it needs | Blocked-on / predicted tier |
|---|---|---|---|---|---|
| 1 | **Gravecrawler + Pitiless Plunderer + Ashnod's Altar** | 32 582 | Ashnod sacs GC (free sac → {C}{C}) → GC dies → Pitiless makes a **Treasure** (any-colour) → GC recast {1}{B} (Treasure pays {B}, {C} pays {1}) → re-enter → re-sac | `sac`✓, sac→death bridge✓, dies-trigger✓, **token→mana**✓ (Treasure & {C}{C}), **recast arm (NEW)** + its `pay:mana` recast co-cost | recursion arm + recast-cost projection. **→ GREEN** (mana-positive: Treasure's `emit:mana:any` covers the {B}; Ashnod's {C}{C} covers the {1}; surplus mana → the loop nets unbounded Treasures/mana, §8 `Productive`) |
| 2 | **Gravecrawler + Pitiless Plunderer + Night of Souls' Betrayal** | 87 | NOSB gives all creatures −1/−1 → GC enters as a **1/0**, dies to SBA immediately → Pitiless Treasure → recast {1}{B} (Treasure pays it) → re-enter → dies again. **No sac outlet at all.** | dies-trigger✓, **token→mana**✓, **recast arm (NEW)**, **+ an SBA-death-on-recast bridge (⟂ NEW, distinct)** | recursion arm **+ a "enters-then-dies-to-SBA" hop the engine does not model** (`modifyPT` −1/−1 is an inert port today). **→ AMBER at best** (the death-on-entry is not an existing arm; needs a P/T-lethality reasoning hop — out of this scope) |
| 3 | **Gravecrawler + Pitiless Plunderer + Bloodflow Connoisseur** | 369 | Bloodflow sacs GC (free sac → +1/+1 counter on itself) → GC dies → Pitiless Treasure → recast {1}{B} (Treasure pays it) → re-enter → re-sac | `sac`✓, bridge✓, dies-trigger✓, **token→mana**✓, **recast arm (NEW)** | recursion arm. **→ GREEN** (Pitiless's Treasure is the mana source that pays the recast; the +1/+1 counter is the §8 non-mana `Productive` output). *Without Pitiless this would be AMBER — see §4.* |
| 4 | **Warren Soultrader + Gravecrawler + Blood Artist** | 38 733 | Warren sacs GC (`Pay 1 life, Sac another creature` → **Treasure**) → GC dies → Blood Artist drains 1 (death payoff) → recast {1}{B} (Warren's Treasure pays it) → re-enter → re-sac | `sac`✓ (Warren has `ExcludeSelf` — GC, not Warren), bridge✓, dies-trigger✓, **token→mana**✓ (Warren's Treasure), **life-drain payoff**✓, **recast arm (NEW)** | recursion arm. **→ GREEN** (Warren's Treasure `emit:mana:any` pays the {1}{B}; the drain is the §8 `Productive` non-mana output). **Caveat: the `Pay 1 life` co-cost** — see §4 (the life co-cost is paid down by Blood Artist's own gain, so it does not floor; a *bare* Warren loop with no life-back would be the AMBER edge case) |
| 5 | **Warren Soultrader + Gravecrawler + Zulaport Cutthroat** | 34 860 | as #4, payoff is Zulaport's drain (each opponent loses 1 / you gain 1 on death) | identical to #4 | recursion arm. **→ GREEN** (Zulaport's `you gain 1` even repays Warren's `Pay 1 life`; drain is the productive output) |
| 6 | **Warren Soultrader + Gravecrawler + Essence Warden** | 589 | Warren sacs GC → GC dies (Treasure) → recast → **GC ENTERS** → Essence Warden gains 1 on the *entry* (not the death) → re-sac | `sac`✓, bridge✓, **token→mana**✓, **recast arm (NEW)**, **+ the recast's `etb` → ETB-trigger hop** | recursion arm. The payoff fires on **re-entry** (`etb`/Enters trigger), so the recast hop must surface an `etb` port the ETB payoff consumes. **→ GREEN** (the recast definitely re-enters; Essence Warden's "creature you control enters" subsumes GC; Treasure pays the recast) |
| 7 | **Warren Soultrader + Gravecrawler + Suture Priest** | 575 | as #6 — Suture Priest's "another creature enters under your control → you gain 1" fires on GC's re-entry | identical to #6 | recursion arm + recast→`etb` hop. **→ GREEN** (same shape as #6) |

**Already-AMBER siblings (do NOT touch — they reconstruct without recursion):** the
**Chatterfang + Warren Soultrader** combos (`1385-3000-5670`, `261-3000-5670`, `2741-3000-5670`,
`2842-3000-5670`, `3000-4231-5670`) already reconstruct as **AMBER** via the existing
**token→sac → Chatterfang-doubler** machinery (no Gravecrawler; the Squirrel-token engine). The prompt
flagged "Chatterfang/Warren combos already partially reconstruct as AMBER — check": **confirmed** —
they are a *different* family (token-doubler, not graveyard-recursion) and are out of scope. The
recursion arm neither helps nor harms them.

---

## 2. The mechanism

The canonical aristocrat-recursion loop, hop by hop:

```
  GC on battlefield
    └─(a) FREE/CHEAP SAC OUTLET sacrifices GC          sac:creature:controlled        [exists ✓]
    └─(b) sac → GC dies to graveyard (CR 701.21a→700.4) ltb:creature:to-graveyard      [bridge exists ✓]
    └─(c) DEATH PAYOFF triggers (drain / Treasure)      trigger:life:* / emit:token    [exists ✓]
    └─(d) GC RECAST FROM GRAVEYARD ({1}{B}, gated on    alternativeCast/FromZone:GY    [NEW ARM]
          "control a Zombie")                            + pay:mana co-cost {1}{B}      [recast cost projection: NEW]
    └─(e) GC RE-ENTERS the battlefield                  emit:returntobattlefield /     [the recursion arm's emit;
          (an ETB payoff may fire here — #6/#7)          recast-enter; etb consume       feeds (a) again, and (c'))
    └─ back to (a): the re-entered GC refuels the sac.
```

**(d) is the genuinely NEW arm.** Everything else exists. The new arm is **graveyard-recursion /
cast-from-graveyard**: a creature that *died to the graveyard* + has a **static cast-from-graveyard
permission** (Gravecrawler's `alternativeCast` / `FromZone: Graveyard`) → it can leave the graveyard
and **re-enter the battlefield**, producing an object that **refuels the sac** (the same way a created
token refuels a sac in the existing `(token, sac)` arm). The recast carries a **mana co-cost** ({1}{B}),
which is what makes the loop's GREEN/AMBER tier turn on mana balance (§4).

---

## 3. What's already covered (reuse — do NOT rebuild)

Verified against the code; reuse exactly these. *Nothing in this list is re-implemented by this feature.*

| Capability | Where | Evidence |
|---|---|---|
| The **`sac` cost** (controlled fodder) | `PortGraph.cs:315` (`case "sacrifice"` → `PortLabel.SacrificeCost`) | Ashnod's/Warren's/Bloodflow's `sacrifice` cost → `sac:creature:controlled` (Warren's adds `:another` from `ExcludeSelf`) |
| The **sac → death bridge** (CR 701.21a→700.4) | `PortGraphEngine.cs:169-171` (Materialize step 3) | over-approximate by design; a sac consume bridges to a `ltb:…:to-graveyard` consume |
| The **dies-trigger** | `PortLabel.DeathTrigger` (`PortGraph.cs:291`) | Blood Artist / Zulaport / Pitiless all carry `ltb:creature:to-graveyard[:controlled]` consumes |
| The **life-drain payoff** (CR 119) — *landed 2026-06-15* | life flow arm (`FlowFeasible ("life","trigger")`, `adding-a-flow-arm.md`) | **confirmed live**: `loseLife`/`gainLife` → `emit:life:*`; recall@Green rose to 0.1515. Blood Artist & Zulaport project the life ports |
| The **token→sac** arm (Pitiless/Ashnod variants) | `FlowFeasible ("token","sac")` (`PortGraphEngine.cs:465`) | a created token (Treasure) refuels a sac |
| The **token→mana** path (Treasure / Ashnod's {C}{C}) | `PredefinedTokens.cs:36` (`Treasure` → `emit:mana:any`); `addMana` → `PortLabel.ManaEmit` (`PortGraph.cs:392`) | Treasure projects `sac`+`tap`+`emit:mana:any`; Ashnod's projects `emit:mana:colorless` (×2) |
| The **§8 mana-balance / per-colour / productivity** machinery | `PortGraphEngine.cs:816-901` (`ManaBalanced`, `ManaProductive`, `GatherManaFlow`) | **this is the GREEN/AMBER decider for the recast cost** — see §4 |

The recursion arm's job is therefore narrow: surface the recast as an emit that **refuels the sac** (a
new emit→consume the engine doesn't make today), carrying a **`pay:mana` recast co-cost** so the
existing §8 balance machinery tiers it. No new tiering logic — the existing operator/§8 does the work.

---

## 4. The five decisions

### Decision 1 — Parse layer: is the cast-from-graveyard permission faithfully modeled? — **PROPOSED: YES, no parse change for (d); one OPTIONAL sharpen for the recast cost**

**Finding (verified, `Gravecrawler.json:42-58`):** the permission is *already* parsed faithfully as a
**static** ability:

```json
{ "EffectType": "alternativeCast", "FromZone": "Graveyard",
  "Condition": { "ConditionType": "count",
    "Filter": { "Subtypes": ["Zombie"], "Controller": "You" },
    "Count": { "Operator": "GreaterThanOrEqual", "Value": 1 } } }
```

This carries everything the arm needs: the **zone** (`FromZone: Graveyard`), and the **gating
condition** ("control a Zombie" — and GC is itself a Zombie, so the condition is self-satisfied while
GC or another Zombie is in play). It is currently **coarse** in the interaction layer:
`known-coarse-projections.json:6` lists `alternativeCast` as *"no flow rule consumes it yet"* (and
`mayPlayFromGraveyard:80` is the keyword sibling). **That is the projection sharpen this feature
performs** (parse-describe side of the split): promote `alternativeCast` with `FromZone: Graveyard`
from coarse to a typed **recast emit** carrying the gating `Condition.Filter` as its **Subject**.

- **PROPOSED:** No change to the *parser/AST* — the gold is faithful. The change is the
  **PortWalk projection** (move `alternativeCast` off the coarse allowlist, add to
  `PortWalkProjection.EffectTypes`).
- **The one OPTIONAL sharpen (recast cost):** the recast's **mana cost** is the card's own
  `{1}{B}` mana-cost attribute (`Gravecrawler.json:62-75`), not text inside the `alternativeCast`
  effect. The arm must read that mana-cost attribute to project the `pay:mana:{1}{B}` recast co-cost.
  This is a *projection* read of already-parsed data, not a parser change. (CR 702.x: an alternative
  cast that doesn't say "rather than pay its mana cost" still costs the card's mana cost — Gravecrawler
  pays {1}{B} from the graveyard.) **No false-positive surface in parse:** the arm reads structured zone
  + condition + mana cost; it invents nothing.

**Soundness:** the permission is a *static* property of a specific card; the arm gates on its presence
(§5). The condition ("control a Zombie") is self-satisfied for GC (it is a Zombie) — the engine may
treat a self-Zombie permission as live within the loop; a *non-self* zombie-gate (a non-Zombie with
"cast from GY while you control a Zombie") would need the Zombie supplied by the board, which the engine
can't certify → that variant floors to AMBER (the condition is an unmet board predicate). For the
family in scope, GC's permission is self-certifying.

### Decision 2 — Interaction layer: the recursion arm — **PROPOSED: a new `(recast, sac)` flow arm (+ a `(recast-enter, etb)` arm for the ETB payoffs), the projection-then-connection split**

The recast is the **only** new hop. Two sub-parts, both following `adding-a-flow-arm.md`'s recipe:

**(2a) Project a typed recast emit + its co-cost (parse/projection side).**
- In `PortGraph.cs` `EmitPort`, add an `alternativeCast` case (guarded on `FromZone == "Graveyard"`)
  that emits **`emit:returntobattlefield:self`** (reusing the *existing* return-to-battlefield label
  the §8-B Persist/Undying carve-out already keys on — `PortGraphEngine.cs:982`) with **Subject = the
  card's own self-filter** (it is *this* creature re-entering, CR 400.7 makes it a new object, but for
  the loop it is the same card-identity refueling). Carry the gating `Condition.Filter` as a second
  facet/metadata so the operator can tier the zombie-gate.
- Attach the recast **`pay:mana`** co-cost (read from the card's mana-cost attribute) to that emit's
  ability, exactly as activated abilities attach their costs — so `GatherManaFlow` sees the {1}{B}
  cost on the cycle and `ManaBalanced` can floor it (§4).
- Remove `alternativeCast` from `known-coarse-projections.json`; add it to
  `PortWalkProjection.EffectTypes`. The 03 ratchet **shrinks** (anti-pattern 4 avoided).

**(2b) Add the flow arm (connection side, `PortGraphEngine.FlowFeasible`).** One `switch` clause:
- **`(returntobattlefield, sac)`** — a creature re-entering the battlefield refuels a `sac` consume
  whose fodder filter **Subsumes** the re-entered creature (a re-entered GC, a creature, satisfies
  "Sacrifice a creature"). This is the structural twin of the existing `(token, sac)` arm
  (`TokenSatisfiesAtCreation`), and reuses its `Subsumes` discipline. **The arm decides feasibility
  only; the operator decides certainty** (ADR-0002 §7).
- **`(returntobattlefield, etb)`** *(or reuse the existing `etb` consume directly via the
  card-defined enter edge)* — for #6/#7, the recast's re-entry is an ETB event that feeds an
  Enters-trigger payoff (Essence Warden / Suture Priest). The re-entry is a card-defined GREEN edge
  from the recast emit to GC's own (none — GC has no ETB) *and* a cross-card flow edge into the
  payoff's `etb:creature:controlled` consume. (The Enters trigger already projects via
  `PortLabel.EntersTrigger`; the only new piece is that the recast emit can *feed* it.)

**Where it lives:** unlike copy-inheritance (which needed a combo-aware `Materialize` *graft* because
the copy's meaning depends on other cards), the recursion arm is **intra-card on the emit side**
(Gravecrawler's own static permission) and a **plain cross-card flow edge** on the consume side. So it
does **NOT** need a Materialize graft pass — it is a normal `FlowFeasible` arm (Materialize step 2). It
is strictly simpler than copy-inheritance.

### Decision 3 — Dependency check: does this need initiative-05 (nested-ability) infra? — **PROPOSED: NO. Self-contained flow arm + the parse-projection sharpen.**

**Verdict: NOT blocked on 05.** The reasoning, pinned:
- Gravecrawler's cast-from-graveyard permission is a **top-level static effect** on the card itself
  (`alternativeCast`), already parsed — it is **not** a nested ability of a created object, so it does
  not touch 05's nested-ability sink (which is about a token's *own* ability text, `copy-inheritance
  -scope.md` Decision 5).
- The recast→re-enter→sac loop **is expressible with existing port roles**: the re-entry reuses the
  **existing** `emit:returntobattlefield` label (the §8-B carve-out already recognizes it as "the
  source can be on the battlefield again"); the sac refuel reuses the **existing** `sac` consume role
  and the **existing** `Subsumes` operator; the §8 balance reuses the **existing** `pay:mana` co-cost
  plumbing. No new port role, no nested recursion, no 05.
- The Treasure-mana variants reuse the **existing** `PredefinedTokens` Treasure spec (which *is* a
  nested-ability shim today but already lands) — so even there, no 05 dependency.

The one nuance: **§8-B's one-shot-self-removal prune** (`IsOneShotSelfRemoval`,
`PortGraphEngine.cs:966`) drops cycles through a `ltb:…:to-graveyard:self` consume **unless** the same
self-death drives an `emit:returntobattlefield`. Gravecrawler's death IS its own death (a self-death),
and the recast IS a self-return — so the arm must emit `emit:returntobattlefield` (Decision 2a chooses
exactly this label) **for the §8-B carve-out to retain the cycle**. This is the precise reason the
return-to-battlefield label is the right projection: it dovetails with an *existing* carve-out rather
than needing new prune logic. **This is the key architectural fit and must be verified by an
interaction-judge** (that the carve-out fires for a recast self-return the same way it fires for
Persist/Undying).

### Decision 4 — Soundness / false-positive guard + the mana-balance tiering (THE KEY) — **PROPOSED: gate the arm on a real cast-from-graveyard permission; tier GREEN/AMBER on the §8 recast mana-balance; never a no-permission recast**

**THE KEY RISK.** A naive "anything in a graveyard refuels a sac" arm would manufacture combos
everywhere (every aristocrat deck has creatures dying; a free sac outlet + any dead creature would
false-certify). Two-layer guard:

- **Guard layer (i) — admissibility: the recast emit exists ONLY when the card carries a real
  cast-from-graveyard permission.** The arm keys on `alternativeCast` with `FromZone: Graveyard` (or
  the `mayPlayFromGraveyard` keyword). A creature with no such permission projects **no** recast emit,
  so it cannot refuel the sac from the graveyard — no edge, no cycle. (A vanilla creature that dies
  stays dead, as it should. This is the recursion analogue of copy-inheritance's "a vanilla body has
  no ports to clone.") **The recast emit's Subject is non-null** = the card's self-filter + the
  zombie-gate condition; never a null-Subject GREEN (anti-pattern 3 — `AddRulesEdge` defaults a null
  Subject to GREEN, a false-positive vector for non-fungible resources).
- **Guard layer (ii) — closure + §8 balance.** Even when the recast emit exists, the loop must close
  AND pass §8. The mana-balance is where GREEN vs AMBER lives:

**The mana-balance tiering (spelled out — this is where the false-positive surface is tamed):**

The recast costs **{1}{B}** (1 generic + 1 **black** pip). The existing `ManaBalanced`
(`PortGraphEngine.cs:823`) does **per-colour** balance (CR 107.4): a coloured pip must be paid by its
own colour or the flexible `any` pool; colourless can pay generic but **never** a coloured pip.

| Sac outlet / mana source | Produces per iter | Pays {1}{B}? | Tier verdict |
|---|---|---|---|
| **Pitiless Plunderer** (Treasure on death → `emit:mana:any`) | 1 `any` mana | {B} ← `any`, {1} ← `any`? (1 Treasure = 1 mana, covers one pip; the loop makes a Treasure *every* death, so steady-state ≥1/iter — but {1}{B}=2 needs 2 mana) | with a **second** mana source (Ashnod's {C}{C}, combo #1) → the {C} pays {1}, the Treasure's `any` pays {B} → **balanced → GREEN**. **Pitiless ALONE** (1 Treasure = 1 `any` vs 2-mana recast) is short by 1 → would floor **AMBER** — but #3 (Pitiless+Bloodflow) sacs *free*, so the only cost is the recast {1}{B}=2 against 1 Treasure → **provably short → AMBER** unless a second mana source. **Re-examine #3: see note.** |
| **Ashnod's Altar** ({C}{C} per sac) ALONE | 2 colourless | {1} ← {C} ✓, but **{B} ← {C} ✗** (colourless can't pay a coloured pip) | **provably short on {B} → AMBER** |
| **Pitiless + Ashnod's** (combo #1) | 2 colourless + 1 `any`/death | {1} ← {C}, {B} ← Treasure `any` ✓ | **balanced, with surplus → GREEN** |
| **Warren Soultrader** (Treasure per sac → `any`) | 1 `any` (+ `Pay 1 life` co-cost) | {B} ← `any`; {1} ← ? (1 Treasure = 1 mana, recast = 2) | **short by 1 generic → AMBER** *unless* the loop produces ≥2 mana/iter. **See note — this is the crux for #4/#5.** |

**NOTE — the recast is 2 mana, most single sources make 1/iter.** This is the genuine subtlety the
prompt under-specified. The honest reading:
- **Combo #1 (Pitiless + Ashnod's)**: two mana sources (2 colourless + 1 any) vs a 2-mana recast →
  **mana-positive → GREEN.** This is the cleanest GREEN; it is the **lead GREEN pin**.
- **Combos #4/#5 (Warren + Blood Artist/Zulaport)**: Warren's single Treasure = 1 `any` mana vs the
  {1}{B}=2 recast → **mana-short by 1 → the honest tier is AMBER** under the *current* §8 machinery,
  **not GREEN.** The drain/life is the productive output, but `ManaBalanced` floors a provable mana
  shortfall regardless. *This corrects the prompt's implied GREEN for the Warren variants.* They are
  the **AMBER pin** (mana-neutral/negative). To earn GREEN later would require either (a) a second mana
  source, or (b) recognizing the Treasure pays {B} and the {1} is covered by *accumulating* Treasures
  across iterations (a steady-state argument §8 does not currently make — and **should not** be fudged;
  the AMBER is sound until a §8 enhancement or a parse sharpen earns it, per anti-pattern 2).
- **Combo #3 (Pitiless + Bloodflow)**: free sac (Bloodflow) + 1 Treasure/death = 1 `any` vs 2-mana
  recast → **short by 1 → AMBER** (revised down from the §1 table's optimistic GREEN — the per-colour
  balance is fine on {B} but the generic {1} is unfed at 1 mana/iter). *Pinned as AMBER-eligible.*
- **Combo #2 (Night of Souls' Betrayal)**: needs the unmodeled "enters-as-1/0-dies-to-SBA" hop →
  **AMBER at best, likely Missed** until a P/T-lethality arm (out of scope).

**Revised right-sizing (honest):** the recursion arm lands **one clean GREEN** (combo #1,
Pitiless+Ashnod's — two mana sources cover the {1}{B}) and **several AMBERs** (#3, #4, #5, and #6/#7
turn on the ETB-feed hop). The Warren variants are AMBER, not GREEN, under current §8 — **this is the
soundness-preserving call** and the headline correction this scope makes.

**interaction-judge gate:** every new GREEN recast edge (combo #1) is dispatched to the
`interaction-judge` — recast loops are a prime false-positive surface. A GREEN it can't justify is a
FAIL: stop, drop to AMBER, fix in parse/operator, never fudge the GREEN (anti-pattern 2).

### Decision 5 — Right-sizing + recall target — **PROPOSED: +1 GREEN, +3–4 AMBER**

| Combo | Current | PROPOSED target | Why |
|---|---|---|---|
| Gravecrawler + Pitiless + Ashnod's (#1) | Missed | **GREEN** | two mana sources cover {1}{B}; mana-positive; §8 certifies |
| Warren + Gravecrawler + Blood Artist (#4) | Missed | **AMBER** | recast {1}{B}=2 vs Warren's 1 Treasure/iter → provable {1} shortfall; drain is productive but mana floors it |
| Warren + Gravecrawler + Zulaport (#5) | Missed | **AMBER** | same mana shortfall as #4 |
| Gravecrawler + Pitiless + Bloodflow (#3) | Missed | **AMBER** | 1 Treasure/iter vs 2-mana recast → {1} short |
| Warren + Gravecrawler + Essence Warden (#6) | Missed | **AMBER** | needs recast→`etb`-feed hop; + the Warren mana shortfall |
| Warren + Gravecrawler + Suture Priest (#7) | Missed | **AMBER** | same as #6 |
| Gravecrawler + Pitiless + Night of Souls' (#2) | Missed | **(stretch) AMBER / Missed** | needs the unmodeled enters-as-1/0 SBA hop — out of scope |

**Recall target:** **+1 GREEN** (combo #1) → recall@Green 0.1515 → ~0.182 (5→6 of 33), and **+3–4
AMBER** (#3, #4, #5, and #6/#7 if the ETB-feed hop lands) → recall@Amber 0.3939 → ~0.485–0.515. The
AMBER→GREEN upgrade for the Warren variants is a later **§8 steady-state** or **parse** enhancement
(the multi-Treasure accumulation argument), tracked but not fudged here.

---

## 6. Sized implementation plan (when ratified)

| Track | Work | Size | Files |
|---|---|---|---|
| **A — Parse/projection sharpen** | Promote `alternativeCast`(`FromZone:Graveyard`)/`mayPlayFromGraveyard` from coarse to a typed **`emit:returntobattlefield:self`** carrying self-filter Subject + the zombie-gate condition; attach the card's mana-cost as the recast `pay:mana` co-cost; remove from `known-coarse-projections.json`, add to `PortWalkProjection.EffectTypes` | **S** | `PortGraph.cs` (`EmitPort` case + cost attach), `PortWalkProjection.cs`, `known-coarse-projections.json`, maybe `PortLabel.cs` |
| **B — Recursion flow arm** | `FlowFeasible` clauses: `(returntobattlefield, sac)` (re-entered creature refuels a sac whose fodder `Subsumes` it — twin of `(token,sac)`) and the `(returntobattlefield, etb)` feed for ETB payoffs | **S** | `PortGraphEngine.cs` (`FlowFeasible` + a tiny `Subsumes` helper, reuse `TokenSatisfiesAtCreation` shape) |
| **C — §8-B carve-out verification** | Confirm `IsOneShotSelfRemoval` retains the recast cycle (the self-death drives a self-`emit:returntobattlefield`) — likely **no code change**, just a conformance test + judge | **XS** | `PortGraphEngine.cs:966` (verify), new conformance fixture |

A and B are sequential (B reads A's emit); C is a verification gate. ~1 focused batch for A+B, ~1 for
integration + judge.

### Gate sequence (from `adding-a-flow-arm.md` — the safety net)

1. `PortWalkExhaustivenessTests` (03 ratchet) **shrinks** — `alternativeCast` left the coarse allowlist.
2. `PortWalkSentinelSnapshotTest` regenerated with a justified diff (the new recast label).
3. `nx run bench:recall` — recall **did not decrease** (it rose; +1 GREEN, +3–4 AMBER).
4. **`interaction-judge` PROCEED** on every new GREEN (combo #1) — the false-positive guard. A GREEN
   it can't justify is a FAIL: stop, drop to AMBER, fix in parse/operator (never fudge — anti-pattern 2).
5. `nx run mast:test` green.

### Judge-gating plan

Recast loops are a prime false-positive surface (a careless arm refuels a sac from any dead creature).
Dispatch the **`interaction-judge`** on:
- the new **GREEN** (combo #1, Pitiless+Ashnod's) — confirm the recast permission is real, the §8
  per-colour balance genuinely covers {1}{B}, and the §8-B carve-out correctly retains the self-death
  cycle (the recast self-return is the dual of Persist/Undying);
- a **negative control** — Gravecrawler-permission removed (or a non-recursive creature) + a free sac
  outlet → **no cycle** (guard layer i);
- the **AMBER** Warren variants — confirm the AMBER is soundly irreducible (a *provable* {1} mana
  shortfall at 1 Treasure/iter vs a 2-mana recast), not a fixable operator gap.

---

## 7. Pinned relationships (one-liners)

- **Copy-inheritance:** SEPARATE, already landed. Recursion is *not* a graft (intra-card emit), so it
  needs **no Materialize pass** — strictly simpler. Shares only the `Subsumes` discipline.
- **Initiative-05 (de-string leaves / nested abilities):** **NOT a dependency** (Decision 3). GC's
  permission is a top-level static, already parsed.
- **§8-B one-shot-self-removal prune:** **LOAD-BEARING fit** — the recast must emit
  `emit:returntobattlefield` so the existing Persist/Undying carve-out retains the self-death cycle.
- **Token-doubler family (Chatterfang × Warren):** SEPARATE (already AMBER). Untouched by this arm.
- **§8 steady-state mana enhancement:** the future lever that would upgrade the Warren AMBERs to GREEN
  (Treasure accumulation across iterations); tracked, not fudged.
- **P/T-lethality / enters-as-1/0 arm (Night of Souls'):** SEPARATE, out of scope; unblocks combo #2.

---

## 8. Acceptance pins

See `tests/magic-ast-tests/Tests/Interaction/AristocratRecursionScopeTest.cs` — `[Ignore]`-pinned tests
defining the target tiers:
- **Gravecrawler + Pitiless Plunderer + Ashnod's Altar → GREEN** (the mana-positive lead: two mana
  sources cover the {1}{B} recast; the false-positive surface the interaction-judge must clear);
- **Warren Soultrader + Gravecrawler + Blood Artist → AMBER** (mana-neutral/negative: one Treasure/iter
  vs a 2-mana recast → a provable {1} shortfall the §8 balance floors — the honest, un-fudged tier);
- **Gravecrawler + a free sac outlet, recast permission ABSENT → NO cycle** (the negative control,
  guard layer i: graveyard-recursion fires only on a real cast-from-graveyard permission).

They skip (`[Ignore("aristocrat-recursion — pending")]`) until the feature lands. The suite stays
green.
