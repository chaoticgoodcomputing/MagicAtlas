# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** dinotomaton
**Branch:** mast-tdd/2026-07-02-dinotomaton (base 90209551)
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LCI/Dinotomaton.json` — PASS. Oracle text matches oracle-cards.json verbatim ("Menace (...)\nWhen this creature enters, target creature you control gains menace until end of turn."). ETB grant modeled correctly on-axis: `triggered` ability with `Trigger{Timing:When, Event:Enters, Filter{creature, IsSelf:true}}` (the "when" lives in a separate trigger node — no timing baked into the effect) + a single `gainAbility` effect whose `Target` is `{Kind:Target, Filter{creature, Controller:You}}` ("target creature you control"), whose `GainedAbility` is a structured static Menace (`evasion` / `CanBeBlockedBy:creature` / `MinimumBlockers:2`, CR 702.111b) rather than free text, and whose `Duration` is `untilTime → Turn/End` ("until end of turn", CR 611.1). Describe-not-execute: no target-legality or stack semantics baked in.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TargetCreatureYouControlGainsKeywordUntilEndOfTurnRule.cs` — PASS. Doc-comment cites CR 702.111 (subrules a/b/c quoted verbatim — all present in rules-structure.json), CR 115.1 (target declaration), CR 611.1 (continuous effect with fixed duration); all three exist and match the modeling. Rule builds a structured keyword ability and returns false on an unrecognised keyword (bails to fallback) rather than emitting a free-text residual.
- `mast-tdd/2026-07-02-dinotomaton#projection` — PASS (N/A). No new discriminator introduced: the effect type `gainAbility` (GainAbilityEffect), `evasion` (EvasionEffect), and the `Enters` trigger event all pre-exist on base; the diff touches only a new parser rule + new fixture (no AST node changes). No PortWalk projection decision is required.

## Regression check

- Fixture is brand-new (no prior LCI/Dinotomaton.json on base) — no prior gold to regress.
- Both printed abilities represented: static Menace keyword (ability 1, KeywordSource:Menace + reminder text) preserved as a sibling; ETB grant (ability 2) added on-axis. No ability dropped, added, or inverted.
- Out-of-axis nodes correct and unchanged: manaCost {3}{R} (MV 4), colors [R], colorIdentity [R], creatureStats 4/3, type line Artifact Creature — Dinosaur Gnome.

## Glossary gaps

(none — "menace" is CR 702.111.)

## Process notes

Only verbatim-exempt raw fields present (TypeLine.Raw, Oracle.RawText, Reminder.Text, manaCost.Raw, P/T Raw); no `unparsed` nodes, no `Characteristics`/`*Text` residual carrying parsed semantics. The gained-ability static Menace mirrors the card's own static Menace node exactly, which is internally consistent.

**PROCEED** — 0 FAIL.
