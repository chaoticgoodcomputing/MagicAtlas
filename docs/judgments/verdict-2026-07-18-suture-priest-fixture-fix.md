# MAST judge — batch verdict

**Date:** 2026-07-18
**Scope:** 3 changed files (1 fixture, 1 AST rule, 1 bench pin file) + 1 projection-decision check, branch `mast-tdd/2026-07-18-suture-priest-fixture-fix` (`295f3506`) vs base `6fca3caf`
**Result:** FAIL

## Summary

- PASS: 3
- FAIL: 1

## FAIL verdicts

### `libs/magic-ast/Parsing/Parsers/Triggered/Rules/MayHaveThatPlayerLoseLifeRule.cs`
**Verdict:** FAIL
**Issue:** wrong rule citation — CR 118.12 does not support the claim it's cited for.
**Rule citation:** CR 118.12 (as cited); should be CR 603.5
**Rule text:**
> CR 118.12: "Some spells, activated abilities, and triggered abilities read, '[Do something]. If [a player] [does, doesn't, or can't], [effect].' Or '[A player] may [do something]. If [that player] [does, doesn't, or can't], [effect].' The action [do something] is a cost, paid when the spell or ability resolves. The 'If [a player] [does, doesn't, or can't]' clause checks whether the player chose to pay an optional cost..."
> CR 603.5 (not cited, the actually-applicable rule): "Some triggered abilities' effects are optional (they contain 'may,' as in 'At the beginning of your upkeep, you may draw a card'). These abilities go on the stack when they trigger, regardless of whether their controller intends to exercise the ability's option or not. The choice is made when the ability resolves."

**What the fixture/AST says:** The doc-comment reads: "Here there is no follow-up: the 'you may' makes the life-loss itself optional (CR 118.12), full stop." — explicitly disclaiming the paired-conditional shape, then citing the one rule (118.12, filed under CR 118 "Costs") whose entire text *is* that paired-conditional shape.
**Why this misrepresents the rule:** CR 118.12 requires a subsequent "If [player] does/doesn't/can't, [effect]" clause and frames "[do something]" as a cost paid at resolution — neither element exists in Suture Priest's bare "you may have that player lose 1 life" (no follow-up at all, confirmed by the doc-comment's own words). CR 603.5, which is in `rules-structure.json` and directly on point (bare optional triggered-ability effects, choice made at resolution), was not cited. This is a genuine rule-family mismatch (CR 118 "Costs" vs CR 603 "Triggered Abilities"), not a subrule-letter imprecision.
**Suggested fix:** Replace `CR 118.12 ("may" effects)` with `CR 603.5 (optional triggered-ability effects)` in the doc-comment (both the class summary and the citation list at the bottom). The AST modeling itself (`OptionalEffect` wrapping `loseLife`, no `IfYouDo`) is correct and needs no change — this is a comment-only fix.

**Note on precedent:** the codebase's own existing `OptionalMillTriggeredRule.cs` (a prior bare-"you may X" rule with no follow-up) cites *no* "may" rule at all rather than reaching for 118.12 — the established convention already avoids this exact mismatch elsewhere in the codebase.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NPH/SuturePriest.json` — PASS. Corrected `Input.OracleText`/`TypeLine` verified byte-exact against `Data/_01_Raw/Datasets/External/oracle-cards.json` (both `"Whenever another creature you control enters, you may gain 1 life.\nWhenever a creature an opponent controls enters, you may have that player lose 1 life."` and `"Creature — Phyrexian Cleric"` match the live corpus record exactly). `Output` was independently confirmed to be a genuine re-run of the current parser: the fixture's parameterized test passes against the live parser (3 passed / 1 environment-gated skip unrelated to this branch), and every `SourceSpan.Start/Length` in the committed AST matches the exact substring offsets of the corrected oracle text (hand-verified via Python slicing, not merely trusted). Both effects correctly wrap in `OptionalEffect` per the "may" in the corrected text — CR 603.5, CR 119.3 (life totals), CR 109.5 ("that player").
- `tools/bench/MagicAtlas.Bench/combo-expected-tiers.json` — PASS. Independently reproduced the Green→Amber re-pin by applying only the corrected fixture + new rule to the pre-branch base commit *without* touching the pin: exactly one bench test fails (`Combo_261-2577-5670_is_Green`, actual Amber vs pinned Green), all other 72 pass unchanged. Traced the mechanism in code: `PortGraph.cs`'s generic `optional`/`composite` recursion (lines 658–680) marks the wrapped `gainLife` emit `Gated = true`, and `PortGraphEngine.cs` line 48's `Firable` predicate (`!Edges.Any(e => e.From.Gated || e.To.Gated) && ...`) floors any cycle touching a Gated port to Amber — exactly matching ADR-0002 §8 as the pin's reason text describes. The general Gated-floors-to-Amber engine policy is correctly left untouched by this branch (confirmed via `git diff --name-only`, only 3 files touched, no engine files). Ran the full bench suite on the branch head: 73/73 passed, with 261-2577-5670 the only combo whose pinned tier text differs from base.
- `libs/mast-interaction/PortGraph.cs#optional-projection` — PASS. This branch introduces no new AST discriminator: `OptionalEffect` and `LoseLifeEffect` both already exist with pre-existing `PortGraph` projections (the generic `optional`/`composite` recursion and the `loseLife` case at line 924, respectively). The new parser rule only recognizes a new *textual shape* composing two already-projected primitives — initiative 03's projection-decision requirement (new PortGraph case / coarse-projection justification) does not apply.

## Glossary gaps

None found — "optional", "life loss", "that player" are all pre-existing, already-modeled concepts; no new terms introduced.

## Process notes

- Ran the full CORE ring (`dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj`) on the branch's own worktree (`.claude/worktrees/agent-a745b7cb9a0eef221`, already checked out at `295f3506`): 5670 passed, 0 failed.
- Ran the full bench suite (`dotnet test tools/bench/MagicAtlas.Bench/MagicAtlas.Bench.csproj`) on both the branch (73/73 pass) and, via a throwaway worktree at base `6fca3caf` with only the fixture+rule files patched in (pin left untouched), reproduced the exact single-combo regression the branch's re-pin describes — the strongest available confirmation that the re-pin is honest and minimal, not a broader or self-serving change.
- Checked the corpus for other cards matching the new rule's bare "you may have that player lose N life" pattern (Blood Seeker, Quest for the Nihil Stone): neither has an existing gold fixture, so the new rule cannot silently reclassify a previously-correct gold. Bloodchief Ascension's fixture (the sibling "with follow-up" shape) is untouched by the diff and its rule (`MayHaveThatPlayerLoseLifeYouGainRule.cs`) is unaffected — regex anchoring (`^...$`) makes the two rules mutually exclusive by construction.
- The one FAIL is a doc-comment citation defect only (wrong CR rule referenced for the "bare may" semantics) — the AST shape, the fixture correction, and the bench re-pin are all independently verified correct and require no change. Fix is a one-line comment edit; does not require touching the modeled effect, the fixture, or the pin.
