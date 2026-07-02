# MAST judge — batch verdict (delta: doublecast)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-doublecast (base 4618d173)
**Scope:** 1 fixture (Doublecast.json) + 1 parser rule (NextCastCopySpellDelayedRule.cs, out-of-judge-scope code)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Doublecast.json` — PASS. Oracle text verified verbatim
  against oracle-cards.json ("When you next cast an instant or sorcery spell this turn, copy that spell.
  You may choose new targets for the copy."). Modeled as a single `spell` ability whose sole effect is
  `createDelayedTrigger` (CR 603.7 — an effect that on resolution creates a delayed triggered ability),
  wrapping a `DelayedTriggeredAbility`:
  - **Trigger** `Timing:"When"` (the codebase's "triggers once" timing — faithfully encodes the card's
    "next", distinct from `Whenever`), `Event:"SpellCast"` (CR 603.2), `Filter{CardTypes:["spell",
    "instant","sorcery"], Controller:"You"}` — matches the established "instant or sorcery spell you cast"
    convention (ThousandYearStorm, YoungPyromancer, ProfessorOnyx all use the same triple).
  - **Window** `untilTime / Turn·End` — captures the "this turn" bound as a separate duration field
    (CR 603.7b: a delayed trigger's "this turn" stated duration), precedented shape (AcrobaticLeap).
  - **Effect** `copy` with `Target{Kind:"It"}` ("that spell") and `MayChooseNewTargets:true`
    (CR 707.10 — a spell copy inherits targets unless an effect grants reselection).

  Timing and effect are a proper composite (trigger node carries the "when"; `copy` carries only the
  action) — no baked-in timing, describe-not-execute. No free-text and no `unparsed` residual anywhere in
  the ability body. New file / single ability, so no sibling or out-of-axis regression. All three cited
  rules (603.7, 603.2, 707.10) exist in rules-structure.json and their text matches the modeling.

## Projection decision (initiative 03)

Not applicable: the diff adds only a parser rule + fixture. No new AST discriminator is introduced —
`createDelayedTrigger`, `copy`, `SpellCast`, `When`, `untilTime`, and `MayChooseNewTargets` all pre-exist.
The exhaustiveness ratchet therefore requires no new PortWalk projection entry for this branch.

## Glossary gaps

_None._

## Process notes

The parser rule file `NextCastCopySpellDelayedRule.cs` is code (out of the judge's scope — parser
correctness is NUnit's job), but its doc-comment's CR citations (603.7, 603.2, 707.10) were cross-checked
and are accurate. The doc-comment transparently notes that "next" on an event trigger is expressed via
`When`+Window rather than the `GameTime{When:"Next"}` marker (which is reachable only on clock/"At"
triggers) — a defensible descriptive choice that preserves all textual content ("next" and "this turn").

**PROCEED** — FAIL count is 0.
