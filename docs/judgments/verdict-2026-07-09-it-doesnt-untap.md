# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 AST rule node) + 1 projection check — branch `mast/it-doesnt-untap-during-cont`, base `aaec9d3b`
**Result:** FAIL

## Summary

- PASS: 2
- FAIL: 1

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Triggered/Rules/ItDoesntUntapNextUntapStepTriggeredRule.cs
**Verdict:** FAIL
**Issue:** Wrong/absent CR citation for the anaphoric-pronoun modeling decision.
**Rule citation:** CR 113.8 (cited in the doc-comment as "CR 113.8b")
**Rule text:** > "The controller of an activated ability on the stack is the player who activated it. The controller of a triggered ability on the stack (other than a delayed triggered ability) is the player who controlled the ability's source when it triggered..."
**What the AST says:** doc-comment: "Here the subject is 'it', the generic anaphoric pronoun (CR 113.8b) referring to the object named by the trigger's filter — for Apes of Rath, the attacking creature."
**Why this misrepresents the rule:** CR 113.8 has zero subrules, so `113.8b` is absent from `rules-structure.json`; and CR 113.8's actual subject is ability-controller determination on the stack, not the reference semantics of the pronoun "it". The node makes a real modeling decision here (`Target = ObjectReference.It()`) and cites this rule as its authority, so the wrong-topic/absent citation is load-bearing, not incidental. This mirrors the "citing the Fading rule on a Kicker node" FAIL pattern.
**Suggested fix:** Drop the `(CR 113.8b)` parenthetical (a missing citation is fine — the modeling is correct), or cite the combat rule that actually identifies the attacking creature "it" refers to (CR 508.1, Declare Attackers) — the analogue of the sibling `ThatCreatureDoesntUntapTriggeredRule`, which correctly cites CR 509.1 for its blocked-creature back-reference. The primary effect citation CR 502.3 is verbatim-correct and should stay.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TMP/ApesOfRath.json` — PASS. OracleText byte-identical to Scryfall oracle-cards.json ("Whenever this creature attacks, it doesn't untap during its controller's next untap step."); mana cost / type line / P-T / colors all match. Trigger decomposes correctly into timing (`Whenever`) + event (`Attacks`) + filter (`{creature, IsSelf}`) separate from a structured `doesntUntap` effect (CR 502.3) with `Target{Kind:It}` + `WhoseUntapStep:"its controller's next"`. No `unparsed`, no `UnstructuredEffect`, no `OtherX`, no lossy drop/merge.
- `mast/it-doesnt-untap-during-cont#projection` — PASS (N/A). No new discriminator: the `doesntUntap` effect, `ObjectReferenceKind.It`, and the `Attacks` trigger event are all pre-existing (`newAstNode=false`, `shared=[]`). No PortWalk projection decision is required, so nothing to judge for sensibility.

## Glossary gaps

None. `glossary.json` covers Untap Step, Attacking Creature, Declare Attackers, Untap, etc.

## Process notes

- The new rule is a faithful sibling of `ThatCreatureDoesntUntapTriggeredRule` (identical regex modulo the subject, same `DoesntUntapEffect` + `WhoseUntapStep` convention), differing only in subject: "it" (`ObjectReferenceKind.It`) vs "that creature" (`ObjectReferenceKind.ThatCreature`). Behavior/output modeling is sound — the ONLY defect is the doc-comment's `CR 113.8b` citation.
- `DoesntUntapEffect.WhoseUntapStep` is a pre-existing free-text `string?` field ("your" / "its controller's next"), carried unchanged (not a shared edit; `shared=[]`). It borders on encoding a structured concept (which player's step + "next") as prose, but it is established convention used identically by the sibling rule and is out of scope for this branch — noted, not failed.

## Result

**HALT: mast/it-doesnt-untap-during-cont** — one FAIL (wrong/absent `CR 113.8b` citation on the new rule node). Trivially fixable inline by removing the parenthetical or repointing to CR 508.1; the fixture and effect modeling are otherwise trustworthy to merge.
