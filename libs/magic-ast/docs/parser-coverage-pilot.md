# Parser-coverage fan-out pilot — disposition (2026-06-17)

A 6-batch AFK fan-out run of the `mast-tdd-loop`, targeting the cards that block the most popular
Commander Spellbook combos (the `InteractionTriage` worklist's `topComboBlockingCards`). This records
what landed, what the loop proved, and the disposition of the hard remainder. **Consolidated at batch 6
by decision** — the loop is validated and the remaining frontier needs dedicated surface work, not more
fan-out batch-cards.

## Outcome

| | |
|---|---|
| Batches | 6 (one calibration + 5 production) |
| Cards merged | **50** |
| `parseReady` combos | 1328 → **2191** (+863), of 91,795 total CSB |
| `bench:recall` | 73/73 every batch — zero combo-tier regressions |
| `nx run mast:test` | 4712 green (was ~4490 at start) |
| Materialized port graph | 2,889 → **8,210 ports**, 167k → **1.11M edges** (per `port-graph-metrics.json`) |

Per-batch: b1 calibration 7 merged · b2 10/10 PASS · b3 9 · b4 8 · b5 9 (+Nest redeemed) · b6 7.

## What the loop proved

- **The pre-dispatch oracle-fidelity gate works.** Seeding each gold `Input` from the corpus
  (`tools/seed-gold-input.py`) before dispatch eliminated the transcription-drift failure mode that the
  calibration batch surfaced (the in-worktree fidelity test skips — corpus absent — so drift otherwise
  only fails at the orchestrator's post-merge CORE gate). Residual drift modes still caught by judges:
  curly-vs-straight quotes (Deadeye), and they regen-fix in minutes.
- **The judge is the keystone — it caught every real defect:** shared-regex overfit ×4 (Mindcrank,
  Rings, Hapatra, + Orcish-adjacent), oracle-drift ×2 (Maddening kicker-reminder, Peregrin P/T,
  Deadeye quotes), dropped-qualifier ×2 (Nest -1/-1 type, Ulalek "other"), free-text residual ×2
  (Rings, One Ring), CR-citation ×1 (Orcish Amass). Redeemed: Peregrin, Maddening, Mindcrank, Deadeye,
  Orcish, Nest.
- **Hybrid serial-merge scales:** parser slices are mostly new-file/collision-free; the only conflicts
  are on generated artifacts (`ast-schema.json`, `known-coarse-projections.json`), resolved by
  regeneration in the integrated tree. Even shared-file edits (`TriggerCondition.cs`, `ObjectFilter.cs`,
  `AbilityClassifier.cs`) auto-merged when additive in different regions.
- **Per-combo bench reach 6 ≠ union-viz reach 5.** The reach constant is correctly split:
  `PortGraphEngine.DefaultReconstructionReach=6` for the tiny per-combo bench;
  `MaterializeCyclesStep` stays at 5 for the whole-corpus union (1.1M edges — length-6 enumeration is
  intractable there, exactly the graph-size × reach cost in `cycle-enumeration-acceleration.md`).

## The hard remainder (NOT done — needs dedicated work, not fan-out)

**Deferred after 2 FAILs each — need bespoke parser-surface design:**
- **Rings of Brighthearth** — "whenever you activate an ability… you may pay {2}: copy that ability."
  Needs a *copy-an-activated/triggered-ability* surface + a *conditional-pay* clause. Two attempts hit
  unanchored-regex overfit, then a free-text residual.
- **The One Ring** — needs an *intervening-if on a triggered ability* ("if you cast it"), *protection
  from everything*, and *burden-counter draw scaling*. Two attempts hit a wrong self-type filter, then a
  free-text `OtherCondition` residual.

**Carried FAILs — fixable, branches preserved (`mast-tdd/parse-hapatra-vizier`, `…-ulalek-fused-atrocity`):**
- **Hapatra, Vizier of Poisons** — gold is clean; the shared `TriggeredRuleHelpers.cs` change is an
  unanchored overfit that mislabels sibling corpus cards. Fix = anchor the matcher (same class as the
  Mindcrank/Rings overfit).
- **Ulalek, Fused Atrocity** — `CopyEffect` target filter drops the "other" qualifier (CR 109.5);
  fix = add `ExcludeSelf` to the copy filter (the established `ExcludeSelf` machinery).

**Bailed (no faithful surface — correct refusals, NO FALSE GREEN):**
- **Tainted Pact** — "repeat this process until…" iterative control-flow + cross-iteration name-uniqueness.
- **Twinflame** — per-target iteration (`ForEachTarget`) + plural-token anaphora ("those tokens"). (Strive itself is addable.)
- **Dualcaster Mage** — copy *a spell*; the deferred spell-copy arm (ADR Option B).

## Recommended next moves (in priority order)

1. **Reflection-seam refactors (`FANOUT.md §1.4`) to kill the overfit FAIL class.** The recurring
   shared-regex overfit (Mindcrank, Rings, Hapatra) is the predicted hazard. A `[QualifierAxis]` registry
   (one file per qualifier) and a `[ClassifierRoute]` registry would convert the collision-prone shared
   edits to new-file work — removing both the overfit risk *and* the serial-merge contention. Highest
   leverage; benefits every future card.
2. **Dedicated surface design for the copy-ability / intervening-if cluster** (Rings, One Ring, and the
   deferred spell-copy arm for Dualcaster). These are doctrinal AST shapes, not batch-cards — design once,
   then they (and their combos) open up.
3. **Land the 2 carried FAILs** (Hapatra anchor-fix, Ulalek `ExcludeSelf`) — quick, fold into (1)/(2).
4. **The 50 cards' new surfaces are now available** (devotion, play-from-top + alt-cost-life, escape,
   soulbond, additional-combat-phase, counter double/halve, -1/-1 counter triggers with type+minimum,
   token-copy, planeswalker loyalty, equipment-recursion, …) — future combos that reuse them will parse
   for free.

## Provenance / how to resume

- Worklist: `InteractionTriage` flow → `interaction-triage-report.json` `topComboBlockingCards`
  (popularity-weighted). Refresh: run the flow (bounded — kill after `MaterializeCardEdges`; the slow
  `MaterializeCycles`/viz tail is not needed for the worklist or `port-graph-metrics.json`).
- Harness: `tools/seed-gold-input.py` (pre-dispatch fidelity gate) + the slate-driven parser-batch
  workflow; per-batch slate is `{batch, slate:[{slug,name,combos,input,source,hint}]}`.
- Nothing is pushed — all 6 batches are committed on `feat/mast-improvements` for a human to push.
