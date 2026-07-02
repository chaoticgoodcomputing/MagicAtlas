# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** demonic-vigor
**Branch:** mast-tdd/2026-07-02-demonic-vigor
**Scope:** 1 fixture (WTH/DemonicVigor.json), 1 parser rule (ReturnThatCardToHandOnDeathTriggeredRule.cs — informational, parser correctness is NUnit's job)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## Target line

Oracle (verified against oracle-cards.json, exact match):
> Enchant creature
> Enchanted creature gets +1/+1.
> When enchanted creature dies, return that card to its owner's hand.

The judged axis is the Aura death-triggered return. Modeled as:
- `Trigger { Timing: "When", Event: "Dies", Filter: { CardTypes: ["creature"], IsEnchanted: true } }`
- `Effects: [ { EffectType: "returnToHand", Target: { Kind: "It" } } ]`

This is structurally identical to the established sibling M14/UnhallowedPact ("When enchanted
creature dies, return that card to the battlefield...") — same trigger shape — differing only in
the destination (`returnToHand` vs `returnToBattlefield`), which correctly tracks "to its owner's
hand" vs "to the battlefield". Timing lives in the Trigger node, not baked into the effect
(describe-not-execute). `"that card"` anaphora → `ObjectReference.It()` per corpus convention.
`ReturnToHandEffect`'s definition already means "return [target] to its owner's hand", so no extra
destination field is required.

## PASS verdicts

- `WTH/DemonicVigor.json#triggered-return` — PASS. Correct trigger discriminators + plain
  returnToHand effect; no baked timing; no free-text/unparsed residual. CR 700.4 ("dies" = put into
  graveyard from battlefield) and CR 400.7 (esp. 400.7f — Aura death triggers can find the enchanted
  permanent's new object) both exist in rules-structure.json and match the modeling.
- `WTH/DemonicVigor.json#siblings` — PASS. No regression: the Enchant restriction static ability and
  the +1/+1 modifyPT static ability are preserved and faithful; three abilities map 1:1 to the three
  oracle lines; no dropped/added/inverted ability.

## Projection decision (initiative 03)

Not applicable. The branch adds NO new discriminator — `returnToHand`, `Event: "Dies"`,
`IsEnchanted`, `EnchantedOrEquipped`, and `enchantRestriction` all pre-exist in the corpus. The new
`.cs` is a parser *rule* (regex → existing `ReturnToHandEffect`), not a new AST node/effect type, so
the exhaustiveness ratchet requires no PortWalk projection entry.

## Glossary gaps

None. "Dies" is present in glossary.json (cites rule 700.4).

## Process notes

Citations verified: CR 700.4 and CR 400.7 both present in rules-structure.json; glossary "Dies"
present. No FAILs.

ALL PASS
