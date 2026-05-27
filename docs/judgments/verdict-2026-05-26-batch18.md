# MAST judge — batch 18 verdict

**Date:** 2026-05-26
**Scope:** 14 items (11 fixtures, 3 parser surfaces: `ExileTargetSimpleRule.cs`, `UntapTargetRule.cs`, `KeywordDefinitions.Indestructible`)
**Result:** PASS

## Summary

- PASS: 14
- FAIL: 0

## Rule-citation check (against `rules-structure.json`)

The briefing prose cited "701.10 Exile" and "701.20 Untap", but those rule numbers belong to **Double** and **Reveal** respectively in the Comprehensive Rules. The actual implementation does not propagate those briefing errors:

- `ExileTargetSimpleRule.cs` doc-comment cites **701.13** — correct (`701.13 Exile`, glossary entry confirms: "See rule 406, 'Exile.' / 701.13").
- `UntapTargetRule.cs` doc-comment carries no rule number — acceptable (the AST `UntapEffect` is not annotated with a rule number; the descriptive doc-comment "Untap target [filter]" is sufficient). For completeness, the correct citation is **701.26 Tap and Untap** (`701.26b`: "To untap a permanent, rotate it from a sideways position to the upright position").
- `KeywordDefinitions.Indestructible` carries `RuleReference = "702.12"` — correct (`702.12a–c` describes Indestructible as a static ability that prevents destruction).

No stale citations leaked from the briefing into production source. Recall batch 17's 509.1d-vs-508.1c FAIL: this batch avoids that class of error.

## PASS verdicts

### Family A — ExileTargetSimpleRule + 5 fixtures (Rule 701.13)

- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ExileTargetSimpleRule.cs` — PASS. Cites 701.13 correctly. SimplePattern explicitly excludes `land` for `ExileTargetLandRule` coexistence; alphabetical dispatch ordering at equal priority is documented inline. Helper rename to `ParseTargetFilter` is generic and shared cleanly across destroy + exile paths.
- `tests/magic-ast-tests/Data/HandParsedCards/MOM/FinalDeath.json` — PASS. `exile` effect with `Target.Filter.CardTypes = ["creature"]`. Descriptive only; no zone-move sequencing.
- `tests/magic-ast-tests/Data/HandParsedCards/ULG/Erase.json` — PASS. `exile` effect, `CardTypes = ["enchantment"]`.
- `tests/magic-ast-tests/Data/HandParsedCards/BFZ/ScourFromExistence.json` — PASS. `exile` effect, `CardTypes = ["permanent"]`. Colorless `{7}` mana cost rendered correctly.
- `tests/magic-ast-tests/Data/HandParsedCards/GTC/ShatteringBlow.json` — PASS. `exile` effect, `CardTypes = ["artifact"]`. Hybrid `{R/W}` symbol typed as `hybrid` with both colors.
- `tests/magic-ast-tests/Data/HandParsedCards/SHM/Unmake.json` — PASS. `exile` effect, `CardTypes = ["creature"]`. Triple-hybrid `{W/B}{W/B}{W/B}` rendered as three hybrid symbols; mana value 3 correct per CR 202.3b.

### Family B — UntapTargetRule + 3 fixtures (Rule 701.26)

- `libs/magic-ast/Parsing/Parsers/Spell/Rules/UntapTargetRule.cs` — PASS. Filter vocabulary covers `creature | artifact | enchantment | land | planeswalker | permanent`. Emits `UntapEffect { Target = ObjectReference { Kind = Target, Filter = { CardTypes = [...] } } }` — descriptive verb+target only, no engine state mutation.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ModifyPTSpellRule.cs` (sibling-shape extension) — PASS. The new `"It gets +N/+M until end of turn"` branch is narrowly scoped (one additional regex, `ObjectReferenceKind.It` already exists in the AST, no new fields). No scope creep; same `ModifyPTEffect` shape as the existing "Target creature gets" branch.
- `tests/magic-ast-tests/Data/HandParsedCards/ULG/BurstOfEnergy.json` — PASS. `untap` effect, `CardTypes = ["permanent"]`.
- `tests/magic-ast-tests/Data/HandParsedCards/FRF/Refocus.json` — PASS. Two abilities (untap target + draw a card), one `spell` Kind per oracle line — consistent with the per-line ability convention. `drawCards` effect carries explicit `Count: 1` literal quantity and `Player.Kind = "You"`.
- `tests/magic-ast-tests/Data/HandParsedCards/9ED/Inspirit.json` — PASS. Single-line two-sentence oracle correctly bundled into ONE ability with TWO effects (untap + modifyPT) per the multi-effect-per-clause doctrine (`feedback_mast_multi_effect_per_clause`). Pronoun back-reference resolved via `Target.Kind = "It"`. `Duration.DurationType = "untilEndOfTurn"` per CR 514.

