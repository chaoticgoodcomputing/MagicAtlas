# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** any-opponent-may-have-it-deal-5 (branch `mast/any-opponent-deal-5-damage`)
**Scope:** 3 files (1 fixture, 1 parser rule, 1 shared AST node) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/OTJ/LonghornFirebeast.json` — PASS. `Input.OracleText` is byte-identical to the Scryfall oracle for Longhorn Firebeast. Gold decomposes cleanly: `Trigger{Timing:When, Event:Enters, Filter:{creature, IsSelf}}` + one `optional` effect carrying `Chooser:{Opponent}` ("any opponent may"), `Inner: dealDamage 5 Source:It Target:Opponent` ("have it deal 5 damage to them"), and `IfYouDo: sacrifice Target:Self` ("If a player does, sacrifice this creature"). No `unparsed`/`unstructured` Kind, no free-text carrying structure, no lossy drop/merge. Timing (Trigger) and action (effects) are correctly separate composites. Recipient uses singular `Opponent` (not `EachOpponent`), matching "any opponent … to them." CR 118.12 (optional "may … if that player does"), CR 120.1 (source deals damage), CR 701.21a (sacrifice), CR 603.2 (triggered) all confirmed present and consistent.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AnyOpponentMayHaveItDealDamageThenSacrificeSelfRule.cs` — PASS. Anchored (`^…$`) single-rule matcher builds exactly the gold AST above. Doc-comment cites CR 118.12, 120.1, 701.21a, 603.2 — every one exists in `rules-structure.json` and its text matches the modeling. `IsCombat` left null (non-combat ping, correct — no "combat damage" marker in the text). Source `It` mirrors the established `ItDealsDamageToTargetTypeDisjunctionRule` convention.

- `libs/magic-ast/AST/Effects/Core/OptionalEffect.cs#Chooser` — PASS. Sound generalization of the shared node: adds nullable `ObjectReference? Chooser` with `null ≡ controller ("you may")` and `[JsonIgnore(WhenWritingNull)]`, so every existing "you may" fixture is byte-unchanged. Mirrors the separate-chooser concept already on `DiscardCardsEffect.Chooser` (verified present). The doc-comment also corrects the node's cited rule from CR 117.7 (which is about casting a spell "in response" — unrelated) to CR 118.12, the actual "[A player] may [do something]. If [that player] does, [effect]" rule — a citation improvement, not a regression.

- `libs/magic-ast/AST/Effects/Core/OptionalEffect.cs#projection-decision` — PASS. The branch introduces no new PortWalk discriminator: it reuses `optional`, `dealDamage`, and `sacrifice`, all of which already appear in `PortWalkProjection.EffectTypes` with semantic projections (optional → gated blink/Inner recursion; dealDamage → emit:damage; sacrifice → sac cost), plus the pre-existing `Enters` trigger event. The new `Chooser` is an attribute refinement on an existing effect, not a discriminator string, and does not alter the flow-relevant projection surface. No projection-file change is required and none is missing — the ratchet does not fire here.

## Glossary gaps

None. "sacrifice", "opponent", "deal damage", and the ETB trigger are all covered by rules-structure.json / standard glossary terms.

## Process notes

- Parser plumbing described in the dispatch (sentence-bundle splitter failing both halves, trimmed-interior fallthrough → single-rule loop) is parser correctness, out of judge scope; the batch's green NUnit run covers it.
- The `Chooser`/`Target` both being `Opponent` intentionally co-refer to the single choosing opponent ("any opponent … to them"); the schema has no linked-variable tie between them, but both are structured `ObjectReference`s (not free text) and the descriptive fidelity is intact — no action needed.
