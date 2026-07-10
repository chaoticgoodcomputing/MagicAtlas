# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast-tap-unless-pay-carnophage
**Family:** tap-this-creature-unless-you-pay — "tap this creature unless you pay 1 life." on Carnophage (TMP)
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TMP/Carnophage.json` — PASS. Input.OracleText is byte-identical to the real card ("At the beginning of your upkeep, tap this creature unless you pay 1 life.", verified against oracle-cards.json). The trigger correctly decomposes timing (`Timing: At`, `Event{Part: Upkeep, Edge: Beginning, Whose: You}`) from the effect. The "unless you pay" gate is modeled structurally as `PreventableEffect` (discriminator "preventable") wrapping `Inner{tap, Target: Self}` with `Unless{Player: You, Cost: payLife 1 (literal)}` — no free text, no IUnparsed/UnstructuredEffect, no lossy drop/merge. Tap semantics match CR 701.26a; the cost-or-consequence gate matches CR 118.5 ("it's not automatically paid").
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TapSelfUnlessPayLifeRule.cs` — PASS. New [TriggeredRule] wired into the triggered effect-parse path; reuses existing AST nodes only (newAstNode=false) — TapEffect (`[OracleEffect("tap")]`), PreventableEffect (`[OracleEffect("preventable")]`), UnlessClause, PayLifeCost (`[OracleCost("payLife")]`) — producing a shape identical to the gold fixture. It is a faithful mirror of the established SacrificeSelfUnlessPayRule on the tap side. Cited rules cross-check clean: 701.26a Tap text matches verbatim; 118.5 contains the quoted "it's not automatically paid"; 109.2 self-reference exists. None contradicts the modeling.
- `mast-tap-unless-pay-carnophage#projection` — PASS. No new discriminator (effect/cost type, trigger event, or restriction) is introduced — the branch composes pre-existing tap/preventable/payLife/unless nodes — so no PortWalk projection decision (semantic or coarse) is required by initiative 03.

## Glossary gaps

(none)

## Process notes

- shared=[] confirmed: the diff touches only the new parser rule and the new fixture (no shared-node edits to review for over-generalization).
- Citation 118.5 is literally about {0}/reduced-to-{0} costs; the rule text nonetheless carries the general "paying a cost is not automatic" principle the doc-comment invokes, and it is used identically by the sibling SacrificeSelfUnlessPayRule. Supporting, not contradictory — not a FAIL. The load-bearing citation for the tap consequence is 701.26a, which is dead-on.

ALL PASS
