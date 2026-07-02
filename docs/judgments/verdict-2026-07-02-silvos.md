# MAST judge — batch verdict (delta: silvos)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-silvos
**Scope:** 1 fixture (Silvos, Rogue Elemental) + 1 parser rule (RegenerateSelfByNameEffectRule.cs); regenerate discriminator pre-existing
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/.../ONS/SilvosRogueElemental.json#activated-regenerate` — PASS. Oracle "{G}: Regenerate Silvos." verified against oracle-cards.json. Modeled as `Kind: activated`, `Costs:[mana {G}]`, `Effects:[{EffectType: regenerate, Target:{Kind: Self}}]`, `IsManaAbility:false`. Correct discriminator (CR 701.19 keyword action); "Silvos" is the card's own name so Target=Self is faithful per CR 201.5 (its own example is literally "Regenerate Skithiryx"). Describe-not-execute (shield/destruction-replacement left to engine); timing carried by the activated-ability wrapper + mana cost, not baked into the effect.
- `tests/.../ONS/SilvosRogueElemental.json#trample` — PASS (out-of-axis sibling). Static `keywordAbility` Keyword `Trample`, matching the established convention (identical to ChargingBinox / CuboidColony gold). Preserved, structured, no residual.
- `mast-tdd/2026-07-02-silvos#projection` — PASS. Branch adds a parser rule + fixture only; the `regenerate` EffectType already exists on base and already carries a sensible `known-coarse-projections.json` entry ("baseline coarse fallback … no flow rule consumes it yet"). A regeneration shield is a destruction-replacement, not an interaction edge a flow rule would clearly want — coarse is defensible. No new discriminator ⇒ projection ratchet not triggered.

## Delta checks (a)-(d)

- (a) Target line structured correctly — YES (right node `regenerate`, Target Self, describe-not-execute, no baked timing).
- (b) No new free-text / unparsed residual — YES (jq sweep: no `unparsed` Kind/EffectType anywhere).
- (c) No regression — YES. New fixture; both abilities present (Trample + regenerate), attributes intact (manaCost {3}{G}{G}{G} MV6, colors G, colorIdentity G, 8/5), TypeLine Legendary Creature — Elemental. Nothing dropped/added/inverted.
- (d) Cited CR rules exist & match — YES. CR 701.19a (regenerate replacement effect) and CR 201.5 (name-reference = that object) both present in rules-structure.json and match the modeling.

## Glossary gaps

(none)

## Process notes

Oracle text confirmed exactly against oracle-cards.json: "Trample\n{G}: Regenerate Silvos." The parser rule's self-by-name heuristic (capitalised proper noun in the regenerate slot → Self) is code, not gold, and its doc-comment citations (701.19a, 201.5) are ground-truth. Both are cross-referenced clean.