### Family C — Indestructible keyword + 3 fixtures (Rule 702.12)

- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (Indestructible entry) — PASS. `RuleReference = "702.12"` correct per `702.12a` ("Indestructible is a static ability") and `702.12b` ("A permanent with indestructible can't be destroyed"). `Category = KeywordCategory.Static`, `HasParameter = false`, expansion produces `StaticAbility { KeywordSource = "Indestructible", Effects = [IndestructibleEffect()] }`. Engine-flavored "damage can't kill" semantics correctly absent from the AST — the keyword's presence is the whole MAST record.
- `tests/magic-ast-tests/Data/HandParsedCards/DST/DarksteelCitadel.json` — PASS. Two abilities: `static` (indestructible) + `activated` mana ability (`{T}: Add {C}`). `IsManaAbility: true` correctly flagged per CR 605.1. Keyword presence recorded via `KeywordSource: "Indestructible"`.
- `tests/magic-ast-tests/Data/HandParsedCards/MH2/SilverbluffBridge.json` — PASS. Three abilities: enters-tapped static, indestructible static, hybrid mana activated. Discriminators `entersTapped` and `indestructible` are camelCase and term-matched to the rules.
- `tests/magic-ast-tests/Data/HandParsedCards/MH2/DarkmossBridge.json` — PASS. Same three-ability shape as Silverbluff with `{B} or {G}` mana production.

## Discriminator and structure checks

- All `EffectType` discriminators are camelCase and reuse established strings: `exile`, `untap`, `indestructible`, `drawCards`, `modifyPT`, `entersTapped`, `addMana`.
- All `ObjectReference` instances follow the convention: `Kind: "Target" | "It"`, `Filter: { CardTypes: [...] }`.
- No `Kind: "unparsed"` ability in any of the 11 fixtures.
- No `EffectType: "unparsed"` in any effect list.
- No free-text `Characteristics` shortcuts; everything filter-shaped goes through `CardTypes`.

## Cross-cutting checks

- **Helper rename safety:** `SpellRuleHelpers.ParseTargetFilter` has exactly 4 call sites in source (`DestroyTargetSimpleRule`, `DestroyAllRule`, `DestroyTargetTriggeredRule`, `ExileTargetSimpleRule`); zero stale `ParseDestroyFilter` references in `libs/magic-ast/**/*.cs`. There are three stale references in `libs/magic-ast/GLOSSARY.md` doc strings — non-blocking documentation lag, addressed by next GLOSSARY regeneration.
- **ExileTargetLand coexistence:** `ExileTargetSimpleRule.SimplePattern` regex explicitly omits `land` (alternation: `creature|artifact|enchantment|planeswalker|permanent|instant|sorcery`). Doc-comment documents the priority/alphabetical-dispatch rationale. No double-fire risk.
- **Descriptive-not-executive (per `feedback_mast_describes_not_executes`):** `ExileEffect`, `UntapEffect`, `IndestructibleEffect` carry verb + target (or, for Indestructible, just presence). No zone-move sequencing, no tap-state mutation field, no damage-prevention modeling. Engine semantics deferred to a runtime layer per doctrine.

## Glossary gaps

None. All terms used (`exile`, `untap`, `indestructible`, `target`, `permanent`, `creature`, `artifact`, `enchantment`, `land`) appear in `glossary.json`.

## Process notes

- Briefing prose contained two incorrect rule numbers (701.10 for Exile, 701.20 for Untap). Production source code does not propagate either error — `ExileTargetSimpleRule` cites the correct 701.13, and `UntapTargetRule` carries no rule citation (so cannot be wrong). Recommend orchestrators treat briefing rule numbers as suggestive only and verify against `rules-structure.json` before they reach source.
- Hybrid-mana coverage across batch 18 fixtures (Shattering Blow `{R/W}`, Unmake `{W/B}{W/B}{W/B}`) exercises the `hybrid` symbol discriminator at both single and triple-stack densities. Mana value computation appears correct in both cases (2 for Shattering Blow, 3 for Unmake) per CR 202.3b.
- Multi-effect-per-clause doctrine correctly applied at Inspirit (one ability, two effects) and correctly NOT applied at Refocus (two oracle LINES, two abilities). Per-line vs per-clause distinction handled consistently.
