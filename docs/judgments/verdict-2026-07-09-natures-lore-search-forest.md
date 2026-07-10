# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Batch:** natures-lore-search-forest
**Branch:** mast/natures-lore-search-forest (base b1c7f836)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TMP/NaturesLore.json` — PASS. `Input.OracleText` is byte-identical to Scryfall oracle-cards.json ("Search your library for a Forest card, put that card onto the battlefield, then shuffle."); mana cost `{1}{G}`, type Sorcery, colors/identity `[G]` all correct. Gold is a single structured `searchLibrary` effect: `Filter{CardTypes:[land], Subtypes:[Forest]}`, `Count` literal 1, `Destination: Battlefield`, `Revealed: false`. Correctly filters on the Forest land subtype without asserting a `Basic` supertype (CR 305.6 — a "Forest card" is any card with the Forest subtype, so dual/snow lands qualify). No `IUnparsed`, no `UnstructuredEffect`, no free text.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/SearchLibraryLandTypeToBattlefieldUntappedRule.cs` — PASS. Cited CR 305.6 (basic land types) and CR 701.23 (search action) both exist in `rules-structure.json` and their text matches what the node models. Sound untapped twin of `SearchLibraryLandTypeToBattlefieldRule`: the regex anchors on `battlefield, then shuffle` (no "tapped" token), making it mutually exclusive with the tapped sibling; emits `SearchDestination.Battlefield`. "then shuffle" is consumed by the regex, not left unparsed.
- `...SearchLibraryLandTypeToBattlefieldUntappedRule.cs#projection` — PASS. No new discriminator (worker: `newAstNode=false`, `shared=[]`). Reuses the pre-existing `searchLibrary` effect type and the pre-existing `SearchDestination.Battlefield` enum value; the `SearchDestination.cs` file is untouched by this branch. Nothing new for the PortWalk projection ratchet to enforce.

## Glossary gaps

(none) — Forest, Basic Land Type, Library, Shuffle are all present in glossary.json.

## Process notes

- **Implicit-shuffle convention split (pre-existing, not a FAIL for this branch).** The corpus is inconsistent about "then shuffle": spell-family ramp golds `RampantGrowth` and `Cultivate` fold the shuffle into the bare `searchLibrary` effect (no explicit `shuffle` node), whereas land-activated multi-type golds `DeceptiveLandscape` / `ContaminatedLandscape` wrap the search in a `composite` with an explicit `shuffle` effect. The new Nature's Lore gold follows the directly-analogous spell-ramp precedent (RampantGrowth/Cultivate) and the emitting rule mirrors the tapped sibling. This is consistent and trustworthy for the family; flagged only so the orchestrator can consider normalizing the corpus's shuffle-modeling convention corpus-wide.
- The doc-comment's aside that "a" is the "absent count" case is a slight mischaracterization of the regex (the `count` group's `[a-z]+` captures "a" for "a Forest card"), but the resulting gold `Count` = literal 1 is semantically correct and parser mechanics are out of judge scope (NUnit's job).
