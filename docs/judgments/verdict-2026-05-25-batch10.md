# MAST judge — batch 10 verdict

**Date:** 2026-05-25 **Result:** PASS (8 PASS / 0 FAIL)
**Scope:** 4 fixtures, 1 new AST type, 3 parser surfaces, 1 incidentally-passing fixture (Coliseum Behemoth — existing infra).

## PASS
- `CantBeCastEffect` (Rule 601.5) — parameterless effect, filter on `AffectedObjects`.
- `TriggeredAbilityParser.TryParseAddManaEffect` (Myr Moonvessel).
- `StaticAbilityParser.TryParseTribalAnthemModifyPT` + `ClassifyGrantTarget` "[Subtype]s you control" form (Sachi).
- `StaticAbilityParser.TryParseCantBeCastRestriction` (Gaddock Teeg).
- Fixtures: `5DN/MyrMoonvessel`, `CHK/SachiDaughterOfSeshiro`, `EOC/ColiseumBehemoth`, `LRW/GaddockTeeg` — all PASS.
- **Coliseum Behemoth incidentally green** — first batch where existing infra (Trample keyword + ETB triggered self-by-type + ModalEffect from Batch 3) fully covered a card. Validation of the descriptive AST design.

## Closing
**Verdict: PROCEED.**
