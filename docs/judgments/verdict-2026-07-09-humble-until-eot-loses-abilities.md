# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/humble-until-eot-loses-abilities
**Family:** until-end-of-turn-target-creatur — "Until end of turn, target creature loses all abilities and has base power and toughness 0/1." (Humble, USG)
**Scope:** 6 targets (1 fixture, 2 AST nodes, 1 parser rule, 1 projection entry, 1 schema)
**Result:** PASS

## Summary

- PASS: 6
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/USG/Humble.json` — PASS. Both clauses fully structured: `loseAbility` with `AllAbilities: true` + `setBasePT` Power 0 / Toughness 1, each carrying the shared until-end-of-turn `Duration`; single target modeled as `Target` (creature filter) on the first effect and anaphoric `It` on the second (CR 601.2c — one instance of "target" governs both). No `unparsed`/`UnstructuredEffect`, no lossy drop or merge, no free text. `Input.OracleText` byte-identical (92 chars) to Scryfall Humble oracle text; mana `{1}{W}`, Instant, colors/CI `[W]` all match.
- `libs/magic-ast/AST/Effects/Modification/SetBasePTEffect.cs` — PASS. New node models "has base power and toughness X/Y" as a layer-7b set effect; CR 208.4a ("Effects that set a creature's power and/or toughness to specific values may refer to base power and/or toughness") and CR 613.4b ("Effects that set power and/or toughness to a specific number... Effects that refer to the base power and/or toughness... apply in this layer") both verified verbatim. `Power`/`Toughness` typed as `Quantity` (literal for Humble, variable-ready); correctly distinguished from `DefinePTEffect` (CDA, layer 7a, single-value, targetless — CR 604.3) and `ModifyPTEffect` (7c additive). Layer/timestamp ordering left to engine per descriptive-not-executive doctrine.
- `libs/magic-ast/AST/Effects/Modification/LoseAbilityEffect.cs` — PASS. Sound generalization: the universal "loses **all** abilities" quantifier becomes a structured `AllAbilities` bool (a scope, not a named ability) instead of a free-text sink; `AbilityText` demoted to optional residual for single named-ability removals. CR 113.10 ("An effect that removes an ability will state that the object 'loses' that ability") cited and verified.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/LosesAllAbilitiesAndSetBasePTSpellRule.cs` — PASS. `IMultiSpellRule.TryMatchMulti` emits the flat sibling list `[loseAbility(AllAbilities:true), setBasePT]` with a shared target (`It` back-ref) and shared EOT duration; leading (Humble) and trailing (Ovinize) regex forms; single-effect `TryMatch` intentionally disabled. Cited CR 601.2c / 113.10 / 208.4 verified. (See process note on a stale doc-comment.)
- `libs/mast-interaction/known-coarse-projections.json#setBasePT` — PASS (initiative-03 projection decision). New discriminator `setBasePT` is parked as a justified coarse fallback, consistent with the P/T-setting family (`definePT`, `becomesCreature` confirmed present in the coarse list). No flow rule reads base-P/T-setting; a 0/1 base set is a soft-removal/downside effect, not a combo-enabling value edge — a sensible inert carve-out, not something a flow rule would clearly want.
- `libs/magic-ast/schema/ast-schema.json` — PASS. Regenerated schema is consistent with the code: `loseAbility` drops the now-conditional `AbilityText` (Fields = `[Target]`; `AllAbilities`/`AbilityText` are `[JsonIgnore]`-conditional), `setBasePT` added with `[Power, Target, Toughness]`; `SchemaHash` updated.

## Glossary gaps

- "base power and toughness" — referenced (discriminator `setBasePT`) in the fixture / `SetBasePTEffect.cs`. Not present as a term in `glossary.json`; the rules concept is covered by CR 208.4. Minor gap, non-blocking.

## Process notes

- The parser rule's doc-comment `<para>` describes emitting "a `LoseAbilityEffect` with `AbilityText=\"all abilities\"`", but the emitted code (and the gold fixture) correctly use the structured `AllAbilities = true` flag. This is a stale doc-comment (code-quality), not a rules-accuracy or gold-data defect — the actual gold data carries the structured quantifier, so it does not affect the verdict.
- `modifyPT` is referenced in the projection reason as a P/T-family sibling but is not itself in the coarse list (it may carry a semantic projection); does not affect the sensibility of parking `setBasePT` coarse.

ALL PASS
