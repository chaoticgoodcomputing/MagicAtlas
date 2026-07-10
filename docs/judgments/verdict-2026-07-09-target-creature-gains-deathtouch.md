# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** target-creature-gains-deathtouch
**Branch:** mast/target-creature-gains-deathtouch
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/VOW/BattleRageBlessing.json` — PASS.
  Input.OracleText is byte-identical to the real card ("Target creature gains deathtouch and
  indestructible until end of turn. (Damage and effects that say \"destroy\" don't destroy it.)");
  mana {1}{B}, Instant, mono-B colors/identity all match. The spell ability decomposes into two
  `gainAbility` effects: (1) `Target` creature (Filter CardTypes=creature) gains a static Deathtouch
  keyword ability, (2) `It` (anaphoric back-reference to the same creature) gains a static
  Indestructible keyword ability — both with `untilTime` end-of-turn duration. Faithful to CR 702.2
  (Deathtouch) / 702.12 (Indestructible) as a CR 611.2 spell-generated continuous effect applied in
  Layer 6 (CR 613.1f). No IUnparsed, no UnstructuredEffect, no `"Kind":"unparsed"` / `"EffectType":
  "unparsed"`, no lossy drop or merge. Reminder text is verbatim-by-design and correctly omitted
  (it only restates the two granted keywords). Anaphoric first-Target/second-It split mirrors the
  existing composite-buff rules and is sound.

- `libs/magic-ast/Parsing/Parsers/Spell/Rules/TargetCreatureGainsMultipleKeywordsSpellRule.cs` — PASS.
  New `[SpellRule]`/`IMultiSpellRule` parser rule (auto-discovered via reflection). Introduces NO new
  AST node or discriminator — it constructs pre-existing `GainAbilityEffect` / `StaticAbility` /
  `KeywordAbilityEffect` (and existing `KeywordAbility` enum values) verified present on the base
  commit. Emits a flat one-`GainAbilityEffect`-per-keyword list, requires >=2 " and "-joined keyword
  segments (strictly disjoint from the single-keyword rule), and bails to the fallback on any
  unrecognised keyword — no lossy escape hatch. Cited CR rules 611.2, 702.2, 702.12 exist and match
  the modeling. See finding below on 613.1c.

- `mast/target-creature-gains-deathtouch#projection` — PASS.
  No new discriminator (effect/cost type, trigger event, or restriction) is introduced
  (newAstNode=false, shared=[]), so initiative-03 ratchet requires no PortWalk projection decision.
  N/A by construction; nothing parked as coarse.

## Glossary gaps

None. Both "Deathtouch" and "Indestructible" are present in glossary.json with CR citations
(702.2 and 702.12 respectively).

## Process notes

- **Subrule-letter slip (non-blocking):** the rule's doc-comment cites "613.1c (Layer 6 —
  ability-adding effects)". In rules-structure.json, Layer 6 (ability-adding effects, keyword
  counters, ability-removing) is **613.1f**; **613.1c** is actually "Layer 3: Text-changing effects".
  The parent rule (613.1, the layering system) is correct and the prose correctly names Layer 6 /
  ability-adding, so this is a subrule-letter imprecision within the right parent — per the judge
  doctrine (do not nitpick parent-vs-subrule precision; forgive omitted/imprecise subrule letters),
  it is NOT a FAIL. Recommend correcting the letter to 613.1f on a future touch.
- The parser rule's keyword→ability factory is self-contained (duplicates the single-keyword rule's
  mappings by design so it can evolve independently); this is a code-quality/parser concern, out of
  the judge's rules-accuracy scope, and does not affect fixture or AST correctness.

ALL PASS
