# MAST judge — batch verdict

**Date:** 2026-05-26
**Batch:** 15 (ETB-destroy mech + Echo helper-novel)
**Scope:** 14 items (9 fixtures, 2 AST nodes, 3 parser surfaces)
**Result:** PASS

## Summary

- PASS: 14
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

### AST nodes

- `libs/magic-ast/AST/Effects/Keyword/EchoEffect.cs` — PASS. Descriptively models Rule 702.30a: `Cost` field captured as polymorphic `Cost` (typical `ManaCost`), no upkeep-trigger / sacrifice-unless-pay machinery baked in. Doc-comment correctly cites 702.30 and acknowledges the descriptive-not-engine doctrine. Discriminator `"echo"` matches the rule terminology word-for-word. Trait set (`IOptionalEffect, IDurativeEffect, IPreventableEffect`) mirrors Bestow as briefed.
- `libs/magic-ast/AST/Effects/ZoneChange/RegenerateEffect.cs` — PASS. Descriptively models Rule 701.19 with a single `Target: ObjectReference` field; the shield / replacement-event semantics are deferred to engine per 701.19a/b. Discriminator `"regenerate"` matches glossary entry. Doc-comment cites 701.19 precisely and flags the replacement-event mechanics as out-of-scope.

### Parser surfaces (unmerged branches inspected)

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/DestroyTargetTriggeredRule.cs` (branch `worktree-agent-a2498df175a0cd1a4`) — PASS. Single regex `^destroy\s+target\s+(?<filter>.+)$`, delegates filter parsing to the shared `SpellRuleHelpers.ParseDestroyFilter` (same lexical surface as `DestroyTargetSimpleRule` — no body duplication). No free-text fallthrough: a filter the helper does not recognize returns `false`, preserving the constrained-vocabulary discipline. Cites Rule 701.8 implicitly via `DestroyEffect` reuse.
- `libs/magic-ast/Parsing/Combinators/OracleParsers.cs` Echo addition (branch `worktree-agent-a0029bd6bf966825e`) — PASS. Mirrors Bestow shape exactly: `Keyword("Echo")` + `AtLeastOnce()` mana symbols + `_optionalReminder`. Emits `StaticAbility { Effect = EchoEffect { Cost } }` with no upkeep-trigger expansion. Added to the `.Or()` chain after `Bestow.Try()` per the briefing. Reminder text lands on `StaticAbility.Reminder`, not modeled semantically.
- `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs` `TryParseRegenerateEffect` (branch `worktree-agent-a0029bd6bf966825e`) — PASS. Handles "Regenerate this creature" → `ObjectReference.Self()` and "Regenerate target [type]" → structured `Target/Filter`. No replacement-event modeling; emits `RegenerateEffect` descriptively per Rule 701.19.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReturnToHandRule.cs` controller-filter extension (branch `worktree-agent-a0029bd6bf966825e`) — PASS. "an opponent controls" maps to a structured `ControllerFilter.Opponent` discriminator (not free text). Mirrors the existing `You` branch's shape.

### Fixtures — Echo family

- `tests/magic-ast-tests/Data/HandParsedCards/USG/ViashinoOutrider.json` — PASS. Single `static` ability, `KeywordSource: "Echo"`, `EffectType: "echo"`, `Cost` is a well-formed `ManaCost` with `{2}{R}` symbols. No upkeep-trigger expansion. Matches Rule 702.30a literal text.
- `tests/magic-ast-tests/Data/HandParsedCards/USG/GoblinWarBuggy.json` — PASS. Two sibling static abilities: `Haste` (existing keyword) + `Echo {1}{R}`. Both descriptively modeled. Rule 702.30a + Rule 702.10 (Haste).
- `tests/magic-ast-tests/Data/HandParsedCards/ULG/AlbinoTroll.json` — PASS. Static `Echo {1}{G}` + activated `{1}{G}: Regenerate this creature` with `Target.Kind = "Self"`. Activated cost shape uses standard `mana` cost; effect uses new `regenerate` discriminator. Rule 702.30a + Rule 701.19a (resolving-ability variant).
- `tests/magic-ast-tests/Data/HandParsedCards/TSP/Stingscourger.json` — PASS. Static `Echo {3}{R}` + triggered ETB `returnToHand` with `Target.Filter.Controller: "Opponent"` (structured). Matches Rule 702.30a + Rule 603.6a (enters trigger) + Rule 109.2 (target filter).

### Fixtures — ETB-destroy family

- `tests/magic-ast-tests/Data/HandParsedCards/P02/OgreArsonist.json` — PASS. Single triggered ability: `Enters` event with self-filter `creature`, single `destroy` effect with `Target.Filter.CardTypes = ["land"]`. No `Each`, no duration. Matches Rule 603.6a + Rule 701.8a.
- `tests/magic-ast-tests/Data/HandParsedCards/VMA/GoblinSettler.json` — PASS. Identical shape to Ogre Arsonist with same `land` filter — confirms the rule's vocabulary is structurally invariant across cards.
- `tests/magic-ast-tests/Data/HandParsedCards/DDU/ViridianShaman.json` — PASS. `destroy target artifact` → `CardTypes: ["artifact"]`. Matches Rule 701.8 + Rule 205.2 (artifact card type).
- `tests/magic-ast-tests/Data/HandParsedCards/CMD/MonkRealist.json` — PASS. `destroy target enchantment` → `CardTypes: ["enchantment"]`. Matches Rule 701.8 + Rule 205.2.
- `tests/magic-ast-tests/Data/HandParsedCards/UMA/AngelOfDespair.json` — PASS. Two abilities: `Flying` (existing keyword) + ETB destroy with `CardTypes: ["permanent"]` (widest filter case). Permanent-typed filter is the canonical "any permanent" shape; no degeneration to free text. Rule 702.9 + Rule 603.6a + Rule 701.8.

## Glossary gaps

None. Echo, Regenerate, and Destroy all have glossary entries pointing to their respective rules (702.30, 701.19, 701.8).

## Process notes

- No `"Kind": "unparsed"` or `"EffectType": "unparsed"` anywhere in the 9 fixtures.
- No free-text `Characteristics` arrays substituting for structured fields. Controller filters use the structured `ControllerFilter` discriminator (`Opponent`, `You`); type filters use `CardTypes` arrays.
- Echo cost is consistently typed as `ManaCost` across all four Echo fixtures, with symbol shape matching the established mana-cost convention (`generic` + `colored`).
- The `EchoEffect.Cost` field is declared as the polymorphic `Cost` base (not the concrete `ManaCost`). This is the right call: it parallels `BestowEffect`/`EquipEffect`/`CyclingEffect` and accommodates the (currently theoretical) non-mana echo cost case without baking the assumption into the type signature.
- `RegenerateEffect.Target` uses `ObjectReference` rather than a separate self-vs-target encoding, consistent with how `DestroyEffect` etc. handle targeting. The `Albino Troll` fixture exercises the `Self` reference; would expect future "Regenerate target creature" cards to exercise the `Target` reference branch already implemented in `TryParseRegenerateEffect`.
- `DestroyTargetTriggeredRule` correctly reuses `SpellRuleHelpers.ParseDestroyFilter` rather than duplicating the body of `DestroyTargetSimpleRule`. This keeps the constrained filter vocabulary single-sourced — a future filter addition lands in one place.
