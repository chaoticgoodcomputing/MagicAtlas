# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** mistvein-borderpost
**Branch:** mast-tdd/2026-07-02-mistvein-borderpost
**Scope:** 1 fixture (delta-judge)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ARB/MistveinBorderpost.json` — PASS.
  The target line "You may pay {1} and return a basic land you control to its owner's
  hand rather than pay this spell's mana cost." is modeled as an `alternativeCosts`
  attribute whose `Cost` is a `composite` of a `mana` cost ({1}) plus a `returnToHand`
  cost targeting a basic land you control (`CardTypes:[land]`, `Supertypes:[Basic]`,
  `Controller:You`). This is the right discriminator and structure, faithful to the card,
  describe-not-execute, with no baked-in timing.

## Cross-references

- **Oracle text**: matches `oracle-cards.json` verbatim (mana `{1}{U}{B}`, type `Artifact`,
  and the three-line oracle body).
- **CR 118.9** (alternative cost): present in `rules-structure.json` — "Alternative costs
  are usually phrased, 'You may [action] rather than pay [this object's] mana cost' ...".
  Matches the modeling exactly.
- **CR 604.5**: present — "abilities that say ... 'You may pay [cost] rather than pay [this
  object]'s mana cost' ... work while a spell is on the stack." Justifies hosting the line
  as a card-level cost attribute rather than an oracle ability (mirrors Bestow's handling).

## Axis checks

- (a) **Structure correct** — `alternativeCosts` / `composite` / `returnToHand` are the
  correct nodes; filter captures "a basic land you control"; mana `{1}` parsed as generic 1.
- (b) **No free-text / unparsed residual** — `Oracle.Abilities` contains only the two real
  abilities; the cost line is skipped from abilities (ClauseSplitter) and re-expressed as a
  typed attribute. No `unparsed` node anywhere.
- (c) **No regression** — new fixture; siblings preserved and correctly modeled:
  "This artifact enters tapped." → `static` with `When: asThisEnters` + `tap` on `Self`
  (timing and effect kept as a composite, not swallowed); "{T}: Add {U} or {B}." → `activated`
  mana ability with `tap` cost, `addMana`, `IsManaAbility: true`.
- (d) **Citations valid** — both CR 118.9 and CR 604.5 exist in the rules data and match.

## Projection decision (initiative 03)

N/A — the branch introduces **no new discriminator**. `CompositeCost`, `AlternativeCost`,
`AlternativeCostsAttribute`, `ReturnToHandCost`, and the `composite` CostType all pre-exist at
the base SHA; the diff adds only narrow regex recognizers (AttributeExtractor / ClauseSplitter)
that map the Borderpost line onto existing typed cost nodes. No new `PortGraph` case or
`known-coarse-projections.json` entry is required.

## Process notes

The `returnToHand` cost implicitly returns to the owner's hand (the default destination),
matching "to its owner's hand". `SourceSpan` covers the parsed line. No glossary gaps.

**Result: ALL PASS**
