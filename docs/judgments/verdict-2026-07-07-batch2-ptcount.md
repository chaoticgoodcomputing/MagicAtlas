# MAST judge — batch verdict

**Date:** 2026-07-07
**Batch:** batch2-ptcount (`mast-tdd/2026-07-07-pt-per-count`, base `02bae0fd`)
**Scope:** 2 surfaces (1 shared-helper parser branch, 1 gold fixture)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

Claim closed: "P/T bonus scaled by a count" — Salvage Slasher, "This creature gets +1/+0 for each artifact card in your graveyard." No new discriminator introduced (all nodes reused, matching the Rat Colony gold shape) → no initiative-03 projection decision required.

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/StaticRuleHelpers.cs` (new `BuildObjectCountFilter` "your graveyard" branch) — PASS. No sibling-mislabel risk; Controller=You + Zone=Graveyard is consistent with the ObjectFilter/CountOf family convention; citations valid.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/DSK/SalvageSlasher.json` — PASS. Typed `modifyPT` with a `count` power modifier + literal-0 toughness, mirroring Rat Colony; no unparsed/free-text/diagnostics.

## Detailed rationale

### Shared-helper sibling-mislabel risk (the core ask)

The new branch: `^(?<type>.+?)\s+cards?\s+in\s+your\s+graveyard$` (IgnoreCase), placed AFTER the "all graveyards" branch and BEFORE the "you control" board-count branch.

- **Disjoint from every sibling.** The pattern is fully `^...$`-anchored, so it only fires on a phrase ending exactly in "card(s) in your graveyard". The "all graveyards" sibling ends in "in all graveyards" (different literal tail → no overlap in either direction, regardless of ordering). The "you control" sibling ends in "you control"; "attached to it" ends in "attached to it". None of these can be captured by, nor can they capture, the new branch. No sibling's counted-set phrase is stolen or mislabeled.
- **Head-noun handling is correct.** The `cards?` token consumes the "card"/"cards" head noun, so only the leading type noun (e.g. "artifact") is passed to `ClassifyTypeNounPhrase`. Feeding the whole "artifact cards" would wrongly classify "cards" as a subtype; this branch avoids that, identically to the established "all graveyards" branch.
- **Untyped phrases fall through honestly.** A bare "cards in your graveyard" / "card in your graveyard" (no leading type) cannot match: `.+?` requires ≥1 char plus a `\s+` before the only `card(s)` token, and there is nothing before it → `Match` fails → the method returns `null` (honest fallback). So a generic "each card in your graveyard" is NOT wrongly typed; it is simply left unhandled. Good.
- **Concrete captures are correct.** "artifact cards in your graveyard" → `ClassifyTypeNounPhrase("artifact")` → `CardTypes:["artifact"]`, then `with { Zone=Graveyard, Controller=You }`. "creature cards in your graveyard" → `CardTypes:["creature"]`, same scope. Subtype heads (e.g. "Zombie cards in your graveyard") → `Subtypes:["Zombie"]`. All correctly scoped.

### Controller vs Owner for "your graveyard" — consistent with family convention

`ObjectFilter` carries BOTH a `Controller` (CR 109.4) and an `Owner` (CR 108.3) axis, and its own doc-comment notes a graveyard card is owned, not controlled. `CardTypeDiversityCondition` models "your graveyard" via `Owner`. Strictly per CR, cards in a graveyard have owners, not controllers, so `Owner=You` would be the more precise axis. HOWEVER, the ObjectFilter/CountOf family's two direct precedents for "your graveyard" counts — Cryptic Serpent and Ghoultree (both cost-reduction counts) — use `Controller:You` + `Zone:Graveyard`. Salvage Slasher matches that established family convention exactly. FAILing it would require it to deviate from its own precedents. This is a cross-family Controller-vs-Owner inconsistency worth reconciling in the engine-lens audit, NOT a per-fixture FAIL.

### Layer / CR citation check

- **CR 604.3** exists and reads "Some static abilities are characteristic-defining abilities…" — present and matching.
- **CR 613.4c** exists: "Layer 7c: Effects and counters that modify power and/or toughness (but don't set power and/or toughness to a specific number or value) are applied." — this is the correct, load-bearing citation for a "gets +N/+0" modifier.
- **Imprecision (process note, not a FAIL):** the helper comment calls Salvage Slasher's bonus a "characteristic-defining P/T bonus (CR 604.3)". A "gets +1/+0" is a layer-7c *modifier*, not a CDA — it does not SET base P/T and does not function outside the battlefield, so it fails CR 604.3a's criteria. (Contrast the sibling "all graveyards" branch, whose card Terravore genuinely IS a CDA because it SETS P/T — the "characteristic-defining" framing was accurate there and was copied onto Salvage Slasher where it is not.) The operative modeling is `modifyPT` (a modifier) and the layer cite (613.4c) is exactly right, so the 604.3 characterization is contextual/imprecise, not contradictory to the modeled node. Recommend rewording the comment to "layer-7c P/T-modifying static ability (CR 613.4c)" and dropping the CDA framing.

### "+1/+0" is typed, not free text

Power scales via `PowerModifier: { QuantityType: "count", CountOf: {...} }`; toughness is `{ QuantityType: "literal", Value: 0 }`. Neither half is a free-text string. Correct.

### Unparsed / describe-vs-execute / dropped sibling

The gold has no `"Kind":"unparsed"`, no `"EffectType":"unparsed"`/`UnparsedEffect`, no `Diagnostics`. The card has a single ability, fully modeled; no sibling ability dropped.

## Glossary gaps

None. ("characteristic-defining ability" = CR 604.3; power/toughness modification = layer 7c per CR 613.4c; both grounded.)

## Process notes

1. Controller-vs-Owner for owned zones (graveyard/hand/library) is modeled two ways across families — `Owner` in `CardTypeDiversityCondition`, `Controller` in the `ObjectFilter`/`CountOf` "your graveyard" counts (Cryptic Serpent, Ghoultree, now Salvage Slasher). Not blocking this batch, but a reconciliation candidate for the engine-lens audit.
2. Helper comment mislabels a layer-7c "gets +N/+0" modifier as "characteristic-defining"; harmless to the modeled node (613.4c is correct) but worth a comment-only tidy.

**Result: ALL PASS.**
