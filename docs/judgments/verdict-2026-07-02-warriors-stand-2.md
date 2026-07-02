# MAST judge — delta verdict (warriors-stand)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-warriors-stand
**Base:** 4618d173
**Scope:** delta-judge of regenerated gold `libs/magic-ast/schema/ast-schema.json` (+ new fixture, new Condition node)
**Result:** PASS

> Note: an untracked prior-run verdict `verdict-2026-07-02-warriors-stand.json/.md` already existed (same PASS conclusion); per SKILL these `-2` files are the current render.

## Summary

- PASS: 3
- FAIL: 0

## Card

Warrior's Stand — `{1}{W}` Instant. Oracle (verified against oracle-cards.json):
"Cast this spell only during the declare attackers step and only if you've been attacked this step.\nCreatures you control get +2/+2 until end of turn."

## Delta

The branch's schema delta is a single insertion: `BeenAttackedThisStepCondition` /
discriminator `beenAttackedThisStep` (fieldless Condition marker), plus a SchemaHash bump.
`TimingModificationEffect` (effectType `timingModification`) and its `Phase`/`Condition`
fields pre-exist on base — unchanged by this branch.

## PASS verdicts

- `libs/magic-ast/schema/ast-schema.json#beenAttackedThisStep` — PASS. New fieldless Condition
  marker for "you've been attacked this step"; fieldless is justified ("you" = controller,
  "this step" = current declare-attackers step, both idiom-inherent). Used as the `Condition`
  gate of a `timingModification` restrict effect — the "when" (Timing:Phase, Phase string) and
  the intervening gate (Condition) are decomposed, not conflated into a discriminator. Schema
  change is a pure insertion; no sibling entry dropped/reordered.
- `tests/.../WarriorsStand.json` — PASS. Casting restriction → `timingModification{Restrict,
  Timing:Phase, Phase:'declare attackers step'}` gated by structured `beenAttackedThisStep`.
  No `unparsed` node; the gate this task owns is fully structured (not dropped into
  `OtherCondition` free-text). Second ability "+2/+2 until end of turn" preserved and faithful
  (`modifyPT`, Each creature you control, Duration untilTime Turn/End). The `Phase` value is a
  free-text string, but it rides the PRE-EXISTING `Phase` field (documented for "upkeep",
  "combat", "end step") — a separate phase-naming axis, not a residual introduced by this task.
- `mast-tdd/2026-07-02-warriors-stand#projection` — PASS. `beenAttackedThisStep` is a
  `ConditionKind`, off the PortWalk projection surface. `PortWalkExhaustivenessTests` enumerates
  only `effectType`/`costType`/`triggerEvent`/`restriction`; no new discriminator on those axes
  was added (host `timingModification` is pre-existing). No projection decision is required,
  and none is missing — sensible.

## Rule cross-reference (all verified verbatim in rules-structure.json)

- CR 508.1 — "First, the active player declares attackers. This turn-based action doesn't use the stack. ..." (matches)
- CR 508.1a — "The active player chooses which creatures that they control, if any, will attack. ..." (matches condition doc-comment verbatim)
- CR 508.1b — "If the defending player controls any planeswalkers, is the protector of any battles ... announces which player ..." (matches verbatim)
- CR 601.3a — "If an effect prohibits a player from casting a spell with certain qualities, that player may consider any choices ..." (matches parser-rule doc-comment verbatim)

## Glossary gaps

None.

## Process notes

The only free-text in the gold is `Phase:"declare attackers step"` on the pre-existing
`TimingModificationEffect.Phase` string field — a standing phase-naming modeling choice on a
different axis, not a residual this task introduced. Structuring named phases into an enum is
out of this task's axis.

ALL PASS
