# Batch 16 verdict — Family 1 only (ETB-explore)

**Date:** 2026-05-26
**Scope:** 7 files (5 fixtures, 1 AST node, 1 parser rule)
**Result:** PASS

> **Family 2 deferred.** This judge pass covers Family 1 (ETB-explore) only. Family 2 (CantAttackOrBlock — `Enchanted creature can't attack or block.`) bailed on the trait-boundary stop condition: `StaticAbility.Effect` is singular and would need pluralization to a list before the multi-effect-per-clause shape can land. Family 2 will be handled as its own batch after a separate StaticAbility migration. Its absence here is by design, not a FAIL.

Source branch: `worktree-agent-ab5b00f19d12132b3`.

## Summary

- PASS: 7
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/AST/Effects/CardFlow/ExploreEffect.cs` — PASS. Discriminator `"explore"` matches the glossary term verbatim and the Rule 701.44 heading. Rule cite `701.44a` is precise to the keyword-action subrule. Field surface is `Target: ObjectReference` plus the standard `IOptionalEffect` / `IDurativeEffect` / `IPreventableEffect` traits, mirroring `SurveilEffect`. The reveal / land-to-hand / +1/+1-counter / graveyard sequence from 701.44a appears only in the descriptive doc-comment, never as fields — consistent with the descriptive-not-executive doctrine (`feedback_mast_describes_not_executes`). Directory placement (CardFlow alongside SurveilEffect) is justified in the doc-comment: explore's primary descriptive axis is library-reveal-then-zone-shift, with counter placement as a conditional secondary path.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExploreTriggeredRule.cs` — PASS. `[TriggeredRule]` reflection-discovered; no modification to `TriggeredAbilityParser.cs`. Match pattern `^it\s+explores$` (after `Trim().TrimEnd('.')`, case-insensitive) is tightly anchored — it cannot swallow any non-trigger oracle line that merely mentions "explore" (e.g., "this creature explores twice"; "exploring") because of the `^…$` anchors and the literal terminal `explores`. Subject is `ObjectReference.It()` per Rule 109.1 ("it" in a triggered ability refers to the triggering object), matching the convention used by `ModifyPTTriggeredRule`. Reminder-text handling is delegated to the existing paren-strip infrastructure on `TriggeredAbility.Reminder`.

- `tests/magic-ast-tests/Data/HandParsedCards/LCI/RiverHeraldScout.json` — PASS. Trigger shape `{Timing: When, Event: Enters, Filter: {CardTypes: ["creature"]}}` matches Rule 603.6a ETB convention. Effect `{EffectType: "explore", Target: {Kind: "It"}}` matches Rule 701.44a + Rule 109.1. Reminder text preserved verbatim on `Reminder.Text`. All discriminators camelCase. No `"unparsed"`.

- `tests/magic-ast-tests/Data/HandParsedCards/LCI/PathfindingAxejaw.json` — PASS. Identical ability shape to RiverHeraldScout; differs only in P/T and mana cost. Matches Rule 701.44a + Rule 109.1.

- `tests/magic-ast-tests/Data/HandParsedCards/XLN/MerfolkBranchwalker.json` — PASS. Identical ability shape; matches Rule 701.44a + Rule 109.1. Reminder text preserved.

- `tests/magic-ast-tests/Data/HandParsedCards/XLN/IxallisDiviner.json` — PASS. Identical ability shape; matches Rule 701.44a + Rule 109.1. Reminder text preserved.

- `tests/magic-ast-tests/Data/HandParsedCards/XLN/QueensAgent.json` — PASS. Two abilities: (1) sibling Lifelink modeled as keyword `static` ability with `KeywordSource: "Lifelink"` and `Effect: {EffectType: "lifelink"}` per the established keyword-StaticAbility convention (Rule 702.15 Lifelink); (2) explore trigger identical to the other four fixtures. No bundling across abilities; sibling shapes are independent.

## Glossary gaps

None. `Explore` is in `glossary.json` and cites Rule 701.44. `Lifelink` is established prior-art. `It` pronoun handling is grounded in Rule 109.1.

## Process notes

- The five fixtures share an identical trigger+effect shape; the only varying surface is `Attributes` (mana cost, colors, creatureStats) and Queen's Agent's sibling Lifelink. This is the expected generalization: one new `[TriggeredRule]` covers all 15 cards in the cluster.
- The doc-comment on `ExploreEffect` includes the full 701.44a reminder-style procedural text in its summary. This is descriptive prose, not a structural field — the AST shape itself carries no reveal/counter/graveyard machinery, which is what matters for the descriptive-not-executive doctrine. PASS.
- The trait set `IOptionalEffect, IDurativeEffect, IPreventableEffect` is broader than the canonical ETB-explore line strictly requires (no oracle currently says "You may explore" or "explore. Then if you don't, …"), but mirroring the standard effect-trait surface is the established convention; carrying optional traits unused-but-uniform is preferable to bespoke trait sets per effect.
- Family 2's bail is the correct call. Pluralizing `StaticAbility.Effect` → `Effects` is a schema migration that touches every static-ability fixture and parser path in the corpus; bundling it into a mech batch alongside a novel-shape AST would conflate concerns. The deferral is on-doctrine.

## Closing

Path: `/home/spelkington/Repos/cgc/MagicAtlas/docs/judgments/verdict-2026-05-26-batch16.md`. Counts: PASS 7, FAIL 0. No blockers. Orchestrator should proceed.

PROCEED
