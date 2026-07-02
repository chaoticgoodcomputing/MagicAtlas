# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 branch (mast-tdd/2026-07-02-red-ward) — 1 fixture (LEG/RedWard.json) + projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LEG/RedWard.json` — PASS. Oracle text matches oracle-cards.json verbatim ("Enchant creature\nEnchanted creature has protection from red. This effect doesn't remove this Aura."). Target line: `gainAbility` (Target EnchantedOrEquipped) -> `GainedAbility` static `KeywordSource: Protection` -> `protection` effect `From:[{Color, R}]` with `DoesNotRemoveThisAura: true`. Discriminator `protection` matches CR 702.16a; red encoded as color "R" (not a colorless-encoding concern); the self-preservation clause is a structured boolean flag (describe-not-execute, no baked timing), faithful to CR 702.16n (specified Aura not put into graveyard as an SBA) overriding CR 702.16c. Enchant-creature sibling preserved as `enchantRestriction` (CardTypes: creature). No `unparsed` Kind/EffectType, no free-text `Characteristics`. New fixture — no prior state to regress.
- `mast-tdd/2026-07-02-red-ward#projection` — PASS. The new `DoesNotRemoveThisAura` bool refines the already-projected `protection` effect type, which carries a justified `known-coarse-projections.json` entry ("baseline coarse fallback — no flow rule consumes it yet"). The self-preservation flag creates no flow edge (no mana/draw/untap/trigger) and is genuinely inert for combo reconstruction; parking it coarse is sensible.

## Glossary gaps

(none)

## Process notes

CR cross-reference: 702.16a (protection static ability), 702.16c (Aura-removal SBA), and 702.16n (the "this effect doesn't remove this Aura" override) all exist verbatim in rules-structure.json and match the modeling; the ProtectionEffect.cs doc-comment quotes 702.16n/702.16c accurately.
