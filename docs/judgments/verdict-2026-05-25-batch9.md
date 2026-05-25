# MAST judge — batch 9 verdict

**Date:** 2026-05-25 **Mode:** verify (in-process)
**Scope:** 8 items (4 fixtures, 4 parser surfaces, 0 new AST types)
**Briefing:** `docs/judgments/briefing-2026-05-25-batch9.md`
**Result:** PASS (8 PASS / 0 FAIL)

## PASS verdicts
- `SpellAbilityParser.TryParseCounterTargetTypeOrSubtypeSpellEffect` — PASS. Cites Rule 205.3 (subtypes). Vocabulary-driven token classification routes card-types to `CardTypes` (lowercased) and subtypes to `Subtypes` (capitalization preserved). Subsumes both Nullify and Hisoka's Defiance shapes.
- `TriggeredAbilityParser` "aura" added to self-by-type list (Gift of Strands) — PASS. Reuses Crystal Grotto's batch-7 generalization.
- `StaticAbilityParser.TryParseAnthemModifyPT` (Gift of Strands) — PASS. `Enchanted creature gets +X/+Y.` → ModifyPTEffect with no Duration.
- `AbilityClassifier` ability-word conditional route + `SpellAbilityParser` preamble strip (Spell Snuff) — PASS. Uses documented `Instructions` free-text escape hatch for "If you have 5 or less life,".

Fixtures `CHK/HisokasDefiance`, `CHK/Nullify`, `MOR/GiftOfStrands`, `DKA/SpellSnuff` — all PASS.

## Process notes
- **Helper-flagged doctrine concern (Spell Snuff)**: `Instructions: ["If you have 5 or less life"]` is the free-text escape hatch. Long-term fix is structured `InterveningIf`/`Condition` on `SpellAbility` — currently only `StaticAbility` and `TriggeredAbility` carry that field. Should be lifted to a shared trait.
- **Subtype-self target convention** (Gift of Strands): "Aura" lands on `CardTypes` (not `Subtypes`) to match the existing self-by-type convention (Crystal Grotto's "this land" precedent). Doctrine question: should self-by-subtype-token route differently? Worth a future audit.
- **Mergeable conflict between Hisoka and Nullify**: Nullify's per-token vocabulary classification supersedes Hisoka's regex-based subtype-disjunction recognizer. Resolution: kept Nullify's; Hisoka's redundant method dropped during conflict resolution.

## Closing
**Verdict: PROCEED.**
