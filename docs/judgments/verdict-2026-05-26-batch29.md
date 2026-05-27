# MAST judge — batch verdict

**Date:** 2026-05-27 (batch dated 2026-05-26)
**Scope:** 4 files (3 fixtures, 1 AST node)
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Keyword/EvolveEffect.cs` — PASS. Parameterless keyword effect with discriminator `"evolve"`; doc-comment cites Rule 702.100 and correctly defers trigger / power-toughness comparison / counter-placement to engine territory per 702.100a. Mirrors AscendEffect / ConvokeEffect / PersistEffect shape (four trait interfaces, no mechanic-specific fields).
- `tests/magic-ast-tests/Data/HandParsedCards/GTC/AdaptiveSnapjaw.json` — PASS. Single ability: `StaticAbility { KeywordSource: "Evolve", Effects: [{ EffectType: "evolve" }], Reminder: { Text: ... } }`. Reminder text corpus-verbatim per Rule 702.100a.
- `tests/magic-ast-tests/Data/HandParsedCards/GTC/ClingingAnemones.json` — PASS. Two abilities in oracle order: Defender (parameterless `defender` discriminator, established convention) + Evolve. Both static, both per established keyword conventions (Defender per Rule 702.3, Evolve per Rule 702.100).
- `tests/magic-ast-tests/Data/HandParsedCards/GTC/CloudfinRaptor.json` — PASS. Two abilities in oracle order: Flying (`evasion` effect with `CanBeBlockedBy.Characteristics: [flying, reach]`, established convention per Rule 702.9b) + Evolve. No drift.

## Glossary gaps

None. "Evolve" is present in `glossary.json` with cite to Rule 702.100.

## Process notes

- **Reminder-text source-of-truth:** orchestrator's briefing called out that an earlier draft of the briefing quoted the rule's reminder text (`"that creature's power is greater...and/or that creature's toughness..."`) instead of the corpus oracle phrasing (`"greater power or toughness than this creature"`). The mech caught this and used the corpus text. This is the correct call: **oracle text is the source of truth for `Reminder.Text` fixture content, not rule text.** The rule and oracle describe the same trigger; the oracle just uses the shorter conjoined-comparison phrasing. Reminder-text fidelity is to printed oracle, not to the Comprehensive Rules text. Worth canonicalising in CONTRIBUTING.md if not already (the batch 28 reminder-variance lesson covers the same principle).
- **Static-Kind for triggered keyword:** Evolve is a triggered ability per Rule 702.100a, but fixtures encode it as `Kind: "static"` with `KeywordSource: "Evolve"`. This matches the established MAST convention for all named keyword abilities (the keyword's presence is static card metadata; the trigger semantics belong to the engine reading the keyword + rule). Consistent with Persist (batch 14, also triggered, also `Kind: "static"` with `KeywordSource`). Not a defect — the engine-lens-audit can revisit if it wants a `triggered` Kind for keyword-sourced abilities, but that's a structural axis change outside this judge's scope.
- **Rule citation precision:** the AST doc-comment cites Rule 702.100 (the parent named-rule). 702.100a is the operative definition subrule. For a parameterless keyword whose AST records only presence, parent-rule cite is appropriate — the AST doesn't model any specific subrule clause. PASS as written.
