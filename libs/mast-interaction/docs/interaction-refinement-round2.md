# Interaction-graph refinement — judge round 2 (2026-06-04)

Sampled 3 partial + 3 derived reconstructed cycles from the regenerated corpus (121 surfaced
cycles: 1 verified / 60 partial / 60 derived) and ran one interaction-judge per cycle against real
Oracle text + the CR. Partials were enriched with their CSB **anchor** combo (the nearest combo by
shared-card overlap — full card list, the shared/missing cards, and what the combo *produces*) so the
judge had "what is this a fragment of?" context.

## Verdicts

| Cycle | Cards | Engine | Judge | Root cause |
|---|---|---|---|---|
| P#13 | Ashnod's Altar + Pitiless Plunderer + Chatterfang | Green | **GENUINE-LOOP** ✓ | — (real combo; see anchor note) |
| P#25 | Arcbound Ravager + Pitiless Plunderer | Amber (Types) | **CORRECTLY-HEDGED** ✓ | — (artifact⊄creature; needs Displaced Dinosaurs) |
| P#1  | Ant Queen + Ashnod's Altar | Green | **FALSE-POSITIVE** | **(B) per-colour balance** |
| D#61 | Bog Initiate + Chromatic Star | Green | **FALSE-POSITIVE** | **(A) self-sac one-shot** |
| D#62 | Bog Initiate + Dromar's Attendant | Green | **FALSE-POSITIVE** | **(A) self-sac one-shot** |
| D#111| Barrels of Blasting Jelly + Pitiless Plunderer | Amber (mana-negative) | **FALSE-POSITIVE** (should prune) | **(A) self-sac one-shot** |

