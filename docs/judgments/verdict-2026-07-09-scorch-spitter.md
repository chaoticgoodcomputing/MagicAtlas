# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** scorch-spitter (branch `mast/scorch-spitter-it-deals-1-damage`)
**Scope:** 2 files (1 fixture, 1 shared rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M21/ScorchSpitter.json` — PASS. `Input.OracleText`
  is byte-identical to the real card ("Whenever this creature attacks, it deals 1 damage to the
  player or planeswalker it's attacking."). Gold decomposes correctly into a `triggered` ability:
  `Trigger{Timing: Whenever, Event: Attacks, Filter{CardTypes:[creature], IsSelf:true}}` for
  "Whenever this creature attacks" (CR 508 declare-attackers trigger), plus a plain `dealDamage`
  effect (`Amount` literal 1, `Target.Kind: AttackedPlayerOrPlaneswalker`, `Source.Kind: Self`).
  "it" = the ability's own source (CR 109.1); damage from a resolving triggered ability is CR 120.1;
  the defending object is the player/planeswalker the attacker is attacking (CR 508.1b). Timing and
  effect are properly composed (Trigger node + plain effect), no baked-in timing. No `unparsed`
  node, no `UnstructuredEffect`, no free-text, no lossy drop/merge.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/DealsDamageToAttackedPlayerOrPlaneswalkerRule.cs`
  — PASS. Sound generalization of an existing rule. The regex tail was widened from
  `that\s+creature\s+is` to `(?:that\s+creature\s+is|it['’]s|it\s+is)` so the same effect shape is
  reached by both surface forms: Cavalcade of Calamity ("that creature is", where the attacker is a
  distinct object from the source enchantment) and Scorch Spitter ("it's", where the attacker IS the
  source). Both name the single defending object the trigger's attacking creature is attacking and
  resolve to the same `AttackedPlayerOrPlaneswalker` reference / same gold shape — a legitimate
  merge, not a lossy conflation. The numeric-amount requirement (`\d+|one…ten`) is preserved, so the
  trample reminder "can deal excess combat damage to the player or planeswalker it's attacking" stays
  excluded. Cited rules CR 120 (damage), CR 508.1b (attacked player/planeswalker), and CR 109 (source
  object) all exist in `rules-structure.json` and match the modeling.

- `mast/scorch-spitter-it-deals-1-damage#projection` — PASS. No new discriminator introduced
  (`newAstNode=false`, `reachedVia=Extended`). The `AttackedPlayerOrPlaneswalker` `ObjectReferenceKind`,
  the `dealDamage` effect, and the `Attacks` trigger event all pre-exist at the base SHA
  (`ObjectReference.cs` already carries the reference kind). No new effect/cost type, trigger event,
  or restriction is added, so the initiative-03 PortWalk projection ratchet requires no new decision.

## Glossary gaps

None. Terms used (damage, attacks, planeswalker) are covered by existing rule concepts.

## Process notes

Branch scope is exactly the two expected files. This is a coverage extension of the existing
attack-trigger burn family to a second surface form ("it's attacking"), not a structural change.
