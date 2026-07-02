# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** converter-beast (branch `mast-tdd/2026-07-02-converter-beast`)
**Scope:** 1 fixture + 1 projection decision (incubate keyword action on ETB)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MOM/ConverterBeast.json` — PASS. "When this creature enters, incubate 5." decomposes correctly into a `triggered` ability: a `Trigger{Timing:When, Event:Enters, Filter{CardTypes:[creature], IsSelf:true}}` (CR 603.6a) composed with a plain `incubate` effect carrying `Count` literal 5 (CR 701.53a). Timing and action are separate composable nodes — no baked-in timing. Discriminator `incubate` matches CR 701.53 word-for-word. Describe-not-execute: the Incubator token, its +1/+1 counters, and its "{2}: Transform this token." body are correctly left as verbatim reminder text (CR 207.2a / 111.10i), not inlined as rules-bearing free text. Oracle text matches oracle-cards.json verbatim; power 0 / toughness 1, colors, colorIdentity, manaCost and typeLine (Phyrexian Beast) all preserved. No unparsed node/effect anywhere; single ability, no dropped/added/inverted ability.
- `libs/mast-interaction/known-coarse-projections.json#incubate` — PASS. The new `incubate` discriminator carries an initiative-03 projection decision: a justified `known-coarse-projections.json` entry (CR 701.53a) rather than a semantic PortGraph projection. Sensible — incubate produces an Incubator token that requires paying `{2}` to transform before it is a 0/0 creature, so it does not immediately feed any flow rule; it is an explicit sibling of the already-coarse `amass`/`investigate` token-creation keyword actions. No flow rule reads Incubator-token creation yet, so parking it coarse is consciously inert, not an insensible miss.

## Rule cross-reference

- CR 701.53 (Incubate) — present; 701.53a/701.53b quoted verbatim in the node doc-comments.
- CR 603.6a (enters-the-battlefield abilities) — present; matches the `Enters` trigger modeling.
- CR 207.2a (reminder text) — present; justifies keeping the parenthetical token detail as `Reminder.Text`.
- CR 111.10i (Incubator token) — present; matches the reminder content.

## Glossary gaps

_None surfaced._

## Process notes

New `IncubateEffect` (`libs/magic-ast/AST/Effects/Keyword/IncubateEffect.cs`) and `IncubateTriggeredRule` are consistent with the amass/investigate keyword-action discipline. Schema regen (`ast-schema.json`) adds the `incubate` discriminator with the single `Count` field — consistent with the fixture. Nothing out-of-axis was touched.