Two calibration cases (P#13, P#25) confirm the judges are well-calibrated: they passed a real combo
and a sound Amber hedge.

## Root cause A — self-sacrifice "Sacrifice this" costs are not self-bound (3 of 4 FPs)

Chromatic Star (`{1}, {T}, Sacrifice this: Add any`), Dromar's Attendant (`{1}, Sacrifice this
creature: Add {W}{U}{B}`), and Barrels of Blasting Jelly (`{5}, {T}, Sacrifice this artifact: …`) all
**consume themselves** as an activation cost — they fire **once**. The parser does **not** bind
"Sacrifice this [self]" to `:self` (confirmed: `sac:…:self` never projects in the corpus; Barrels
projected the generic `sac:artifact:controlled`, and Chromatic Star/Dromar's self-sac co-cost is
absent from the cycle's conjunction entirely → `coCostsSatisfied=True`). So a one-shot self-consuming
producer is modelled as an infinitely re-firable source.

This is exactly **B follow-on #1** (deferred from the §8 one-shot-self-removal prune), now motivated by
three concrete false-GREENs. The fix is two parts:
1. **Parser:** self-bind "Sacrifice this [card]" cost fodder → `IsSelf` (so it projects `sac:…:self`).
   Per [[project_mast_keyword_trigger_bypass]] this must patch **both** subject-construction paths.
2. **Engine:** extend `PortGraphEngine.IsOneShotSelfRemoval` to the `sac` role (a `sac:…:self` of a
   non-token permanent is one-shot, same as the `ltb:…:self` death) → prune. The conjunction will also
   independently flag the unfed self-sac co-cost as Amber once the cost is projected.

Secondary (D#61/D#62): both loops are also **net-zero 1-for-1 mana filters** — even with self-sac
fixed, the balance certifies `produced ≥ cost` (= for a filter), and a net-zero do-nothing loop with no
side-trigger isn't a combo. Latent: the engine also treats `{T}` as a **free** cost (no once-per-untap
gate), so a tapped mana rock could false-loop without an untapper — not yet biting (canonical combos
are sac-based) but flagged.

## Root cause B — mana balance is fungible-total, not per-colour (P#1)

Ant Queen (`{2}{G}: create an Insect`) sacrificed to Ashnod's Altar (`Sacrifice a creature: {C}{C}`):
3 mana incl. **green** owed, 2 **colorless** produced — net-negative AND colorless cannot pay `{G}`
(CR 107.4, 105.1). The §8 balance sums mana **fungibly** (the caveat recorded when A landed), so the
green pip is "paid" by colorless. Fix: make `ManaBalanced` per-colour — for each coloured pip, the
producers must be able to supply that colour (`any` counts), not just the total. The colour-aware
**flow** check (`ManaColorFeeds`) already exists; the **balance** must reuse it.

## Anchor-heuristic finding (classification quality)

P#13's loop is the real **Chatterfang × Ashnod × Pitiless** combo, but the partial anchor locked onto
a *different* combo (Wretched Throng / Sundering Archaic / Sunscape Familiar) that merely shares
Ashnod + Pitiless. `MaterializeCyclesStep.Classify` picks the **first** co-occurring pair / smallest
superset; it should pick the combo with **maximal overlap** with the cycle's cards (and ideally report
it's a nearest-match, not ground truth). Cheap fix, improves the viz/enrichment honesty.

## Recommended order
A (self-sac — 3 FPs, de-deferred B follow-on) is highest-impact; B (per-colour balance — 1 FP, the
A-caveat refinement) is contained; the anchor max-overlap is a cheap bundle-along.

## Resolution (2026-06-04)
- **A (self-sac one-shot) — DONE** (commit `4d3235b6`): parser self-binds "Sacrifice this" → `IsSelf`
  (14 golds); `TokenSatisfiesSacAtCreation` refuses to refuel a `:self` sac from a created token. The
  Chromatic Star / Dromar's Attendant / Barrels false-GREENs left the surfaced set; Green 74→39.
  *Newly visible (distinct cause):* net-zero / **tap-gated** mana filters (Bog Initiate + a `{T}` filter)
  remain false-GREEN — the engine treats `{T}` as a free cost (no once-per-untap gate). Follow-on C.
- **B (per-colour balance) — DONE**: `ManaBalanced` now buckets by colour (a coloured pip must be paid
  in its colour or from the `any` pool, CR 107.4). Ant Queen × Ashnod's Altar → Amber (mana-negative);
  canonical combos retained; Green 39→38.
- **Anchor max-overlap — DONE** (commit `c69e62f6`): `Classify` scores every candidate combo by
  card-overlap and anchors a partial to the max-overlap (tightest, then lowest-index) combo. The
  Ashnod × Pitiless × Chatterfang loop now anchors onto {Chatterfang, Pitiless Plunderer} (its real
  core synergy) instead of the unrelated 5-card Wretched Throng combo.
- **C-tap (tap rate-limit) — DONE**: a `{T}` cost now gates firability (CR 107.5; `PortGraph.IsGated`),
  so tap mana-rock / mana-dork loops floor to Amber instead of false-certifying. §9 tokens gated only
  when tap-without-sac (Treasure stays ungated). *Conservative limitation:* self-untapping permanents
  (Blasting Station) and externally-untapped dorks floor to Amber (false-Amber) — untap **renewal**
  isn't modeled (now resolved — see C-untap). Correction: Bog Initiate / Farrelite Priest / Initiates are
  NOT tap abilities (I mis-tagged them) — they are `{1}: Add {one mana}` filters → the net-zero class below.
- **C-untap (untap-renewal carve-out) — DONE**: a tap gate is now *dischargeable* (`PortNode.TapGated` /
  `PortCycle.TapRenewed`): every tap-gated permanent the loop traverses must untap **itself** each
  iteration (`etb:X → emit:untap:self`, fed by a loop-made token triggering `etb:X`). Blasting Station ×
  Pitiless × Chatterfang is GREEN again; Corridor Monitor ("untap **target**") stays Amber. A first judge
  ruled it UNSOUND (the projection dropped the untap target, so target-untap read as self-untap →
  false-GREEN); fixed by projecting `emit:untap:self` only for `ObjectReference.Self` and requiring it in
  renewal; re-judge SOUND. Residual (safe): triggered "untap it" collapses to Self (errs to false-Amber,
  never false-GREEN) — optional parser follow-up.
- **C-netzero (net-zero mana filter) — DONE**: new `PortCycle.Productive` axis — a pure-mana loop must
  net **positive** mana (`ManaProductive`, `produced > cost`); a 1-for-1 filter (Bog/Farrelite/Initiates)
  is a do-nothing → Amber (reason `net-zero filter (no surplus)`). A loop with a non-mana output (token)
  is productive via that output. Judge: SOUND, no counterexample; the inert-emit lenience concern is moot
  (inert emits aren't cycle members). Green 35→32; canonical combos retained.
- **Grinning Ignus (self-bounce recast) — DONE.** "{R}, Return this to hand: Add {C}{C}{R}" read net +2,
  but the return-to-hand cost was **dropped at parse** (no cost rule), so the engine missed the recast.
  Two parts: (1) parser — a new `ReturnToHandCost` + `ReturnSelfToHandCostRule` (13 corpus cards whose
  self-bounce cost was silently dropped, 0 golds; a parser-fidelity fix beyond the graph); (2) engine —
  `IsGated` hard-gates a self-bounce ability (the source leaves the battlefield and must be recast, a
  mana cost the loop doesn't model → can't certify → Amber). Conservative: also floors reanimator
  self-bouncers (Recurring/Chthonian Nightmare) to Amber — the safe direction (recast + its enablers
  unmodeled); modelling the recast cost + cost-reducers is a deeper follow-on.

---

# Round 3 — 3-judge cleanup on the PARTIAL tier (2026-06-04)

Sampled 9 partials across 3 thematic judges (Green / gated+Types / straddle+reason), CSB-anchor
enriched. Findings: the partial tier carried systematic **false-Ambers that should prune** and
**false-GREENs**. (Process note: the auto-briefs paraphrased some cards wrong; the judges re-derived
from the real AST and ruled on that — future briefs must carry real oracle text.)

- **Seat 1 (Green partials):** 3/3 sampled FALSE-GREEN — Ghave's `+1/+1 counter` token-cost dropped;
  Ruthless Knave needs a recursive creature feeder; Greater Good's "activate only as a sorcery" dropped.
  *Caveat:* Ghave / Greater Good / Anointer Priest have **no fixtures**, so these rest on printed text,
  not the engine's actual parse — verify the ports before fixing.
- **Seats 2+3 (Amber):** 5/6 SHOULD-PRUNE, 1 AMBER-CORRECT (#31 One with the Kami "modified-creature"
  predicate — calibration ✓). Two systematic impossible-loop classes, 10 cycles each:
  1. **Treasure-sac → creature-dies** (Lithatog/Extruder/Megatog/… × Pitiless): an artifact token can't
     be a creature, so the dies-trigger never fires.
  2. **Basri's Lieutenant intervening-if** ("if it had a +1/+1 counter"): the loop's counter-less
     Knight tokens fail the gate, so it can't repeat.

## Fixes (engine prunes)
- **Bridge-respects-token-type — DONE** (`BridgeFedByIncompatibleToken`): prunes a cycle whose loop
  sacrifices a created token that can't be the type its bridged dies-trigger requires (a Treasure into
  "a creature dies"). Dual of `TokenSatisfiesAtCreation`; excludes `:self` (the §8-B rule's domain);
  artifact-creature tokens (Construct/Servo) are retained via the creature lift. Judge: SOUND, no
  false-prune. Corpus: Lithatog/Extruder/Megatog/Ravenous Intruder loops gone; "Types" partials 14→5.
- **Basri intervening-if prune — DONE** (`CounterGateUnsatisfiable`): a "had a +1/+1 counter" dies-gate
  (Basri's Lieutenant) makes counter-less tokens, so a loop fed by them never re-fires the gate → prune.
  Done properly: structured the condition (`TriggeringObjectCounterCondition`), projected `RequiresCounter`
  onto the dies-trigger + `putCounters` → `emit:counter`, and the prune carves out a per-iteration counter
  source (a loop-fed `etb:creature → emit:counter`, Cathars'-Crusade-style; the one-time self-ETB counter
  is excluded). Judge: SOUND, no false-prune (the real 3-card Cathars' combo closes through counter-flow,
  which isn't an edge type, so it's never reconstructed — pruning the false 2-card loop costs nothing).
  Corpus: all 10 Basri loops gone; canonical combos intact.
