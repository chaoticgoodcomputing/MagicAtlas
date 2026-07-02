# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** nesting-wurm
**Branch:** mast-tdd/2026-07-02-nesting-wurm (base 90209551)
**Scope:** 1 fixture + 1 parser rule (projection check)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NestingWurm.json` — PASS. Oracle text verified verbatim against oracle-cards.json ("When this creature enters, you may search your library for up to three cards named Nesting Wurm, reveal them, put them into your hand, then shuffle"). Target ability modeled as `triggered{ Trigger{Timing:When, Event:Enters, Filter{CardTypes:[creature], IsSelf:true}}, Effects:[ optional{ Inner: searchLibrary{ Filter{Name:"Nesting Wurm"}, Count{upTo, Max:3, Min:0}, Destination:Hand, Revealed:true } } ] }`. Correct discriminator (`searchLibrary`, CR 701.23), name filter faithful to "cards named", `upTo(3)` faithful to "up to three", `Revealed:true` = "reveal them" (CR 701.20), `Destination:Hand` = "put them into your hand", `you may` = `optional` wrapper. Timing lives in the Trigger node, not baked into the effect. "then shuffle" folded as rules-inferred bookkeeping (CR 701.24) — consistent with every existing searchLibrary fixture (DuneMover) and the CR 701.23 Veteran Explorer precedent. Trample static keyword sibling preserved; mana/colors/colorIdentity/creatureStats (4/3) correct. No free-text carrying rules meaning (card-name literal is exempt), no unparsed nodes, no dropped/added/inverted abilities.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SearchForNamedCardsUpToNTriggeredRule.cs#projection` — PASS. Branch introduces no new discriminator: it adds only a parser rule + fixture reusing pre-existing `searchLibrary`/`optional`/`upTo` nodes (SearchLibraryEffect.cs unmodified). No new PortWalk projection decision required; ratchet presence check N/A.

## Citation cross-reference

All three cited rules exist in rules-structure.json and match the modeling:
- CR 701.23 (Search) — present; its own example uses Veteran Explorer's "up to two ... then shuffle" pattern, directly supporting the count-bounded + folded-shuffle modeling.
- CR 701.20 (Reveal) — present; supports `Revealed:true`.
- CR 701.24 (Shuffle) — present; supports folding "then shuffle" as bookkeeping.

## Glossary gaps

(none)

## Process notes

New fixture (added, not modified) — no sibling fixtures altered, no out-of-axis nodes touched. Diff scope is exactly the two files above.
