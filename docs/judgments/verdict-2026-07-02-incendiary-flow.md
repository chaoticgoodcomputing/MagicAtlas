# MAST judge — batch verdict (DELTA)

**Date:** 2026-07-02
**Batch:** incendiary-flow
**Branch:** mast-tdd/2026-07-02-incendiary-flow (base 90209551)
**Scope:** 1 fixture (KLD/IncendiaryFlow.json, new) + projection check
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/KLD/IncendiaryFlow.json` — PASS.
  Oracle text verified verbatim against oracle-cards.json: "Incendiary Flow deals 3 damage to
  any target. If a creature dealt damage this way would die this turn, exile it instead."
  - **(a) Correct structure.** Two-ability decomposition faithful to the two sentences:
    - Sentence 1 → `SpellAbility` / `dealDamage` (Amount literal 3, Target `AnyTarget`, Source `Self`).
    - Sentence 2 (this task's axis, the exile-instead-of-dying rider) → `StaticAbility` /
      `replacement` watching a `death` event on `AffectedObjects` = creature with History
      `dealtDamageBy` Self, `OriginalEventOccurs: false`, `Replacement` = `exile` Target `It`.
      Correct replacement shape (CR 614.1/614.6), correct "dies" semantics (CR 700.4),
      correct anaphoric `It`. Describe-not-execute; timing is NOT baked into an effect
      discriminator — it decomposes as `Kind: static` + `EffectType: replacement` + `Event: death`.
  - **(b) No new residual on this axis.** No `unparsed` Kind/EffectType anywhere. The sole free
    text is `DealtDamageByPredicate.Timeframe: "this way"`, which lives on the
    damage-provenance / linked-ability-window axis (CR 607.1) — the SAME deferred-enum debt
    family as SengirBats (which already carries a `Timeframe` free-text residual). It is a
    named `debt` whitelist carve-out with a plausible reason, on a DIFFERENT axis than the
    exile-replacement structure this task owns — not a fail per the dispatch.
  - **(c) No regression.** Brand-new fixture (absent at base) — no siblings to drop/invert;
    both sentences represented once. Whitelist change is a single append; no other entries,
    AST nodes, or projection files touched.
  - **(d) Citations sound.** CR 614.1, 614.6, 700.4, 607.1 all exist verbatim in
    rules-structure.json and match the modeling.

- `mast-tdd/2026-07-02-incendiary-flow#projection` — PASS.
  No new discriminator introduced. All effect/event/predicate types used
  (`dealDamage`, `replacement`, `death`, `exile`, `dealtDamageBy`) resolve to pre-existing AST
  nodes (`DealDamageEffect`, `ReplacementEffect`, `DeathEvent`, `ExileEffect`,
  `DealtDamageByPredicate`). No PortGraph case or `known-coarse-projections.json` entry is
  required, and none was expected — projection decision is N/A for this branch.

## Glossary gaps

None.

## Process notes

- The `Timeframe: "this way"` free text is genuinely a linked-ability window (CR 607.1) scoping
  the replacement to creatures dealt damage by THIS spell. A structured provenance-window enum
  would eventually replace it; the whitelist correctly buckets it with SengirBats as a separate
  de-string initiative rather than perpetuating an on-axis shortcut. Nit (non-blocking): the
  "would die **this turn**" one-shot duration is not carried as an explicit field, but the
  replacement's spell-scoped lifetime is implicit and does not misrepresent the rule.

**PROCEED** — FAIL count is 0.
