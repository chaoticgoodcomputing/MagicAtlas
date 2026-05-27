# Verdict — batch 21 (2026-05-26)

Mode: verify (judge-pass-2). Scope: Family A (ETB-energy `you get {E}…`) + Family B (Delve keyword).

Sanity check: `dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj` → **552/552 passed, 0 failed, 0 skipped**.

---

## Family A — `GainEnergyEffect` + `GainEnergyTriggeredRule`

| # | Item | Result | Note |
|---|---|---|---|
| A1 | Discriminator value `"gainEnergy"` matches `[OracleEffect("gainEnergy")]` and fixture gold | **PASS** | Sage of Shaila's Claim emits `"EffectType": "gainEnergy"`. |
| A2 | `Player` modeled as `ObjectReference` (mirroring `GainLifeEffect`), not a separate `PlayerReference` type | **PASS** | Field is `required ObjectReference Player`; fixture gold uses `{ "Kind": "You" }`. The briefing's free-text reference to "PlayerReference" was sloppy shorthand — the mech correctly used `ObjectReference`, which is the actual codebase convention. |
| A3 | `Amount: Quantity` required; literal counts use `LiteralQuantity` | **PASS** | Fixture: `{ "QuantityType": "literal", "Value": 3 }` for `{E}{E}{E}`. Symbol-count derivation in parser is sound. |
| A4 | Descriptive doctrine — no energy-pool / counter-on-player bookkeeping fields | **PASS** | `GainEnergyEffect` carries only `Amount`, `Player`, plus the four trait-interface fields (IOptionalEffect / IDurativeEffect / IPreventableEffect). No engine state. |
| A5 | Triggered-rule regex `^you\s+get\s+(?<symbols>(?:\{E\}\s*)+)$` correctly accepts 1-or-more `{E}` symbols, IgnoreCase, post-reminder-strip | **PASS** | Anchored both ends; `{E}` symbol-count via second regex over the capture group. Safe. |
| A6 | `TriggeredAbilityParser.ExtractTrailingReminder` regex change `\s*\(([^)]+)\)\s*\.?\s*$` accepts optional trailing period | **PASS** | End-anchored. `[^)]+` is non-greedy enough at the tail; only the LAST parenthetical is stripped per the existing contract. No regression risk in non-reminder contexts (552/552 confirms). |
| A7 | Reminder text stored without surrounding parens — matches existing convention | **PASS** | Gold: `"Text": "three energy counters"`. Same convention as XLN/Ixalli's Diviner reminder corpus. |
| A8 | Sage of Shaila's Claim gold — single ability, no extraneous content | **PASS** | One triggered ability, Trigger = `{ When, Enters, creature }`, single `gainEnergy` effect, reminder attached. ManaCost/colors/creatureStats attributes correctly populated. |
| A9 | No `unparsed` anywhere in Family A fixture | **PASS** | `grep -ril "unparsed"` over the fixture: none. |
| A10 | Energy-as-cost deferred (no `"Pay {E}"` fixture in this batch) | **PASS** | Only single ETB-energy printing (Sage of Shaila's Claim) was fixtured; mech correctly bailed on the others to avoid pulling in an `EnergyCost` AST. Documented in the scope. |
| **A11** | **Rule citation accuracy on energy** | **FAIL** | `GainEnergyEffect.cs` docstring cites "Rule 107.4f / 107.6" and `GainEnergyTriggeredRule.cs` cites "Rule 107.4f". Both are wrong. **107.4f is Phyrexian mana symbols** (`{W/P}`, `{U/P}`, etc.). **107.6 is the untap symbol `{Q}`.** The authoritative rule for energy is **107.14**: *"The energy symbol is {E}. It represents one energy counter. To pay {E}, a player removes one energy counter from themselves."* (Confirmed against `rules-structure.json`.) The briefing's own pre-flight cited "107.10" which is also wrong (that's the Future Sight timeshifted type-icon rule). The AST shape and behavior are correct, but the in-code rule citations actively misdirect future readers to unrelated CR sections. |

**Family A: 10 PASS, 1 FAIL (rule citation).**

---

## Family B — `DelveEffect` + KeywordDefinition + OracleParsers combinator

