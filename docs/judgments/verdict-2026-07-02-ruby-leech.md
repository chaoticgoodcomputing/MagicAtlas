# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** ruby-leech
**Branch:** mast-tdd/2026-07-02-ruby-leech
**Scope:** 1 fixture (RubyLeech.json) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NEM/RubyLeech.json` — PASS. Oracle "Red spells you cast cost {R} more to cast." is modeled as a `Kind: static` ability with `EffectType: costIncrease`, `Amount: literal 0` (generic component) + `ManaSymbols: [{colored, R}]`, filtered by `AffectedObjects{ CardTypes:[spell], Colors:[R], Controller: You }`. The colored {R} increase is carried as a structured mana symbol rather than flattened to generic {1} — faithful to CR 601.2 (whose Altar's Reap example explicitly distinguishes {B} from {1}) and CR 601.2f (total cost = mana cost + cost increases). Describe-not-execute; no timing baked into the effect. The `First strike` sibling is preserved with the codebase-standard `combatDamageTiming`/`Timing: First` encoding (24 existing fixtures). No unparsed nodes, no free-text residual on any axis. Attributes (manaCost {1}{R}, colors R, colorIdentity R, 2/2) intact.
- `mast-tdd/2026-07-02-ruby-leech#projection` — PASS. The branch adds a `ManaSymbols` refinement field to the pre-existing `CostIncreaseEffect`; it introduces no new port-graph discriminator (effect/cost type, trigger event, or restriction). `costIncrease` is already parked in `libs/mast-interaction/known-coarse-projections.json` as a baseline coarse fallback ("no flow rule consumes it yet"). A pure cast-tax generates no resource, card, or trigger, so no flow rule would want it — the coarse choice is genuinely sensible, not an insensibly-parked live wire.

## Citation cross-reference

- **CR 601.2** — present in rules-structure.json; carries the Altar's Reap example ("Because a spell's total cost is 'locked in' before payments are actually made, you pay {B}, not {1}{B}...") that grounds the colored-vs-generic distinction. Matches modeling.
- **CR 601.2f** — present; "The total cost is the mana cost or alternative cost ... plus all additional costs and cost increases..." Matches the cost-increase concept.
- **CR 118.7** — present; "What a player actually needs to do to pay a cost may be changed or reduced by effects..." Supports the mana-component payment framing. The stale "Rule 117.6" cite from the old doc-comment was removed in this branch.

## Glossary gaps

(none)

## Process notes

Oracle text verified byte-for-byte against `oracle-cards.json` ("First strike\nRed spells you cast cost {R} more to cast.\n"). The `ManaSymbol` serialization `{Kind: colored, Colors: [R]}` in the fixture is consistent with the card's own manaCost attribute serialization in the same fixture.

ALL PASS
