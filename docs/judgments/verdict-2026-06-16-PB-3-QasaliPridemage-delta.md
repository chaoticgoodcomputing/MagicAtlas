# MAST judge — PB-3 delta verdict: QasaliPridemage

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (structured-characteristic axis + comparative-power)
**Scope:** 1 gold fixture (DELTA judgment, not whole-gold purity)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/QasaliPridemage.json` — PASS.
  The slice's TARGET residual was the Exalted trigger filter's combat-state characteristic. It moved from a
  free-text sink `{ "CharacteristicType": "other", "Description": "attacking alone" }` to the structured node
  `{ "CharacteristicType": "combatState", "State": "AttackingAlone" }` (CombatStateCharacteristic /
  CombatState.AttackingAlone in libs/magic-ast/AST/References/Characteristic.cs).

## Delta checks

- (a) TARGET structured correctly: YES. "attacking alone" → `combatState`/`AttackingAlone`, faithful to the real
  oracle text (verified against oracle-cards.json: "Exalted (Whenever a creature you control attacks alone...)")
  and to CR 506.5 ("A creature is attacking alone if it's attacking but no other creatures are"). Exalted itself
  is CR 702.83; the structured filter matches the reminder text.
- (b) No NEW out-of-scope free-text/unparsed residual: CONFIRMED. No remaining `CharacteristicType:"other"`,
  `Description`, `Kind:"unparsed"`, or `EffectType:"unparsed"` anywhere in the gold.
- (c) No regression: CONFIRMED. Both abilities preserved — triggered Exalted (modifyPT +1/+1 until end of turn on
  ThatCreature) and activated `{1}, Sacrifice this creature: Destroy target artifact or enchantment` (mana +
  sacrifice-self costs, destroy with CardTypes ["artifact","enchantment"] target). The co-occurring sibling
  filters (`CardTypes:["creature"]`, `Controller:"You"`) on the trigger are intact. The only other changes are
  cosmetic formatting and a defaulted `CantBeRegenerated:false` on the destroy effect — no semantic change.
- Whitelist: Qasali Pridemage was removed from whitelist-freetext.json (fully cleaned, no S6-shared debt). Correct.

## Out-of-scope residuals remaining

None on this gold. (Qasali is not an S6-shared gold and carries no other-axis residual.)

## Result

ALL PASS
