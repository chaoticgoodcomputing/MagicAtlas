# MAST judge — batch verdict

**Date:** 2026-07-07
**Batch:** batch1-skipdraw (branch `mast-tdd/2026-07-07-skip-draw-step`, base `b77e3912`)
**Scope:** 3 files + 1 projection decision (1 fixture, 2 AST/parser nodes, 1 projection call) — card: Symbiotic Deployment (MH3)
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Replacement/TurnPartEvent.cs#TurnPartEvent` — PASS. Models "skip your draw step" as a typed replaceable turn part per **CR 614.10** ("An effect that causes a player to skip an event, step, phase, or turn is a replacement effect. 'Skip [something]' is the same as 'Instead of doing [something], do nothing.'" — verified verbatim in rules-structure.json). Draw step is kept TYPED: `Part` reuses the shared `TurnPart` enum (`Draw` is a member) and `Whose` uses `ControllerFilter.You` — the same turn-structure vocabulary `GameTime.Whose` already uses (CR 500-series). Discriminator `turnPart` is UNIQUE on the `EventType` base (existing set: generic, untap, mill, damage, destruction, zoneChange, tokenCreation, diceRoll, abilityTrigger, lifeChange, spellCopy, death, drawCard, counterPlacement — no clash). Choosing a new step-skip node over the pre-existing `drawCard` replacement is the more rules-accurate reading: 614.10a says anything scheduled for a skipped step won't happen, i.e. the whole step is skipped, not merely the draw action.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/SkipDrawStepRule.cs#SkipDrawStepRule` — PASS. Anchored regex `^\s*Skip\s+your\s+draw\s+step\.?\s*$` emits a `StaticAbility` → `ReplacementEffect` with `OriginalEventOccurs = false` and no `Replacement`, which is exactly CR 614.10's "instead do nothing". Correctly a static replacement (`Kind: static`), not activated/triggered. Anchoring prevents swallowing a sibling clause that merely contains the phrase.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/MH3/SymbioticDeployment.json` — PASS. `Input.OracleText` matches oracle-cards.json exactly ("Skip your draw step.\n{1}, Tap two untapped creatures you control: Draw a card."). Line 1 gold = typed `turnPart` replacement (`Part: Draw`, `Whose: You`, `OriginalEventOccurs: false`). Line 2 fully modeled and unchanged — activated ability: `{1}` mana + `tapPermanents` (2 creatures you control) cost + `drawCards` effect; no dropped sibling. No `unparsed` / `UnparsedEffect` / `Diagnostics` anywhere; the only free-text fields (`OracleText`, `RawText`, TypeLine `Raw`, manaCost `Raw`) are verbatim-by-design. Serialization cross-checks: `EventType: turnPart` (attribute), `Part: Draw` and `Whose: You` (JsonStringEnumConverter member names) all match the node.
- `projection:turnPart` — PASS. The PortWalk exhaustiveness ratchet gates exactly four dispatch dimensions — `effectType`, `costType`, `triggerEvent`, `restriction` (`PortWalkExhaustivenessTests.Dimensions()`). `turnPart` lives on the `EventType` (`ReplacementEvent`) base, which is NOT one of them, so no `known-coarse-projections.json` entry is required and none is missing. The branch's inert call is sensible: skipping one's own draw step emits no consumable resource and is a self-disadvantage no flow rule reads, so it forms no interaction edge. The branch introduces no NEW gated discriminator (`replacement`, `drawCards`, `mana`, `tapPermanents` are all pre-existing), so the ratchet is not tripped.

## Glossary gaps

None. "Draw step" / "skip" are covered by CR 500.1, 504, 614.10.

## Process notes

- CR citations cross-referenced against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`: **614.10** present and text matches verbatim; **500.10** present (turn-structure context — skipped steps in added phases). Both citations are accurate and support the modeling.
- Minor, non-blocking: line 2's `tapPermanents` cost filter does not carry an explicit "untapped" flag, but a tap cost inherently requires untapped permanents, so "untapped" is templating-redundant; this is a pre-existing cost convention untouched by this branch.
- The base `ReplacementEvent.Controller` (ObjectReference?) and the node's `Whose` (ControllerFilter?) are not a duplication problem — `Whose` mirrors the established `GameTime.Whose: ControllerFilter?` turn-structure idiom, keeping "your" typed rather than free text.

ALL PASS
