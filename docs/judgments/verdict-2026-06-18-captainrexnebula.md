# MAST judge — batch verdict

**Date:** 2026-06-18
**Scope:** 4 items (1 fixture, 1 AST node, 1 parser rule, 1 projection decision) — Captain Rex Nebula slice
**Result:** FAIL

## Summary

- PASS: 2
- FAIL: 2

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Activated/Rules/AnimateTargetIntoVehicleWithCrewAndCrashLandRule.cs
**Verdict:** FAIL
**Issue:** Doc-comment cites the wrong Comprehensive Rules number for "sacrifice".
**Rule citation:** CR 701.16 (cited) vs CR 701.21 (correct).
**Rule text:** > CR 701.16 = "Investigate"; CR 701.21 = "Sacrifice" (confirmed in rules-structure.json and glossary.json: "Sacrifice ... See rule 701.21").
**What the AST says:** doc-comment inline comment — `// ... (CR 706 result; CR 701.16 sacrifice; CR 119 damage).`
**Why this misrepresents the rule:** CR 701.16 is the Investigate keyword action, not Sacrifice. A cited rule whose text contradicts what the node models is a citation FAIL per the judge doctrine (absent-from-data / contradiction). The anchored full-text rule shape itself is fine — it mirrors the accepted `AnimateTargetNoncreatureArtifactByManaValueRule` anchored pattern and is not over-fit (the card is genuinely unique in the corpus); the defect is solely the miscited rule number.
**Suggested fix:** Change `CR 701.16 sacrifice` to `CR 701.21 sacrifice` in the doc-comment. (Also note "CR 119 damage" should be CR 120 — the damage section — though that is secondary; the load-bearing FAIL is the 701.16 contradiction.)

### libs/mast-interaction/PortWalkProjection.cs#becomesPermanent
**Verdict:** FAIL
**Issue:** The new `becomesPermanent` effect discriminator has no PortWalk projection decision at all.
**Rule citation:** n/a (initiative-03 projection-presence requirement).
**What the data says:** `[OracleEffect("becomesPermanent")]` is registered (extends `ContinuousEffect`, same base the registry keys as `EffectType`), but `becomesPermanent` appears in NEITHER `PortWalkProjection.EffectTypes` (semantic projection) NOR `libs/mast-interaction/known-coarse-projections.json` (justified-coarse whitelist) — verified at the branch tip and at the merged HEAD. The sibling `becomesCreature` IS present (whitelisted coarse, "baseline coarse fallback ... no flow rule consumes it yet").
**Why this misrepresents the rule:** The dispatch asks the judge to confirm the projection decision is present AND sensible. There is no decision present to evaluate. Whether this means the exhaustiveness ratchet (`PortWalkExhaustivenessTests`) is RED (and the batch should not have landed) or there is a registry blind spot, the projection item cannot PASS on absence.
**Suggested fix:** Add `becomesPermanent` either as a semantic `PortWalkProjection.EffectTypes` entry (if a flow rule would read a continuous animate) OR as a justified named entry in `known-coarse-projections.json` mirroring the `becomesCreature` carve-out (a continuous-effect characteristic change is plausibly inert for interaction recall today — coarse is defensible — but it must be named). Then re-run the exhaustiveness ratchet.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Modification/BecomesPermanentEffect.cs` — PASS. New-vs-reuse decision is rules-correct: CR 301.7 (+301.7a) confirms a Vehicle isn't inherently a creature, so "becomes a Vehicle artifact" needs a node whose CardTypes box omits "creature"; reusing `BecomesCreatureEffect` (which asserts "creature", as the Karn comparison rule's `CardTypes:["artifact","creature"]` shows) would be a fidelity error. Field shape (Subject / Power / Toughness as Quantity / Colors / CardTypes / AddedSubtypes / GainedAbilities + inherited Duration) is grounded; cited CR 611.1, 301.7, 205, 202.3, 208, 113.6 all exist and match the modeling.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/CaptainRexNebula.json` — PASS. Oracle text matches `oracle-cards.json` verbatim. (a) trigger = beginning of combat on your turn (Event Part:Combat / Edge:Beginning / Whose:You); (b) becomesPermanent with CardTypes ["artifact"] + AddedSubtypes ["Vehicle"] (no "creature"), P/T = DerivedQuantity ManaValue (CR 202.3 / 208); (c) crew 2 as a static Crew keyword ability (CR 702.122); (d) "Crash Land" as a granted triggered ability (CR 113.6) with the GENERAL `DealsDamage` event (CR 120 — correctly NOT a combat-specific `DealsCombatDamage*`, since the oracle text has no "combat" qualifier) + Filter IsSelf; (e) effects = rollDie(6) then conditional(dieRollResult == ManaValue) whose Then is composite[sacrifice(Self), dealDamage(dieRollResult, AnyTarget, Source:Self)]; (f) no IUnparsed / unparsed nodes.

## Glossary gaps

None. `Vehicle` and `Crew` are both in glossary.json and both reference CR 301 / 702.122, consistent with the modeling.

## Process notes

- The parser rule lives under `Parsing/Parsers/Activated/Rules/` (not `Triggered/Rules/` as named in the dispatch). Verdict path corrected to the on-disk location.
- Item 3's anchored full-text approach is NOT the FAIL — it is consistent with the accepted anchored-rule precedent (`AnimateTargetNoncreatureArtifactByManaValueRule`). The only FAIL on item 3 is the `CR 701.16` (Investigate) miscitation for sacrifice; both FAILs are mechanical and low-effort to fix.
- The `becomesPermanent` projection absence may indicate the interaction-layer exhaustiveness ratchet did not see the new discriminator at merge time — worth confirming the ratchet test result, since the doctrine says the ratchet "enforces presence."

HALT: feat/mast-captain-rex-nebula
