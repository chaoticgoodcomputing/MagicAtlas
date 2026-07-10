# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/target-opponent-exiles-greatest-mv
**Family:** target-opponent-exiles-a-creature-or-planeswalker-greatest-mv (fragment on Blot Out)
**Scope:** 6 targets (1 fixture, 2 new AST nodes, 2 shared edits, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 6
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SNC/BlotOut.json` — PASS. Input.OracleText byte-identical to Scryfall oracle-cards.json. The clause is an edict, not targeted removal: only the opponent is targeted (CR 115.1), the exiled permanent is chosen by that opponent (CR 701.13a). Gold captures this correctly — `Player` = {Kind:Target, CardTypes:["opponent"]} (the established target-opponent convention, same as Thought Erasure's discarder axis); the exiled object is `Kind:Any` (NOT Target — opponent chooses) with `Controller:Target` = "a creature or planeswalker they control"; "greatest mana value" is a structured `extremeStat` (Stat=ManaValue, Extreme=Greatest, Scope null since the "among" population equals the enclosing filter). No IUnparsed, no UnstructuredEffect, no free-text characteristic, no lossy drop/merge.
- `libs/magic-ast/AST/References/ExtremeStatCharacteristic.cs` — PASS. Structured superlative predicate replacing the free-text greatest-value carve-out the whitelist named as missing. Descriptive (records "this object is the max/min of Stat over the population"), not executive. Cites CR 202.3 (mana value) and CR 208 (power/toughness) — both exist and match; ties-to-choosing-player noted as a resolution concern, correctly not baked into the predicate.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/TargetOpponentExilesGreatestManaValuePermanentRule.cs` — PASS. Fully anchored `^...$` whole-clause regex that cannot substring-capture the shorter sibling edicts. Emits a sound edict ExileEffect with the two-axis actor/object shape. Cited CR 115.1 / 202.3 / 701.13a all verified present and matching; the 701.13a quote is verbatim from the rules data.
- `libs/magic-ast/AST/Effects/ZoneChange/ExileEffect.cs#Player` — PASS. Sound additive generalization: an optional `[JsonIgnore WhenWritingNull] ObjectReference? Player` edict-actor axis paralleling DiscardCardsEffect.Player and SacrificeEffect. Existing exile fixtures (you-exile-Target) serialize unchanged; Target stays the exiled object rather than being overloaded. CR 701.13a + CR 115.1 both check out.
- `libs/magic-ast/schema/ast-schema.json` — PASS. Mechanical regen adding ExtremeStatCharacteristic (discriminator `extremeStat` under the `CharacteristicType` base, fields Extreme/Stat) plus updated SchemaHash. Consistent with the new node.
- `projection:mast/target-opponent-exiles-greatest-mv` — PASS. The branch introduces one new discriminator, `extremeStat`, but it is a `CharacteristicType` (an ObjectFilter predicate), which is OUTSIDE the four dimensions the PortWalk exhaustiveness ratchet enforces (effectType / costType / triggerEvent / restriction) — filter characteristics produce no port, so no projection decision is required or missing. The `ExileEffect.Player` addition is a new field on the pre-existing `exile` effectType, not a new discriminator; `exile` remains validly coarse-whitelisted in known-coarse-projections.json (unchanged by this branch). No insensible coarse choice was made here.

## Glossary gaps

(none — "exile", "mana value", "target", "opponent" all covered by CR + glossary)

## Process notes

- CR citations cross-referenced against rules-structure.json: 115.1 (targets), 202.3 (mana value), 701.13a ("To exile an object, move it to the exile zone from wherever it is" — verbatim match), 208.1 (power/toughness). All present and non-contradictory.
- The `CardTypes:["opponent"]` player encoding is an established codebase convention (Thought Erasure, Cinder Hellion, Zealot of the God-Pharaoh), not new drift introduced here — out of scope to re-litigate.
- Broader pre-existing gap (not this branch's regression): `exile` as an effectType is coarse across the board; an exile-removal semantic projection would benefit interaction recall, but that decision predates this branch and is tracked by the known-coarse whitelist.

ALL PASS
