# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** look-at-target-opponent-s-hand
**Branch:** look-at-target-opponent-s-hand (base b1c7f836)
**Scope:** 2 files (1 fixture, 1 parser/AST rule) + 1 projection item
**Result:** FAIL

## Summary

- PASS: 2
- FAIL: 1

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Triggered/Rules/LookAtTargetHandTriggeredRule.cs
**Verdict:** FAIL
**Issue:** Doc-comment cites a rule that contradicts the modeling.
**Rule citation:** CR 701.12
**Rule text:** > 701.12 Exchange — "...a spell attempts to exchange control of two target creatures..."
**What the AST says:** doc-comment: "the Peek pattern (Rule 701.12) as a triggered-ability effect fragment" for "look at target opponent's hand."
**Why this misrepresents the rule:** CR 701.12 is the Exchange keyword action (swapping control/ownership of objects), which has nothing to do with looking at cards in a hand. "Peek" is a card name (INV), not a CR keyword action — no "Look"/"Peek" keyword action exists in rule 701 (701.10-701.15 are Double, Triple, Exchange, Exile, Fight, Goad). Looking at a hand is an ordinary one-shot effect governed by hand rules (CR 402), not a 701 keyword action. The citation would mislead any downstream reader cross-referencing it.
**Suggested fix:** Drop the invented "Peek pattern (Rule 701.12)" citation from the doc-comment (a no-citation doc-comment is fine — the modeling is correct), or cite an actually-relevant rule (CR 402 hands). Note: the identical wrong citation is pre-existing in `libs/magic-ast/Parsing/Parsers/Spell/Rules/LookAtHandRule.cs` and in `LookAtCardsEffect.cs` ("Rule 701.12 (look)"); fix all three together. The parser regex and the emitted AST require no change.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/7ED/TelepathicSpies.json` — PASS. Input.OracleText byte-identical to Scryfall oracle ("When this creature enters, look at target opponent's hand."); mana cost/type line/P/T/colors all match. Gold AST is a `triggered` ability with `Trigger{Event:Enters, Filter:{CardTypes:["creature"], IsSelf:true}}` (correct for "this creature enters") + a `lookAtCards` effect (`Player`=Target opponent, `Count`=derived CardsInHand, `Zone`=Hand). No `unparsed`, no `UnstructuredEffect`, no `OtherX`, no free-text prose. The opponent restriction (CR 102.2 — cannot target self) is preserved.
- `look-at-target-opponent-s-hand#projection` — PASS. No new discriminator: the branch adds a parser rule that reuses the existing `lookAtCards` effect, `DerivedKind.CardsInHand`, `Zone.Hand`, and `ObjectReferenceKind.Target`. A parser rule (regex → existing AST) introduces no PortGraph case, so no PortWalk projection decision / known-coarse entry is required (initiative 03 ratchet is not triggered).

## Glossary gaps

None. "Opponent" is in glossary.json; the card introduces no new domain term.

## Process notes

- The new triggered rule is a near-verbatim port of the already-committed spell-side `LookAtHandRule.cs` (same regex, same emitted AST, same doc-comment). The worker's claim (reuse `lookAtCards`, `newAstNode=false`, `shared=[]`) is accurate — the diff adds only the two new files with no edits to shared code.
- The `Player` reference encodes "target opponent" as `Filter:{CardTypes:["opponent"]}`. "Opponent" is not literally a card type, but this mirrors the codebase's established player-role convention (`ObjectFilter.Player()` → `CardTypes:["player"]`, used by 21 fixtures) and an existing gold precedent (`ThoughtErasure.json` uses `CardTypes:["opponent"]` for target-opponent discard). The opponent restriction is faithfully preserved and this is not free-text prose, so it is not a FAIL here; whether player-role belongs on `CardTypes` vs `EntityType`/a controller-relation axis is a structural-family question for the engine-lens audit, out of judge scope.

HALT: look-at-target-opponent-s-hand
