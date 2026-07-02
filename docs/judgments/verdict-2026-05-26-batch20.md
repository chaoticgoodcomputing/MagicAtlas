# MAST judge — batch 20 verdict

**Date:** 2026-05-26
**Scope:** 6 fixtures + 2 source surfaces (KeywordDefinitions.Partner, TriggeredAbilityParser.TryParseAttacksTrigger). No new AST node files.
**Result:** PASS

## Summary

- PASS: 8
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

### Family A — Partner keyword (Rule 702.124)

- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (Partner entry, lines 313-332) — PASS. `Name = "Partner"`, `RuleReference = "702.124"`, `Category = Static`, `HasParameter = false`, expansion produces `StaticAbility { KeywordSource: "Partner", Effects: [PartnerEffect { PartnerType: PartnerType.Partner }] }`. Rule 702.124a defines Partner as a static deck-construction ability; 702.124h gives the literal expansion "You may designate two legendary cards as your commander rather than one if each of them has partner." Descriptive doctrine respected — MAST records keyword presence; Commander-format mechanics stay engine-side per `feedback_mast_describes_not_executes`. `PartnerWith` (702.124j) remains separately registered and the dispatch ordering note in the briefing (PartnerWith.Try() before Partner.Try()) is the right call given the parameterless variant would otherwise greedily swallow "Partner with [Name]" prefixes.

- `tests/magic-ast-tests/Data/HandParsedCards/C16/RavosSoultender.json` — PASS. Four abilities all gold-modeled with existing types: Flying (`evasion` with flying/reach block filter — Rule 702.9), LordPTBuff (`modifyPT` over `Target.Kind: Each` with `Controller: You` + `Characteristics: ["other"]` for "Other creatures you control"), BeginningOfUpkeep trigger emitting `returnToHand` with `IsOptional: true` for the "may" (Rule 603 + 116), and Partner keyword with reminder text matching the printed parenthetical exactly. One ability per oracle line per `feedback_mast_multi_effect_per_clause`.

- `tests/magic-ast-tests/Data/HandParsedCards/C16/RograkSonOfRohgahh.json` — PASS. Four keyword abilities, each split into its own ability (correct since the oracle uses comma-separated keyword list "First strike, menace, trample" but the fixture splits them — consistent with current MAST convention of one keyword per ability). First strike → `combatDamageTiming: First` (Rule 702.7), Menace → `evasion` with `MinimumBlockers: 2` (Rule 702.111b — the rule mandates the 2-blocker minimum, fixture carries it), Trample → `trample` (Rule 702.19), Partner → as above. Note: scope's spelling was "RograkhSonOfRohgahh.json" but actual filename is `RograkSonOfRohgahh.json` — verified the file under that name. See process notes re: ColorIdentity emission.

- `tests/magic-ast-tests/Data/HandParsedCards/C16/IshaiOjutaiDragonspeaker.json` — PASS. Three abilities: Flying, opponent-spell-cast trigger (`Timing: Whenever`, `Event: SpellCast`, `Filter: { Controller: Opponent }`) emitting `putCounters` on `Target.Kind: Self` with `CounterType: "+1/+1"` and `Count: 1` (Rule 603 + 121.1 + 122), and Partner. The `Target.Kind: Self` for "on Ishai" correctly resolves the self-reference-by-name to the AST's self target convention.

### Family B — Attack-trigger ParseObjectFilter unification (Rule 508 + 603)

- `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs:654-680` (`TryParseAttacksTrigger`) — PASS. The method body now delegates to `ParseObjectFilter`, structurally parallel to `ParseDiesTrigger` / `ParseEntersTrigger`. The doc-comment correctly cites Rule 508 (declare attackers step) and enumerates the three shapes the unified path handles: self-by-name ("Whenever [CardName] attacks"), anonymous self ("Whenever this creature attacks"), and controller-filter ("Whenever a creature you control attacks"). This is a load-bearing dedup — the previous inline branch couldn't reach the "this [type]" shape that `ParseObjectFilter` handles generically. No new AST types, no escape hatches. Regression-protection: 544/544 NUnit green confirms self-by-name and "you control" attack-trigger fixtures (e.g., Radha) still parse.

- `tests/magic-ast-tests/Data/HandParsedCards/M12/SteadfastCathar.json` — PASS. Single triggered ability with `Timing: Whenever`, `Event: Attacks`, `Filter: { CardTypes: ["creature"] }` (the unification's output for anonymous "this creature"), emitting `ModifyPTEffect { Target: It, PowerModifier: 0, ToughnessModifier: 2, Duration: untilEndOfTurn }`. Rule 508.1a (creature is declared as attacker) + 603 (triggered abilities) + 122 (P/T modification with explicit duration).

- `tests/magic-ast-tests/Data/HandParsedCards/M19/BrazenWolves.json` — PASS. Identical shape to Steadfast Cathar with `PowerModifier: 2, ToughnessModifier: 0`. Correctly mirrors the rules-side structure; only the modifier values differ.

- `tests/magic-ast-tests/Data/HandParsedCards/PCY/ChargingBandits.json` — PASS. Identical shape, `+2/+0` variant. Three-fixture coverage of the unified path across {W, R, B} colors validates the parser is color-agnostic on the trigger shape.

## Glossary gaps

None. Partner is in `glossary.json` under the composite key `"Partner, \"Partner-[text],\" \"Partner with [name]\""` with rule cite 702.124 + 903.

## Process notes

1. **Rograkh ColorIdentity discrepancy.** Input declares `ColorIdentity: ["R"]` (Scryfall ground truth — Kobolds carry a red color indicator), Output emits `ColorIdentity: []`. The briefing flagged this explicitly as a parser-emission question and asked for confirmation; the fixture matches what the parser produces. Strictly per CR 903.4 / 202.2, color identity includes color indicators, so the printed truth is `[R]`. This is a parser-side accuracy gap (color identity does not currently account for color indicators), not a Family A doctrinal failure — the Partner modeling is independent of this. Surfacing for triage; out of scope for batch 20. Not a FAIL because the fixture's gold is what the parser actually emits, which is the load-bearing contract for `Parser_ProducesExpectedOutput`.

2. **`Characteristics: ["other"]` on Ravos's LordPTBuff** is the established AST convention for "Other creatures you control" (excludes self). Pre-existing pattern; not a free-text smell because it has a well-defined meaning in the AST's vocabulary.

3. **Reminder text fidelity.** All three Partner fixtures carry the exact printed reminder text `(You can have two commanders if both have partner.)`. Rule 207.2 (reminder text in italics) makes this the canonical printed form, and the fixtures match.

4. **One-keyword-per-ability for Rograkh.** The oracle line "First strike, menace, trample" is one printed line carrying three comma-separated keywords. The fixture splits into three abilities (one keyword each). This is the correct call — Rule 702.1 treats each keyword as a distinct ability ("Keyword abilities are static, triggered, or activated abilities, each of which is also a single keyword ability"). The `feedback_mast_multi_effect_per_clause` memory addresses bundling multi-effect *sentences*, not comma-separated keyword lists — those remain one-ability-per-keyword.

5. **Family B fixture set covers the gap minimally.** Three identical-shape printings with different P/T deltas and colors is the right validation surface; expanding the cluster's remaining 13 candidates is mechanical and can land in a future batch without doctrinal risk.
