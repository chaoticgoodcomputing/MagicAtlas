# MAST judge — batch 6 RE-JUDGE (remediation verify)

**Date:** 2026-07-10
**Branch/HEAD:** feat/loop-trial @ 71dd2f17 (baseSha 889ad7e1)
**Scope:** 4 remediated families (K02, K07, K09, K14) — the 4 original FAILs
**Result:** ALL PASS (4/4)

## Summary
- PASS: 4
- FAIL: 0

## Per-family verdicts

### K02 — Wildfire Howl (Gift) — PASS
Original FAIL: gift promise's `ChoosePlayerEffect` was unscoped, but CR 702.174a requires "you may choose an **opponent**."
`GiftKeyword.cs` now builds `new ChoosePlayerEffect { Scope = ControllerFilter.Opponent }`; gold's `optional -> choosePlayer` now carries `"Scope":"Opponent"`. CR 702.174a confirmed verbatim ("...you may choose an opponent"); CR 702.174e confirms the "Gift a card" payoff ("The chosen player draws a card") which the `gift -> drawCards` by `ChosenPlayer` models. `Scope` is K03's merged nullable `ControllerFilter?` field (`JsonIgnore` when null, so unrestricted "choose a player" printings like Sawhorn Nemesis stay null) — the reuse does not affect other cards. Minor prose nit: the doc-comment clause "carries the declaration only" reads slightly stale next to the now-set `Scope`, but code + gold + citation are all correct — not a FAIL.

### K07 — Dragonologist (citations) — PASS
(a) The fabricated "CR 701.19a: to look at cards" quote is gone (701.19 = **Regenerate**, confirmed — the quote was invented). Replaced with "looking is a general instruction, not a CR 701 keyword action." The kept cites verify: CR 701.20a = "To reveal a card, show that card to all players" (Reveal — correct); CR 401.4 = the multi-card-into-a-library-position placement rule (governs the rest-to-bottom disposition; the oracle's "in a random order" is the override) — applicable and non-contradictory.
(b) Tap-state citation 701.21 -> 701.26: 701.21 = **Sacrifice** (old cite was wrong), 701.26 = **Tap and Untap** (correct). Zero residual 701.19/701.21 across all three files (grep-confirmed).

### K09 — Éowyn, Lady of Rohan (citation) — PASS
`EquipAbilityYouActivateCostReductionRule.cs` citation 118.9 -> 118.7 for the "{1} less" reduction. CR 118.7 = "What a player actually needs to do to pay a cost may be changed or **reduced** by effects" (correct for cost reduction). CR 118.9 = alternative costs ("You may [action] rather than pay [this object's] mana cost") — so the doc-comment's parenthetical "118.9 governs alternative costs, not reduction" is accurate. Equip = 702.6 also cited correctly.

### K14 — Serendib Djinn (REAL + citation) — PASS
(a) REAL: `SacrificeTriggeredRule` now maps "it" -> `ObjectReference.It()` and "this creature"/"this permanent" -> `ObjectReference.Self()`. SerendibDjinn 3rd ability ("When you control no lands, sacrifice this creature.") gold sacrifice `Target` is now `{"Kind":"Self"}`.
**BACK-PROP CHECK — CLEAN:** all three existing "sacrifice this creature" golds retain `Target:{"Kind":"Self"}` at HEAD and are NOT broken by the shared-rule change:
- `OTJ/LonghornFirebeast.json` — "...If a player does, sacrifice this creature." -> Self
- `CON/WildLeotau.json` — "...sacrifice this creature unless you pay {G}." -> Self
- `NEM/WhipstitchedZombie.json` — "...sacrifice this creature unless you pay {B}." -> Self

"sacrifice it" golds still map to It (PhantasmalBear, KariZevSkyshipRaider, BalduvianHorde, FallowWurm, FrostWalker, KikiJikiMirrorBreaker, ...). The change aligns the parser with the pre-existing Self golds rather than breaking them.
(b) citation: `ControlNoLandsConditionRule.cs` 603.2e -> 603.8. CR 603.8 = **state triggers**, whose example is literally "a player controlling no permanents of a particular card type" — an exact fit for "When you control no lands." CR 603.2e = the "becomes"-transition trigger (correctly NOT used).

## Process notes
- No new discriminators are introduced by the remediation (K14 refines an existing `SacrificeEffect.Target` reference; `ControlNoLandsConditionRule` reuses `TriggerEvent.ControlNoLandType`); no PortWalk projection decision is implicated by these fixes.
- Gate: `tools/gate-judge-verdict.sh` exits 0 on the JSON verdict.

**ALL PASS**
