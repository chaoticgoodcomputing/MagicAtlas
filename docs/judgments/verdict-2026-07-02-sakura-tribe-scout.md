# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** sakura-tribe-scout
**Branch:** mast-tdd/2026-07-02-sakura-tribe-scout
**Scope:** 1 fixture (+ its activated-ability parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SOK/SakuraTribeScout.json` — PASS. Oracle text
  matches oracle-cards.json verbatim ("{T}: You may put a land card from your hand onto the
  battlefield."). Modeled as `activated` with `Costs:[tap]` and a single effect
  `optional → putFromHandOntoBattlefield{Filter: CardTypes:[land], Zone:Hand, Controller:You}`.
  The "You may" is a composable `OptionalEffect` wrapper (not baked into the effect); {T} is a
  separate cost node so no timing is conflated into the effect (CR 602.1a cost/effect split). The
  effect is descriptive, not executive — it carries no land-drop-limit logic, consistent with
  CR 305.4 ("put lands onto the battlefield ... isn't the same as 'playing a land'") vs the
  CR 116.2a special action it is NOT. Optional `Tapped`/`AttackingThatOpponent` fields are correctly
  absent. No unparsed/free-text residual; filter fully structured. No regression: new single-ability
  card, all attributes (manaCost {G}, colors, colorIdentity, 1/1 stats, Snake Shaman Scout subtypes)
  present and correct.
- `mast-tdd/2026-07-02-sakura-tribe-scout#projection` — PASS. Branch introduces no new discriminator:
  `putFromHandOntoBattlefield` already exists on the base SHA (discriminator-baseline.json + the AST
  node), so the exhaustiveness ratchet does not fire and no new projection decision is required. The
  pre-existing `known-coarse-projections.json` entry ("hand-to-battlefield zone change ... no
  interaction flow rule reads hand-to-battlefield cheat effects yet; consciously inert for recall")
  is a defensible coarse choice — a land ramp/cheat effect no current flow rule consumes.

## Glossary gaps

(none)

## Process notes

- CR citations in the new parser rule doc-comment (CR 305.4, CR 116.2a, CR 602.1a) were all
  cross-referenced against rules-structure.json — each exists and its text matches the modeling.
- This branch's parser adds an activated-ability variant of the existing triggered
  `PutFromHandOntoBattlefieldEffect`; the fixture exercises only the land case. Parser/code-quality
  concerns are out of judge scope (green NUnit + code review own those); this verdict is
  rules-accuracy only.
