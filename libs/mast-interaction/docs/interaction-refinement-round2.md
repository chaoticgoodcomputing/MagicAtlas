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
- **C (tap rate-limit / net-zero filter)** — new follow-on surfaced by A; not yet scoped. The engine
  treats `{T}` as a free cost (no once-per-untap gate) and certifies a net-zero 1-for-1 mana filter as
  infinite, so a tap-gated filter pair (Bog Initiate + Farrelite Priest) is still false-GREEN. Needs a
  tap rate-limit gate (a `{T}` ability can't re-fire within a turn without an untapper) and/or a
  net-positivity requirement (a filter that converts N→N produces no surplus).
