# MAST judge — batch 24 verdict

**Date:** 2026-05-26
**Scope:** 8 files (6 fixtures, 1 new AST node, 1 new parser rule)
**Result:** PASS

## Summary

- PASS: 8
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

### Family A — ETB self-deals-damage-to-any-target

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SelfDealsDamageToAnyTargetTriggeredRule.cs` — PASS. Triggered analog of the spell-side rule; emits `DealDamageEffect { Source: It(), Amount: Literal N, Target: { Kind: AnyTarget } }`. Regex enforces a strict ETB-pronoun shape (`^it\s+deals?...$`) and uses the canonical `ObjectReferenceKind.AnyTarget` discriminator. Descriptive only — no damage-prevention / replacement / source-substitution modeled. Citations to Rule 603 (triggered abilities) and Rule 115.1 / 115.4 ("any target" → creature, player, planeswalker, or battle) are correct.

- `tests/magic-ast-tests/Data/HandParsedCards/MBS/BlisterstickShaman.json` — PASS. Models `When this creature enters, it deals 1 damage to any target.` as a single triggered ability with `Trigger { When/Enters, Filter: { CardTypes: ["creature"] } }` and `DealDamageEffect { Source: It, Amount: Literal 1, Target: { Kind: AnyTarget } }`. No unparsed nodes. Matches Rule 603.1 ability shape (`[When] [trigger condition], [effect]`) and Rule 115.4 "any target" semantics.

- `tests/magic-ast-tests/Data/HandParsedCards/FDN/SkeletonArcher.json` — PASS. Identical AST shape to Blisterstick Shaman, Goblin Shaman → Skeleton Archer subtype change only. Rule 603 + 115.4 satisfied.

- `tests/magic-ast-tests/Data/HandParsedCards/ROE/AkoumBoulderfoot.json` — PASS. Identical AST shape, Giant Warrior. Rule 603 + 115.4 satisfied. Consistent cross-fixture encoding for the same oracle clause.

### Family B — Improvise keyword

- `libs/magic-ast/AST/Effects/Keyword/ImproviseEffect.cs` — PASS. Parameterless keyword-presence record, `[OracleEffect("improvise")]` discriminator matches the rule's literal term word-for-word. Doc-comment cites Rule 702.126 correctly; verified against `rules-structure.json` 702.126a ("Improvise is a static ability that functions while the spell with improvise is on the stack. 'Improvise' means 'For each generic mana in this spell's total cost, you may tap an untapped artifact you control rather than pay that mana.'"). No fields — engine territory (per-artifact cost-reduction, total-cost-determination ordering from 702.126b) is correctly excluded. Mirrors the Convoke/Delve shape.

- `tests/magic-ast-tests/Data/HandParsedCards/AER/FoundryAssembler.json` — PASS. `StaticAbility { KeywordSource: "Improvise", Reminder: "(...)", Effects: [{ EffectType: "improvise" }] }`. Reminder text matches the canonical Improvise reminder verbatim. No unparsed nodes. Rule 702.126a static-ability classification satisfied.

- `tests/magic-ast-tests/Data/HandParsedCards/AER/WindKinRaiders.json` — PASS. Two abilities: Improvise (same shape as above) + Flying. Flying ability uses the established `evasion` effect with `CanBeBlockedBy { CardTypes: ["creature"], Characteristics: ["flying", "reach"] }` — the Rule 702.9 / 509.1b convention already validated in prior batches. KeywordSource "Flying" matches Rule 702.9a terminology.

- `tests/magic-ast-tests/Data/HandParsedCards/AER/BastionInventor.json` — PASS. Improvise + Hexproof; both use established keyword-effect conventions (`"hexproof"` discriminator matches Rule 702.11 terminology, reminder text verbatim). No unparsed nodes.

## Glossary gaps

None. "Improvise" present at `glossary.json` (`"A keyword ability that lets you tap artifacts rather than pay mana to cast a spell. See rule 702.126, 'Improvise.'"`). "Any target" semantics covered under Rule 115.4 in `rules-structure.json`.

## Process notes

1. **Parser-rule doc-comment imprecise on damage citation (non-blocking).** `SelfDealsDamageToAnyTargetTriggeredRule.cs` cites "Rule 119.3: damage dealt by a source." Rule 119.3 actually reads "If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly." — it is the *life-total adjustment* rule, not the damage-dealing rule. The correct damage citation is **Rule 120** (the entire "Damage" section), specifically 120.1 ("Objects can deal damage to battles, creatures, planeswalkers, and players") and 120.2 ("Any object can deal damage"). The briefing itself flagged "119.3 / 120 Damage" jointly, and the AST node `DealDamageEffect` is the load-bearing artifact (parser doc-comments are not in the strict scope of the judge skill's AST-node citation check). Flagged as a low-cost cleanup, not a HALT.

2. **Discriminator hygiene confirmed.** `"dealDamage"`, `"improvise"`, and `"AnyTarget"` (PascalCase per `ObjectReferenceKind` convention) all align with established MAST casing rules and rule-text terminology.

3. **Descriptive doctrine clean.** Neither family models cost-payment mechanics, damage resolution sequences, target re-legality, or replacement/prevention layering — all engine territory. Improvise records keyword presence only; the "tap artifacts → pay {1} each" mechanic is correctly excluded. DealDamageEffect carries source + amount + target only.

4. **No engine creep.** Family B's keyword stacks cleanly with sibling abilities (Flying, Hexproof) using existing conventions — no novel ability-mixing scaffolding introduced.

5. **Cross-fixture consistency.** The three Family A fixtures encode the same oracle clause with byte-identical ability JSON (modulo type-line subtype data). Same property for the three Family B fixtures' Improvise stanza. This is the expected outcome for a single new rule + a single new keyword.
