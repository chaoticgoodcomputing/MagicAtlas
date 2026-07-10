# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast-tdd-each-player-chooses-any-number
**Family:** each-player-chooses-any-number-o (Destined Confrontation)
**Scope:** 5 targets (1 fixture, 1 AST effect node, 1 spell rule, 1 schema edit, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DestinedConfrontation.json` — PASS. Input.OracleText is byte-identical to the real card "Destined Confrontation" (verified in oracle-cards.json; {2}{W}{W}, Sorcery, W, ManaValue 4 — a distinct card from Slaughter the Strong {1}{W}{W} with the same text). Gold is a single `spell` ability with one `sacrificeAllButChosen` effect: `Target = {Kind: EachPlayer, Filter: {CardTypes:[creature]}}`, `KeepTotalPower = {Operator: LessThanOrEqual, Value: 4}`. No `unparsed`/`UnstructuredEffect`/`IUnparsed`, no lossy drop or merge; semantics faithful to "each player chooses any number of creatures they control with total power 4 or less, then sacrifices all other creatures they control."
- `libs/magic-ast/AST/Effects/ZoneChange/SacrificeAllButChosenEffect.cs` — PASS. Fused choose-keep-then-sacrifice-the-complement effect. `Target` (ObjectReference, mirrors SacrificeEffect.Target) + `KeepTotalPower` (a `Comparison` aggregate cap on the SUM of the chosen set, correctly distinguished from a per-object `ObjectFilter.PowerComparison`). "any number" -> no count cap recorded, correct. Fusion is justified: "all other" is defined relative to the just-chosen kept set, so it cannot decompose into standalone choose + sacrifice without a not-chosen filter axis. Cited CR 701.21a exists in rules-structure.json and its text ("its controller moves it from the battlefield directly to its owner's graveyard"; can't sacrifice a permanent you don't control) matches the modeling.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/EachChoosesKeepByTotalPowerSacrificesRestRule.cs` — PASS. Backreferenced-type regex (`\k<type>`) requires both "creatures … all other creatures" occurrences to name the same type; emits one SacrificeAllButChosenEffect. scope->EachPlayer/EachOpponent, type->CardTypes, "or less/fewer"->LessThanOrEqual, "or greater/more"->GreaterThanOrEqual all sound. Cites CR 701.21a correctly.
- `libs/magic-ast/schema/ast-schema.json` — PASS. Adds the SacrificeAllButChosenEffect entry (discriminator `sacrificeAllButChosen`, `IsUnparsed: false`, Fields `KeepTotalPower` + `Target`) exactly matching the node's required fields; SchemaHash regenerated. Sound generalization.
- `libs/mast-interaction/known-coarse-projections.json#sacrificeAllButChosen` (projection decision, initiative 03) — PASS. New discriminator, projection decision present. Coarse choice is sensible: the effect-side `sacrifice` and `destroy` discriminators are both coarse ("no flow rule consumes it yet"); the only semantic sacrifice projection is on the COST side (`sac:<fodder>:controlled`, Chatterfang-style), which does not apply to a spell EFFECT that makes each player sacrifice their own creatures. Parking a mass-sacrifice effect alongside its coarse parent `sacrifice` is consistent convention, not a clearly-wanted flow signal parked as coarse.

## Glossary gaps

None. "Sacrifice" (CR 701.21) is standard; the composite "choose any number … then sacrifice all other" has no single CR keyword, so the descriptive discriminator `sacrificeAllButChosen` (cited to 701.21a) is appropriate.

## Process notes

- Card-identity check: the worker docstrings label the card "Slaughter the Strong / Destined Confrontation". Both are real cards with identical oracle text; the fixture correctly uses the Destined Confrontation printing (mana cost {2}{W}{W}, verified against oracle-cards.json). No confusion in the gold data.
- All referenced AST types verified to exist: `ObjectReferenceKind.{EachPlayer,EachOpponent}`, `Comparison{Operator, Value}`, `ComparisonOperator.{LessThanOrEqual,GreaterThanOrEqual}`, `ObjectFilter.PowerComparison`.

ALL PASS
