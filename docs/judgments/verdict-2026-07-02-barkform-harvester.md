# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** barkform-harvester
**Branch:** mast-tdd/2026-07-02-barkform-harvester
**Base:** 90209551d6e502036241c8011610cec68dd48ef0
**Scope:** 1 fixture + 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/BLB/BarkformHarvester.json` — PASS. Oracle text verified against oracle-cards.json (exact match). The `{2}` activated ability is modeled as a `mana` cost (one generic, amount 2) plus a `putOnBottomOfLibrary` effect targeting a filter `CardTypes:["card"], Zone:Graveyard, Controller:You`, faithfully matching "Put target card from your graveyard on the bottom of your library." The filter shape matches the established corpus convention (cf. C19/PurifyTheGrave.json "target card from a graveyard" → `CardTypes:["card"], Zone:Graveyard`), with `Controller:You` correctly added for "your graveyard." The effect names only the action (destination = library bottom); the activated-ability wrapper carries the cost — no timing baked into the effect, describe-not-execute. Changeling and Reach keyword-ability siblings, TypeLine, and all Attributes are preserved unchanged. No `unparsed` node and no rules-bearing free-text residual (the `Raw`/`RawText` fields are verbatim-by-design). Cited rules all exist in rules-structure.json and match the modeling: CR 400.7 (moved object becomes new object), CR 401.2/401.4/401.7 (library ordering), CR 602.1 (activated ability "[Cost]: [Effect]" form). CR 701.x correctly noted as having no "put on the bottom" keyword action.

- `libs/mast-interaction/known-coarse-projections.json#putOnBottomOfLibrary` — PASS. The branch introduces a new effect discriminator (`putOnBottomOfLibrary`) and registers a projection decision: a justified `known-coarse-projections.json` entry rather than a semantic PortGraph case. The reason ("graveyard-to-library-bottom recursion-denial effect; no interaction flow rule reads library-bottom insertions yet; consciously inert for interaction recall; same coarse precedent as sibling putOnTopOfLibrary") is sensible. This is a value/denial effect, not something a combo-flow rule would clearly want to traverse, and it matches the established coarse treatment of its siblings (putOnTopOfLibrary, putIntoLibraryAtPosition). Presence enforced by the ratchet; sensibility confirmed here.

## Glossary gaps

(none)

## Process notes

New AST node `libs/magic-ast/AST/Effects/ZoneChange/PutOnBottomOfLibraryEffect.cs` carries a single `Target` field and a doc-comment citing CR 400.7 / 401 — cross-referenced and accurate; distinguished from PutOnTopOfLibraryEffect and PutIntoLibraryAtPositionEffect. Schema (ast-schema.json) and discriminator-baseline.json updated consistently to register the new discriminator. Parser rule `PutTargetGraveyardCardOnBottomRule` is out of judge scope (parser correctness is NUnit's job) but its doc-comment citations (CR 602.1, CR 400.7, CR 401) were spot-checked and are accurate.
