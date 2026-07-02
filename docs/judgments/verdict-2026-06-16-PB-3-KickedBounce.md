# MAST judge — DELTA verdict (SLICE PB-3, MB6/KickedBounce)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (consolidated ATOMIC: structured-characteristic axis + comparative-power, PB-2 merged)
**Mode:** delta (judges only the change this slice made, not whole-gold purity)
**Scope:** 1 fixture (working-tree, uncommitted)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MB6/KickedBounce.json` — PASS.
  - **Target structured (a):** The slice's structured-characteristic axis target was the bounce
    target's free-text characteristic `Characteristics: [{CharacteristicType:"other",
    Description:"nonland"}]`. The regen replaced it with the structured negation axis
    `CardTypes:["permanent"]` + `ExcludedCardTypes:["land"]`, faithful to the card's oracle
    "Return target nonland permanent to its owner's hand." (CR 110.1 permanent; CR 205.2a land
    card type; CR 111.1 token-vs-card distinction grounding the ExcludedCardTypes negation axis).
    Correct structured node, correct axis, faithful to the real card.
  - **No new residual (b):** No new free-text/unparsed residual introduced. Remaining `"Raw"`
    fields are exempt verbatim-by-design (`TypeLine.Raw:"Instant"`, `manaCost.Raw:"{1}{U}"`);
    `Reminder.Text` is exempt reminder text. No `Description`, no `other` characteristic, no
    `Kind:"unparsed"`, no `EffectType:"unparsed"` anywhere in the gold.
  - **No regression (c):** Both abilities preserved — Kicker static (`additionalCastCost`,
    IsOptional) and the spell with `returnToHand` + the kicked-conditional `drawCards`
    (`Condition: keywordCostPaid Kicker`). No ability dropped/added/inverted; sibling effects
    intact. Non-semantic regen normalizations only (`Repeatable:false`, `Reminder`,
    `IsVariable:false`, key reordering). KickedBounce correctly removed from
    whitelist-freetext.json (no entry; not an S6-shared gold).
  - **Out-of-scope residual remaining:** None. This gold was fully cleaned by the slice.

## Process notes

- This fixture is a hand-authored teaching card ("Kicked Bounce") not present in oracle-cards.json;
  the fixture's own `Input.OracleText` is the source of truth and the structuring is faithful to it.
- No new discriminator introduced by this gold's change (the ExcludedCardTypes axis already exists),
  so no per-gold PortWalk projection verdict applies here.

ALL PASS
