# MAST judge — batch verdict

**Date:** 2026-05-26
**Batch:** 19 (Flanking + Kicker single-cost)
**Scope:** 8 files (6 fixtures, 2 AST nodes)
**Result:** PASS

## Summary

- PASS: 8
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

### AST nodes

- `libs/magic-ast/AST/Effects/Keyword/FlankingEffect.cs` — PASS. Doc-comment cites Rule 702.25 correctly; parameterless shape matches the keyword's structural surface (the "blocking creature gets -1/-1" expansion in 702.25a is engine territory, per `feedback_mast_describes_not_executes`). Discriminator `"flanking"` matches glossary term word-for-word. Mirrors the established ExaltedEffect/ProwessEffect marker pattern.

- `libs/magic-ast/AST/Effects/Keyword/KickerEffect.cs` — PASS. Doc-comment cites Rule 702.33 correctly (not 702.32, which is Fading). `Cost: Cost` (base type, typically `ManaCost`) faithfully captures Rule 702.33a's `[cost]` parameter. Scope restriction to single-cost kicker is explicitly documented; multi-cost (702.33b) and Multikicker (702.33c) properly deferred. The "if it was kicked" linked-ability expansion (702.33e) is correctly omitted as engine territory, mirroring EquipEffect/CyclingEffect/BestowEffect/EchoEffect.

### Flanking fixtures

- `tests/magic-ast-tests/Data/HandParsedCards/MIR/BurningShieldAskari.json` — PASS. `StaticAbility { KeywordSource: "Flanking", Effects: [{ EffectType: "flanking" }], Reminder: {...} }` shape matches Rule 702.25a. No unparsed nodes. Reminder text preserved verbatim.

- `tests/magic-ast-tests/Data/HandParsedCards/MIR/BenalishCavalry.json` — PASS. Identical shape to Burning Shield Askari; Rule 702.25a represented descriptively.

- `tests/magic-ast-tests/Data/HandParsedCards/MIR/MtendaHerder.json` — PASS. Identical shape; Rule 702.25a represented descriptively.

### Kicker fixtures

- `tests/magic-ast-tests/Data/HandParsedCards/ODY/KrosanDruid.json` — PASS. Kicker {4}{G} encoded as `StaticAbility { KeywordSource: "Kicker", Effects: [{ EffectType: "kicker", Cost: ManaCost{...} }] }` per Rule 702.33a. Sibling ETB triggered ability carries `InterveningIf: { Text: "it was kicked" }` per Rule 603.4 (intervening-if at trigger), consistent with the established free-text-Condition convention used by Mana Vault and Basri's Lieutenant. Scope restriction honored (single mana cost, no and/or, no Multikicker).

- `tests/magic-ast-tests/Data/HandParsedCards/INV/TolarianEmissary.json` — PASS. Kicker {1}{W} per Rule 702.33a. Flying sibling correctly uses existing `EvasionEffect` with `KeywordSource: "Flying"` and the standard `CanBeBlockedBy` filter (creatures with flying or reach) per Rule 702.9b — no doctrinal drift. ETB triggered ability with `InterveningIf` matches the Krosan Druid pattern. Scope restriction honored.

- `tests/magic-ast-tests/Data/HandParsedCards/DMU/JuniperOrderRootweaver.json` — PASS. Kicker {G} per Rule 702.33a. ETB triggered `putCounters` effect with `Target: { Kind: "Target", Filter: { CardTypes: ["creature"], Controller: "You" } }` correctly captures "target creature you control" per Rule 115. InterveningIf consistent with siblings. Scope restriction honored.

## Glossary gaps

- **Kicker** — referenced in 3 fixtures and the new KickerEffect doc-comment. The Comprehensive Rules term is fully defined at 702.33, but `glossary.json` returns `null` for `.terms.Kicker`. This is a corpus indexing gap in the parsed glossary, not a fixture defect — the rule itself exists in `rules-structure.json`. Worth surfacing to glossary regen for triage. Flanking is present in glossary.

## Process notes

- Rule citation discipline: KickerEffect's doc-comment correctly cites 702.33 (Kicker). The briefing flagged 702.32 as a near-miss (Fading) — confirmed against `rules-structure.json` that 702.32 is Fading and 702.33 is Kicker. No off-by-one.
- The `InterveningIf: { Text: "..." }` free-text shape used in all three Kicker fixtures is the established convention (Condition? carries a text payload across all current fixtures). This is a known engine-lens audit item, not batch-19 drift — see `docs/ast-engine-lens-audit.md` for the broader IfYouDoNot / intervening-if structuring discussion. PASS at the batch-19 scope.
- Scope restrictions for Family B verified: no fixture text contains "and/or", "multikicker", or two kicker stanzas. Substitutions (Krosan Druid, Tolarian Emissary, Juniper Order Rootweaver) all carry clean "if it was kicked" ETB triggers — no "if this spell was kicked" *spell-ability* siblings that would require a different pattern.
- 532/532 NUnit green per the briefing.
