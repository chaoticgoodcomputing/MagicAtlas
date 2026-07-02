# MAST judge — batch verdict (sylvan-messenger)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-sylvan-messenger
**Scope:** 3 targets (1 fixture, 1 AST effect node, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LRW/SylvanMessenger.json` — PASS. The ETB "reveal top four, put Elf cards to hand, rest to bottom in any order" is modeled as a `triggered` ability whose `Trigger` carries the timing (`When` / `Enters` / self-creature filter) and whose single `Effects[]` entry is `revealTopPutMatchingToHand{ Player: You, Count: literal 4, Filter: Subtypes[Elf] }`. Timing is composited, not baked into the effect. The Trample sibling (`static` keywordAbility + verbatim reminder) and all Attributes (manaCost/colors/colorIdentity/creatureStats) are preserved. Oracle text matches oracle-cards.json verbatim. No `unparsed` node, no rules-bearing free-text. Both cited rules verified: CR 701.20 (Reveal; 701.20a "show that card to all players for a brief time") and CR 401.4 (any-order remainder placed on the bottom of the library).
- `libs/magic-ast/AST/Effects/CardFlow/RevealTopPutMatchingToHandEffect.cs` — PASS. New discriminator `revealTopPutMatchingToHand` names the game action, not the firing context (no `OnEntry`/`WhenEnters` timing swallow). Structured `Player`/`Count`/`Filter` fields; the two-sentence oracle is treated as one coupled action (back-references "revealed this way"/"the rest") rather than decomposed. The "bottom in any order" disposition is folded into the discriminator's meaning following the `AbundanceRevealEffect` minimal-fields precedent — a modeling choice, not a free-text/escape-hatch. Cited CR 701.20 / CR 401.4 exist and match.
- `libs/mast-interaction/known-coarse-projections.json#revealTopPutMatchingToHand` — PASS (projection decision, initiative 03). The new discriminator ratchets a coarse projection with a plausible justification. Coarse is sensible: this is a library-visibility / card-selection-to-hand action, not a mana/untap/damage/token combo-loop primitive, and no flow rule reads it. Consistent with the sibling reveal/look family (revealUntil, oracleTopLook, impulse, abundanceReveal) all parked coarse. Nothing a flow rule would clearly want is being parked.

## Glossary gaps

None.

## Process notes

- CR citations cross-referenced against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`: CR 701.20 and CR 401.4 both present and textually consistent with the modeling.
- Fixture is a new file (120 insertions, no deletions) — no prior gold to regress; Trample sibling correctly retained alongside the new ETB ability.
- The parser rule file (`.../Triggered/Rules/RevealTopPutMatchingToHandTriggeredRule.cs`) is out of judge scope (parser correctness is NUnit's job); its doc-comment CR cites were nonetheless confirmed consistent.

**PROCEED** — 0 FAIL.
