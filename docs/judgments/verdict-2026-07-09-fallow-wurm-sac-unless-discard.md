# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** fallow-wurm-sac-unless-discard
**Scope:** 2 files (1 fixture, 1 AST/rule node) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MIR/FallowWurm.json` — PASS. `Input.OracleText` is byte-identical to the real Mirage card ("When this creature enters, sacrifice it unless you discard a land card."), as are mana cost `{2}{G}`, type line, P/T 4/4, colors/identity `[G]`. Gold decomposes cleanly into `triggered` (Trigger `When`/`Enters`, Filter creature + `IsSelf`) plus a `preventable` effect: Inner `sacrifice` targeting `It`, `Unless` = You paying a one-card land `discard` cost. Timing is a separate Trigger node, not baked into the effect (composite shape). `Random` is correctly omitted (false) — CR 701.9b makes player-choice the default, so a land discard is player-chosen. No `unparsed`, no `OtherX`, no free-text `Characteristics`; the only `Raw`/`RawText` fields are verbatim-by-design.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SacrificeItUnlessDiscardLandRule.cs` — PASS. `[TriggeredRule]` anchored on `^sacrifice it unless you discard a land card$`, emits `PreventableEffect(SacrificeEffect{Target=It}, UnlessClause{Player=You, Cost=DiscardCost{Filter=land, Quantity=1}})`. Structurally parallels the already-landed sibling `SacrificeItUnlessDiscardRandomRule`, differing only in the land filter and leaving `Random` false. Doc-comment cites CR 701.21a (Sacrifice), 701.9a (Discard), 118.5 (paying a cost is not automatic) — all three exist in `rules-structure.json` and their text matches the cost-or-consequence modeling. No new AST node introduced (`newAstNode=false`).
- `mast/fallow-wurm-sac-unless-discard#projection` — PASS. The branch introduces no new discriminator: it reuses the existing `discard` cost type, `preventable`/`sacrifice` effect types, and the `Enters` trigger event; the `land` filter is a `CardTypes` value, not a new discriminator string. `shared=[]` and the diff touches exactly the fixture + the new rule file, so no PortWalk projection decision (`PortGraph` case / `PortWalkProjection` entry / `known-coarse-projections.json`) is required. Nothing parked as coarse that a flow rule would want.

## Glossary gaps

None. "Sacrifice" and "discard" are standard glossary terms; no novel MTG term is introduced.

## Process notes

Verified the sibling `SacrificeItUnlessDiscardRandomRule` is present at the base SHA, so the `discard`-cost `PreventableEffect` shape and its projection are already established; this branch is a clean parallel narrowing the discard filter to land. Cross-referenced all cited CR rules against `rules-structure.json`: 701.21a, 701.9a, 701.9b, 118.5 all present with matching text. Oracle text confirmed against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`.
