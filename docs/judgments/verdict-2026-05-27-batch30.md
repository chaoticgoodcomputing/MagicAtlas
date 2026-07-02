# MAST judge — batch verdict

**Date:** 2026-05-27
**Scope:** 4 files (3 fixtures, 1 parser rule)
**Result:** FAIL

## Summary

- PASS: 3
- FAIL: 1

## FAIL verdicts

### `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExileUntilLeavesTriggeredRule.cs`
**Verdict:** FAIL
**Issue:** Rule citation in doc-comment is wrong. The XML doc cites "Rule 701.10" for Exile, but **701.10 is "Double"**. The correct rule for Exile is **701.13**.
**Rule citation:** 701.13 (Exile) — verified against `rules-structure.json`.
**Rule text:** > 701.13 Exile. 701.13a "To exile an object, move it to the exile zone from wherever it is. See rule 406, 'Exile.'"
**What the AST says (lines 13–15):**
> "MAST records the exile action descriptively (Rule 701.10) with an `UntilLeavesBattlefieldDuration` whose `Object` is the literal self-reference phrase from oracle text..."
**Why this misrepresents the rule:** A judge checking the cited subrule against the Comprehensive Rules will find a doubling rule, not the exile keyword action. The descriptive doctrine is correct; only the number is wrong. The briefing for batch 30 also carried this miscitation (line 18 — "701.10 Exile"), which is how it propagated.
**Suggested fix:** Replace `Rule 701.10` with `Rule 701.13` in the XML doc-comment of `ExileUntilLeavesTriggeredRule.cs`. Sweep the batch-30 briefing and any other batch-30 artifacts for the same string (`701.10`) before merging the corrected version. Glossary entry for "Exile" already correctly references "rule 406, 'Exile'" — no glossary fix needed.

## PASS verdicts

- `tests/magic-ast-tests/Data/HandParsedCards/M19/HieromancersCage.json` — PASS. ETB enters-trigger (Rule 603.6) emitting `ExileEffect` (Rule 701.13) with `UntilLeavesBattlefieldDuration` (Rule 611.2 continuous effects with stated duration). Target filter `CardTypes:["permanent"] + Characteristics:["nonland"] + Controller:"Opponent"` mirrors the established VoidSnare M15 nonland precedent. Linked LTB-return correctly omitted per descriptive-not-engine doctrine (Rule 603.7d delayed-triggered abilities are engine territory).
- `tests/magic-ast-tests/Data/HandParsedCards/TDM/StormplainDetainment.json` — PASS. Identical body to Hieromancer's Cage; same verified shape; no `unparsed` nodes; mana-cost attribute reflects `{2}{W}` correctly.
- `tests/magic-ast-tests/Data/HandParsedCards/SPM/WebUp.json` — PASS. Type verified as Enchantment per briefing instruction; identical body and shape.

## Glossary gaps

None. "Exile" is present in `glossary.json` (definition references rule 406). "Nonland" not present as a discrete glossary term, but it composes from existing "Land" via negation and matches the VoidSnare precedent; not a new gap introduced by this batch.

## Process notes

**On the verification checks the dispatch enumerated:**

1. **Rule citations (check 1)** — **Failed.** 701.10 is "Double", not "Exile". Correct cite is **701.13**. 611 (continuous effects) is correctly cited at the parent-rule level; 611.2a would be the more precise subrule for a stated duration ("lasts as long as stated by the spell or ability creating it"), but parent-rule cite is defensible since 611 as a whole governs durations.
2. **Discriminator values (check 2)** — All correct: `"exile"` matches the keyword action name; `"untilLeavesBattlefield"` is a coined MAST term but consistent with prior conventions; `"Target"` and `"Opponent"` match existing enum values.
3. **Descriptive doctrine (check 3)** — Correct. No engine-flavored linked-LTB return modeled. The duration `Object` as oracle-verbatim string is consistent with the `AsLongAsDuration.Condition` precedent in `Duration.cs`.
4. **AST shape (check 4)** — Matches the briefing's prescribed shape exactly.
5. **Nonland filter doctrine (check 5)** — Consistent with the VoidSnare M15 precedent (`Characteristics: ["nonland"]`). The convention is intentional. Whether "nonland" should eventually move to a structured negation axis is an engine-lens question, not a per-batch judge call. The convention is currently coherent and not a regression.
6. **`UntilLeavesBattlefieldDuration.Object` first-use (check 6)** — Acceptable. The free-text-self-reference convention mirrors `AsLongAsDuration.Condition` (also free-text). Future structural promotion (e.g., `Self` ObjectReference) would be a refactor across both fields together; isolated promotion of this one field would create asymmetric precedent.
7. **No `unparsed`** — Verified. All three fixtures contain only structured nodes.
8. **No regressions** — 656/656 NUnit green per dispatch, including the prior VoidSnare and nonland fixtures.

**The lone FAIL is a one-character citation typo (`10` → `13`) in a single doc-comment.** Strict PASS/FAIL means this halts the loop. The fix is mechanical: replace `701.10` with `701.13` in `ExileUntilLeavesTriggeredRule.cs`, re-judge, proceed.
