# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** deals-3-damage-divided (branch `mast/deals-3-damage-divided`)
**Base:** aaec9d3b
**Scope:** 3 files (1 fixture, 1 new rule node, 1 shared AST edit) + 1 initiative-03 projection item
**Result:** FAIL

## Summary

- PASS: 3
- FAIL: 1

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Triggered/Rules/ItDealsDamageDividedAmongTargetsRule.cs
**Verdict:** FAIL
**Issue:** Doc-comment miscites the source-anaphora rule — cites nonexistent `CR 113.8b`.
**Rule citation:** CR 113.7 (correct) vs CR 113.8 (cited)
**Rule text:**
> CR 113.7: "The source of an ability is the object that generated it. ... The source of a triggered ability ... on the stack ... is the object whose ability triggered."
> CR 113.8: "The controller of an activated ability on the stack is the player who activated it. The controller of a triggered ability on the stack ... is the player who controlled the ability's source when it triggered ..."
**What the AST says:** doc-comment: "CR 113.8b: 'it' is the anaphoric self-source pronoun (the permanent whose trigger fired)."
**Why this misrepresents the rule:** 113.8 governs the *controller* (a player) of an ability, not the identity of the source object; and 113.8 has no subrules, so `113.8b` is absent from the rules data. The concept the doc-comment describes ("the object whose ability triggered") is exactly CR 113.7. This is a wrong-rule + absent-subrule citation, not mere subrule-letter imprecision.
**Suggested fix:** Change the citation from `CR 113.8b` to `CR 113.7`. No modeling change needed — `Source = ObjectReference.It()` correctly models the self-source; only the doc-comment's rule number is wrong.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/GangOfDevils.json` — PASS. Input.OracleText byte-identical to the real card ("When this creature dies, it deals 3 damage divided as you choose among one, two, or three targets."); mana cost / type line / P/T / id all match. Gold AST has no IUnparsed / UnstructuredEffect / lossy merge: `Trigger{Dies, IsSelf}` + a single `dealDamage` with `Amount:3`, `Divided:true`, `Source:It`, and `Target:AnyTarget` carrying `UpTo(Min 1, Max 3)` faithfully encodes CR 115.7f division, CR 601.2d's ≥1-per-target floor, and CR 115.4 "any target".
- `libs/magic-ast/AST/Effects/Damage/DealDamageEffect.cs#Divided` — PASS. Sound shared generalization: nullable `Divided` records only the division *mode* (the single Amount is split, not multiplied per target); omitted for the ordinary single-recipient case. Doc-comment cites CR 115.7f and CR 601.2d — both exist and their text matches the modeling.
- `libs/mast-interaction/PortWalkProjection.cs#DealDamageEffect.Divided` (initiative-03 projection) — PASS. `Divided` is a boolean facet on the already-projected `dealDamage` effectType (semantic projection `emit:damage:<combat>:<recipient>`, the damage-flow arm), not a new PortWalk dispatch discriminator; the divided case rides the existing damage emit. Division-among-N-targets is a quantity/targeting nuance no flow rule reads as a distinct edge, so no new `PortGraph` case or `known-coarse-projections.json` entry is warranted. Present-by-subsumption and sensible.

## Glossary gaps

None. "any target", "divide"/"distribute", and the damage terms are all covered by the cited CR rules.

## Process notes

- All four cited CR rules were cross-referenced against `rules-structure.json`: 115.7f, 601.2d, 115.4 verified verbatim; 113.8 exists but is the controller rule (no subrules) — the intended rule is 113.7, present in the same subsection.
- The FAIL is a documentation-only defect (rule number in a doc-comment). The AST modeling, the shared `Divided` edit, and the gold fixture are all trustworthy; the fix is a one-token doc-comment change (113.8b → 113.7) after which the batch is clean.

**HALT: mast/deals-3-damage-divided**
