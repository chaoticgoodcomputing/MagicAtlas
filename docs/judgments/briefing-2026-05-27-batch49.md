# Batch 49 briefing — ETB deals N damage to target opponent

**Date:** 2026-05-27
**Cluster:** TopYieldClusters[16] (1-indexed rank 17)
**Template:** `<TRIG> this <TYPE> enters, it deals <N> damage to target opponent.`
**Yield:** 10 cards
**Family:** (UnparsedTriggered, TriggeredAbilityParser.Parse)

---

## Family 1: ETB self-ping to target opponent (Desert dual lands)

**Failure signal:** `TriggeredAbilityParser.Parse` produces an `UnparsedAbility` for the
effect half "it deals N damage to target opponent." because no existing `[TriggeredRule]`
matches the "target opponent" target shape. The trigger condition side (Enters, land filter)
parses fine — `ParseEntersTrigger` already handles "this land enters".

### Cards in this family

1. **Lonely Arroyo** — `This land enters tapped.\nWhen this land enters, it deals 1 damage to target opponent.\n{T}: Add {W} or {U}.` (otherUnparsed=0)
2. **Jagged Barrens** — `This land enters tapped.\nWhen this land enters, it deals 1 damage to target opponent.\n{T}: Add {B} or {R}.` (otherUnparsed=0)
3. **Eroded Canyon** — `This land enters tapped.\nWhen this land enters, it deals 1 damage to target opponent.\n{T}: Add {U} or {R}.` (otherUnparsed=0)

(10 cards total in corpus — all identical oracle structure, varying only in mana colors.)

### Relevant rules

- **Rule 603 (Triggered Abilities)** — triggered abilities begin with "When", "Whenever", or "At". "When this land enters, it deals 1 damage to target opponent." is a triggered ability that fires when the land enters the battlefield (Rule 603.6d for ETB triggers).
- **Rule 120 (Damage)** — "Objects can deal damage to creatures, planeswalkers, and players. This is generally detrimental to the object or player that receives that damage." A land dealing damage to a player is a legal damage source; sources don't need to be creatures.
- **Rule 115 (Targets)** — "target opponent" invokes the targeting rules. The ability requires the controller to choose a target opponent when the ability goes on the stack (Rule 115.1). Opponent is defined in Rules 102.2-102.3.
- **Rule 102.2 (Opponent)** — "Two or more players are opponents if they are not on the same team." The target is constrained to opponents only (not "any player" which would include the controller).

### AST types in scope

- **`DealDamageEffect`** — `[OracleEffect("dealDamage")]`. Fields: `Amount: Quantity`, `Target: ObjectReference`, `Source: ObjectReference?`, `IsOptional: bool`, `IfYouDo: Effect?`, `IfYouDoNot: Effect?`, `Duration: Duration?`, `UnlessClause: UnlessClause?`. Source: `libs/magic-ast/AST/Effects/Damage/DealDamageEffect.cs`.
- **`ObjectReference`** with `Kind = ObjectReferenceKind.Opponent` — already defined in `ObjectReferenceKind` enum with doc "an opponent, target opponent". Source: `libs/magic-ast/AST/References/ObjectReference.cs`.
- **`LiteralQuantity`** — `[OracleQuantity("literal")]`. Factory: `LiteralQuantity.Of(n)`. Source: `libs/magic-ast/AST/Quantities/`.
- **`EntersTappedEffect`** — `[OracleEffect("entersTapped")]`. No required fields. Source: `libs/magic-ast/AST/Effects/`. Used for "This land enters tapped." static ability.
- **Triggered ability pattern** — filter `{ CardTypes: ["land"] }` for "this land enters"; `TriggerEvent.Enters`; `TriggerTiming.When`.

### Sibling abilities

All 10 cards have 3 lines:
1. `"This land enters tapped."` → static ability, `EntersTappedEffect` — already parsed by the `EntersTappedParser`.
2. `"When this land enters, it deals 1 damage to target opponent."` → triggered ability — **this family's gap**.
3. `"{T}: Add {X} or {Y}."` → activated mana ability — already parsed.

Each card's `Parser_ProducesExpectedOutput` test will only go green when all three lines parse correctly. The static and activated lines should already be green; only line 2 is failing.

### Expected generalization

One new `[TriggeredRule]` class — `SelfDealsDamageToOpponentRule` — matching `^it\s+deals?\s+(?<amount>\d+|...)\s+damage\s+to\s+target\s+opponent\.?$` (mirrors `SelfDealsDamageToAnyTargetTriggeredRule` but with `ObjectReferenceKind.Opponent` and the "target opponent" token). Handles all 10 corpus cards in one surface.

### Anti-patterns

- Do NOT model `Source` as something other than `ObjectReference.It()` — "it" refers to the land itself (the ETB trigger's source permanent). The precedent from `SelfDealsDamageToAnyTargetTriggeredRule` is `Source = ObjectReference.It()`.
- Do NOT use `ObjectReferenceKind.AnyTarget` — "target opponent" is more restrictive than "any target" (excludes creatures and planeswalkers). `ObjectReferenceKind.Opponent` is the correct kind.
- Do NOT conflate "target opponent" with "each opponent" — this is a single targeted opponent (`Opponent` kind), not `EachOpponent`.

### Glossary gaps

None — "target", "opponent", and "damage" are all present in `glossary.json`.
