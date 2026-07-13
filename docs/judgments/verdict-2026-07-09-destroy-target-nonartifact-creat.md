# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** destroy-target-nonartifact-creat (branch `mast/destroy-target-nonartifact-creat`)
**Scope:** 3 files (1 fixture, 1 AST-node edit, 1 parser rule) + 1 projection item
**Result:** FAIL

## Summary

- PASS: 3
- FAIL: 1

## FAIL verdicts

### tests/magic-ast-tests/Fixtures/HandParsedCards/FUT/MagusOfTheAbyss.json
**Verdict:** FAIL
**Issue:** The trigger drops the "each player's" scope — `GameTime.Whose` is absent — so the gold is not eventual-truth, and both `ThatPlayer` back-references are left ungrounded.
**Rule citation:** CR 109.5 (player reference); CR 500-series (turn structure / `GameTime`)
**Rule text:** > CR 109.5 — the words that reference a player resolve to a specific player determined by context; here "that player"/"their" resolve to the player whose upkeep fired the trigger, which the trigger must name.
**What the fixture says:** `"Trigger": { "Timing": "At", "Event": { "Part": "Upkeep", "Edge": "Beginning" } }` — no `Whose`, while the effect carries `"Controller": "ThatPlayer"` and `"Chooser": { "Kind": "ThatPlayer" }`.
**Why this misrepresents the rule:** The card is "At the beginning of **each player's** upkeep." `GameTime.Whose` exists exactly for "whose upkeep," and every sibling upkeep fixture sets it — Aboroth/ElvishFarmer/WhipstitchedZombie "your upkeep" → `Whose:"You"`; Iron Maiden "each opponent's upkeep ... that player" → `Whose:"Opponent"` + `ThatPlayer`. "each player's" is expressible as `Whose:"Any"` (the every-player value in `ControllerFilter`). Omitting it (a) lossily drops a rules-load-bearing qualifier and (b) leaves the two `ThatPlayer` references (the antecedent for "that player"/"their") dangling with nothing in the AST to resolve them to. Iron Maiden is the exact structural twin and demonstrates the corpus expects `Whose` set here.
**Suggested fix:** Add `"Whose": "Any"` to the trigger's `Event` (GameTime), matching the Iron Maiden precedent for "each <X>'s upkeep" and grounding the `Controller:ThatPlayer` / `Chooser:ThatPlayer` back-references.

## PASS verdicts

- `libs/magic-ast/AST/Effects/ZoneChange/DestroyEffect.cs#Chooser` — PASS. Sound generalization: nullable `Chooser` records the "of their choice" reassignment of target selection away from the ability's controller (CR 115.1 / 601.2c default; CR 109.5 "that player"); `JsonIgnore` when null keeps existing golds byte-stable; cited rules exist and match.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/DestroyTargetNonartifactCreatureThatPlayerControlsRule.cs` — PASS. Fully-anchored regex emits the correct effect: creature + `ExcludedCardTypes:["artifact"]` (nonartifact, CR 110.4 negation axis), `Controller:ThatPlayer`, `Chooser:ThatPlayer`, `CantBeRegenerated` (CR 701.8a destroy, CR 701.19 regenerate). All cited CR rules exist and are non-contradictory. Emits only the effect, not the trigger, so it is not the source of the missing `Whose`.
- `mast/destroy-target-nonartifact-creat#projection` — PASS. No new PortGraph-relevant discriminator: `DestroyEffect` already projects, the added `Chooser` is inert metadata for flow reachability (who selects the destroy target does not alter ports/edges), and `ThatPlayer` is a reused `ControllerFilter`/`ObjectReferenceKind` value. No new `PortWalkProjection` case or `known-coarse-projections.json` entry is required; the existing projection is sensible.

## Glossary gaps

None. "Destroy", "regenerate", "target", "control" are all covered.

## Process notes

- `Input.OracleText` is byte-identical to `oracle-cards.json` for Magus of the Abyss (verified straight apostrophes in "player's" and "can't").
- The FAIL is on the fixture's trigger, not on the new parser rule or the `Chooser` field — both of those are sound. The trigger is produced by a separate parse path; the gold as committed does not represent eventual-truth because it lossily drops the "each player's" qualifier that anchors this card's `ThatPlayer` semantics.

HALT: mast/destroy-target-nonartifact-creat