| # | Item | Result | Note |
|---|---|---|---|
| B1 | Discriminator value `"delve"` | **PASS** | `[OracleEffect("delve")]` matches fixture gold `"EffectType": "delve"`. |
| B2 | `DelveEffect` parameterless, mirrors `ConvokeEffect` | **PASS** | Only trait-interface fields (IOptional / IDurative / IPreventable). No exile-substitution mechanic modeled. Descriptive doctrine respected. |
| B3 | Rule citation `702.66` | **PASS** | `KeywordDefinition.RuleReference = "702.66"`. Confirmed against `rules-structure.json` — 702.66 is "Delve" with subrules a/b/c defining the cost-substitution mechanic. Glossary entry also points at 702.66. |
| B4 | `KeywordDefinition Delve` registered: `Category = Static`, `HasParameter = false`, in `All` list | **PASS** | Verified at `libs/magic-ast/Keywords/KeywordDefinitions.cs:339–355` and the `All` enumeration at line 406. |
| B5 | `OracleParsers.Delve` combinator added after `Convoke` in the SimpleKeyword `.Or()` chain | **PASS** | `OracleParsers.cs:1352–1353` — `.Or(Convoke).Or(Delve)` ordering preserved. Combinator at 406–414 produces `StaticAbility { KeywordSource = "Delve", Effects = [new DelveEffect()] }`. |
| B6 | Fixture gold shape — `static` ability with `KeywordSource: "Delve"` and `Effects: [{ EffectType: "delve" }]` | **PASS** | All three KTK fixtures (Treasure Cruise, Murderous Cut, Become Immense) match exactly. |
| B7 | No `Reminder` field on the Delve static ability in fixture gold | **PASS** (briefing-deviation, but correct) | The briefing prescribed a `Reminder: { Text: "..." }` field. Cross-check against precedent: `ConvokeEffect` fixtures (MeetingOfMinds, LivingTotem, WorldsoulColossus) **also** omit the reminder field on the keyword's static ability. The mech correctly followed the established Convoke precedent rather than the briefing's looser prescription. Convention wins. |
| B8 | Sibling spell-body lines properly modeled as separate `spell` abilities | **PASS** | Treasure Cruise → `drawCards { Count: 3, Player: You }`. Murderous Cut → `destroy { Target: creature }`. Become Immense → `modifyPT { Target: creature, +6/+6, Duration: untilEndOfTurn }`. All three use the existing parser surfaces correctly. |
| B9 | No `unparsed` anywhere in Family B fixtures | **PASS** | Grep over KTK/ fixtures: none. |
| B10 | KeywordDefinitions Delve docstring rule reference | **PASS** | "Rule 702.66" cited correctly in the inline comment. |

**Family B: 10 PASS, 0 FAIL.**

---

## Cross-cutting

- **NUnit suite:** 552/552 green confirms the reminder-strip regex change is non-regressing. The change is end-anchored and only impacts the trailing-parenthetical-then-optional-period pattern. Safe generic improvement.
- **Energy-as-cost deferral:** Briefing-acknowledged; no fixture attempts to model `Pay {E}`. Correct scope boundary.
- **Descriptive doctrine (`feedback_mast_describes_not_executes`):** Both families respect it. Neither AST type models the actual rules engine (energy-pool maintenance, graveyard-exile cost substitution).
- **Convoke mirroring:** Delve mirrors Convoke shape exactly — keyword name, parameterless effect, static category, registered via `KeywordDefinition` and `OracleParsers` combinator chain.

---

## Required follow-up before unconditional PROCEED

Fix the rule-citation errors in:
- `libs/magic-ast/AST/Effects/Resource/GainEnergyEffect.cs` (docstring: replace `Rule 107.4f / 107.6` with `Rule 107.14`).
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/GainEnergyTriggeredRule.cs` (docstring: replace `Rule 107.4f` with `Rule 107.14`).

This is the kind of error that compounds — future batches will copy the wrong citation by mirror-pattern. It is a rules-accuracy fault, which is exactly what the judge gate is meant to catch.

## Disposition

One FAIL on A11 (rule citation). Per the strict-PASS/FAIL contract, any FAIL halts the loop.

**HALT**
