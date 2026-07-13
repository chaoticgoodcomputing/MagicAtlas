# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** reveal-any-number-blue-cards
**Branch:** mast/reveal-any-number-blue-cards (b16df309) vs base aaec9d3b
**Scope:** 6 targets (1 fixture, 1 AST node, 2 parser rules, 1 projection decision, 1 schema edit)
**Result:** PASS

## Summary

- PASS: 6
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TOR/ScentOfBrine.json` — PASS. Input.OracleText byte-identical to real Scent of Brine (verified against oracle-cards.json), mana {1}{U}, Instant, colors/CI [U] all match. Both sentences fully structured: sentence 1 → `revealCards` (You / `anyAmount` / Hand / filter `card`+`U`), sentence 2 → `preventable` wrapping `counterSpell` (target filter `spell`) with `Unless{Player:Controller, Cost:scaledMana}`. No `unparsed`, no `UnstructuredEffect`, no free text, no lossy drop/merge — the "for each card revealed this way" tail is captured structurally by `scaledMana.Count = cardsRevealedThisWay`. Shape mirrors the Mana Leak counter gold (differing only in the scaled cost) and the Scent of Nightshade reveal gold (differing only in color). CR 701.20a (reveal), 701.6a (counter), 118.1 (cost) all quoted verbatim and correct.
- `libs/magic-ast/AST/Costs/ScaledManaCost.cs` — PASS. Descriptive decomposition of "pay {MANA} for each [count]" into `PerUnit` (ManaCost) x `Count` (Quantity); reference-not-resolution (ADR 0004) — records per-unit + count reference, engine multiplies at pay time. Cleanly distinct from flat `ManaCost`. CR 118.1 quoted verbatim; the unless-clause payment is exactly a "payment necessary … to stop another action from taking place."
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/RevealAnyNumberBlueCardsFromHandRule.cs` — PASS. Fully-anchored regex; emits `RevealCardsEffect` identical in shape to the black sibling. CR 701.20a quoted verbatim and correct.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/CounterUnlessPaysPerCardRevealedRule.cs` — PASS. Emits `Preventable(CounterSpellEffect(target spell), Unless{Controller, scaledMana(PerUnit x cardsRevealedThisWay)})`. Anchored end-to-end on the "for each card revealed this way" tail (Priority 60), so it never overlaps the flat-cost `CounterUnlessPaysRule` (Mana Leak). CR 701.6a and 118.1 quoted verbatim and correct.
- `libs/mast-interaction/known-coarse-projections.json#scaledMana` (projection decision, initiative 03) — PASS. `scaledMana` is a genuinely new cost discriminator (absent from base). The exhaustiveness ratchet requires presence; a coarse carve-out is present with a plausible reason. Sensibility: the interaction port walker contains no `Unless`/`Preventable` handling at all, so counter-unless payment costs are never projected as resource ports (even flat `mana`, which is semantically projected for activation/cast costs via `PortWalk.Costs`, is not walked on the unless side). A defensive counter-tax paid by an opponent is not consumed by any flow/loop rule, so parking `scaledMana` coarse is genuinely inert — not a flow-relevant discriminator parked as coarse.
- `libs/magic-ast/schema/ast-schema.json` — PASS. Sound generalization: adds the `ScaledManaCost` entry (discriminator `scaledMana`, `IsUnparsed: false`, Fields `Count`/`PerUnit`) to the Cost union; SchemaHash bumped consistently.

## Glossary gaps

None. Reveal (701.20), counter (701.6), and cost (118.1) are all covered by the rules data.

## Process notes

- `CardsRevealedThisWayQuantity` (`cardsRevealedThisWay`) is pre-existing (present at base, introduced by the Scent of Nightshade black sibling); this branch reuses it rather than reintroducing it. Consistent.
- The reason string for `scaledMana` describes flat `mana` as a "coarse … payment cost on the unless side." Flat `mana` is in fact semantically projected in general (`PortWalk.Costs` → `pay:mana:<color>`), but it is not walked when it sits inside an UnlessClause; the walker has no unless/preventable descent. So the operative claim ("no flow rule reads counter-unless payment resources yet") holds, and the coarse choice is sound. Non-blocking wording nuance only.

ALL PASS
