# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** obscura-storefront
**Branch:** mast-tdd/2026-07-02-obscura-storefront
**Base:** cb048c63ea6ae85ef069e0d47244ec68945a5415
**Scope:** 2 changed files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SNC/ObscuraStorefront.json` — PASS.
  Oracle text matches oracle-cards.json verbatim. The reflexive fetchland tail
  ("When you do, search your library for a basic Plains, Island, or Swamp card, put
  it onto the battlefield tapped, then shuffle and you gain 1 life") is modeled as a
  `createDelayedTrigger` wrapping a `DelayedTriggeredAbility` triggered on
  `Sacrifices{IsSelf}` — faithful to CR 603.12 reflexive triggered abilities (the
  rule's own Heart-Piercer Manticore example is the same "When you do" shape). Timing
  lives in the Trigger node, not baked into the effect discriminators (describe-not-
  execute). `searchLibrary` filter is structured (Supertypes Basic, CardTypes land,
  Subtypes Plains/Island/Swamp), Destination BattlefieldTapped, plus structured
  `shuffle{You}` and `gainLife{1, You}`. The leading `sacrifice{It}` sibling is
  preserved. No `unparsed`, no free-text Characteristics. New fixture (add-only) so no
  regression.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReflexiveSacrificeSearchBasicLandGainLifeRule.cs`
  — PASS. Cited CR 603.12, 603.7, 701.23a, 119.3 all exist in rules-structure.json and
  the quoted text matches the modeling. Reuse-only (no new nodes/enums/shared-file
  edits), anchored pattern, mandatory search (not wrapped in optional), optional life
  tail.
- `mast-tdd/2026-07-02-obscura-storefront#projection` — PASS. No new discriminator
  introduced: TriggerEvent.Sacrifices, SearchDestination.BattlefieldTapped, and
  CreateDelayedTriggerEffect/DelayedTriggeredAbility all pre-exist on base. Initiative-03
  projection ratchet is not triggered; no PortWalk/known-coarse decision required.

## Glossary gaps

(none)

## Process notes

Base rule 701.23 carries the search example; 701.23a is the cited subrule and matches
verbatim. The doc-comment correctly distinguishes the mandatory "sacrifice it" here from
the "you may sacrifice" of the Heart-Piercer Manticore example while still (correctly)
treating "When you do" as a separate reflexive/delayed ability object rather than an
inline effect. Diff touches exactly two files (rule + fixture); no out-of-axis nodes
changed.

**PROCEED** — 0 FAIL.
