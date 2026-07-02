# MAST judge — batch verdict (relearn)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-relearn
**Scope:** 1 fixture (Relearn.json), 1 supporting parser rule (ReturnInstantOrSorceryFromGraveyardSpellRule.cs — reference only)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Relearn.json` — PASS. Oracle text verified against oracle-cards.json ("Return target instant or sorcery card from your graveyard to your hand.", {1}{U}{U} sorcery, mono-U). Single `spell` ability with one `returnToHand` effect: `Target` reference (CR 115.1 — "target"), `ObjectFilter` with `CardTypes:["instant","sorcery"]` (card-type disjunction carried on the structured CardTypes axis, not free text), `Zone:Graveyard` (source zone, CR 404.1), `Controller:You` ("your graveyard"). One-shot resolution modeled as a plain effect body with no baked-in timing (CR 608.2). Attributes (manaCost/colors/colorIdentity) and TypeLine correct.

## Delta / regression check

New file (absent on HEAD) — no prior siblings to drop, invert, or reorder. `returnToHand` (ReturnToHandEffect), `Zone.Graveyard`, `ControllerFilter.You`, and `CardTypes` are all pre-existing discriminators/axes; the branch adds only a parser rule that reuses them, so no new PortWalk projection decision is required (initiative-03 ratchet not triggered). No `"Kind":"unparsed"`, no `"EffectType":"unparsed"`, no free-text `Characteristics`. The only `Raw`/`RawText` fields are verbatim-by-design (type line, oracle text, mana cost).

## Rule cross-reference

- CR 608.2 — resolution of an instant/sorcery spell. Present, matches (one-shot spell body).
- CR 404.1 — graveyard is the discard pile. Present, matches (source zone).
- CR 402.1 — the hand. Present, matches (destination).
- CR 115.1 — targeting. Present, matches ("target" keyword ⇒ Target reference).

## Glossary gaps

(none)

## Process notes

Card-type disjunction "instant or sorcery" is encoded as a two-element `CardTypes` list, which is the codebase's established structured convention for pure card-type OR (cf. the generic ReturnToHandRule building CardTypes from multiple type tokens). This is strong typing, not the free-text `Characteristics: ["instant or sorcery"]` anti-pattern, so it PASSes.

ALL PASS
