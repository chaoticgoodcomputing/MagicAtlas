# MAST judge — batch verdict

**Date:** 2026-07-18
**Scope:** Delta-judge of `mast-tdd/2026-07-18-granted-ability-span-aura` (base `87ad2052fc075b80638c172d0df981a4b2a5ae4f`) — 3 parser rule files + all 15 corrected gold fixtures
**Result:** PASS

## Summary

- PASS: 18
- FAIL: 0

## The delta questions, answered

**(1) Is `clause.RawText.IndexOf(body, Ordinal)` safe/correct — any risk of matching the wrong occurrence?**

Low risk, and not realized in this batch. `clause` is documented (and confirmed by reading `GrantedAbilityRule.cs`'s comment: "no in-line newlines reach this layer — clauses are split before us") to be a single already-split clause, i.e. one short sentence like `Enchanted creature has "..."` or `As long as <cond>, each of those creatures has "..."`. The `body` string is extracted from `clause.RawText` by the same regex that matched it, so it necessarily occurs at least once. `IndexOf` returns the *first* occurrence — a wrong-occurrence bug is only possible if the exact quoted-body text (or some prefix of it) coincidentally recurs **before** the quote mark, i.e. inside the short subject noun phrase ("Enchanted creature", "All Slivers", "White creatures you control", "As long as Tandem Lookout is paired..."). None of the 15 corrected fixtures — nor any plausible Aura/Equipment/Soulbond subject phrasing — exhibits this. `EnchantedHasGrantedTriggeredAbilityRule.cs` additionally guards a related, real hazard correctly: it matches its regex against `StripReminderText(clause.RawText)` but re-locates `body` in the **untouched** `clause.RawText` for the offset computation, explicitly to avoid a skew from reminder-text stripping. This is the sounder of two ways to do it.

**(2) Do all 3 rule files apply the identical, correct convention with no logic errors?**

Yes. `GrantedAbilityRule.cs`, `EnchantedHasGrantedTriggeredAbilityRule.cs`, and `AsLongAsStaticGrantRule.cs` all compute `bodyOffsetInClause = clause.RawText.IndexOf(body, Ordinal)` then `bodyAbsoluteStart = clause.SourceSpan.Start + (offset >= 0 ? offset : 0)`, and thread `bodyAbsoluteStart` (not `0`) into the inner `OracleClause.SourceSpan.Start` handed to `ActivatedAbilityParser`/`TriggeredAbilityParser`. The `>= 0` guard against a failed `IndexOf` (falls back to `clause.SourceSpan.Start` alone, i.e. the old bug's behavior only in the impossible case that `body` isn't found at all) is defensive and appropriate — it can't fire given `body` is derived by regex from the same string. `AsLongAsStaticGrantRule.cs`'s `TryParseGrantedActivatedBody` correctly threads the single rebased `bodyAbsoluteStart` through to whichever of the two inner parsers (activated-first, triggered-fallback) ultimately succeeds — confirmed empirically via TandemLookout, which takes the triggered-fallback path and still lands correct spans.

**(3) Spot-checks (target: at least 5, "some Auras, some Equipment/other, at least one not seen elsewhere") — did every corrected fixture actually land the right span?**

Computed and verified all **15 of 15** corrected fixtures by extracting the real `Input.OracleText` (or, for the DFC `ToralfGodofFuryToralfsHammer`, `CardFaces[1].OracleText`) and slicing it at each new `SourceSpan.{Start,Length}` to confirm byte-exact equality with the expected sub-text. Every one matched exactly. Full spread covered: Soulbond triggered (TandemLookout — the target card), Soulbond activated (DeadeyeNavigator), Aura bare-triggered-grant (Sunbond), Aura PT+activated-grant (GauntletsOfLight), Aura multi-clause activated-grant (CompulsoryRest, EpharasRadiance, SadisticObsession), Equipment activated-grant (UmbralMantle — includes a trailing reminder-text parenthetical, correctly not skewing the offset), Equipment on a DFC back face with an unattach-named cost (ToralfGodofFuryToralfsHammer), tribal/subtype grant with no Aura/Equipment subject at all (SachiDaughterOfSeshiro's "Shamans you control have...", TelekineticSliver's "All Slivers have...", CitanulHierophants' "Creatures you control have...", ResplendentMentor's "White creatures you control have..."), and land-typed grant (FindThePath's "Enchanted land has..."). No wrong offsets found anywhere.

**(4) Any CR-incorrectness from treating this as a blanket mechanical rebase — e.g. a genuinely-duplicated body substring?**

None found. All 15 fixtures' diffs are strictly `SourceSpan.Start`-only changes — no `EffectType`, discriminator, target, or cost-type field was altered by the rebase, so there's no risk of the mechanical fix silently changing *what* is modeled, only *where* it's anchored. No fixture's quoted body text recurs elsewhere in its own (pre-split, single-sentence) clause, so the `IndexOf`-first-occurrence behavior never actually had to choose between candidates in this batch. The theoretical hazard flagged in (1) — a body substring that legitimately repeats earlier in the same clause — remains a live, if narrow, future risk (e.g. a hypothetical "Whenever this creature deals damage, this creature deals damage again, that creature has \"...\"" construction), but it is not present in the 15 fixtures graded here and doesn't warrant blocking this batch.

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/GrantedAbilityRule.cs` — PASS. Correct span-rebase convention, CR 113.3 citation checks out.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/EnchantedHasGrantedTriggeredAbilityRule.cs` — PASS. Correct span-rebase convention (re-locates in untouched `clause.RawText`, not the reminder-stripped copy), CR 113.3/603.2 citations check out.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/AsLongAsStaticGrantRule.cs` — PASS. Correct span-rebase convention shared across activated/triggered dispatch branches, CR 702.95 citation checks out.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AVR/TandemLookout.json` — PASS. Spans byte-exact against oracle text (see above).
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AVR/DeadeyeNavigator.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/GauntletsOfLight.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/UmbralMantle.json` — PASS. Spans byte-exact; reminder parenthetical handled correctly.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ToralfGodofFuryToralfsHammer.json` — PASS. Spans byte-exact against the correct DFC back face.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/Sunbond.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/CHK/SachiDaughterOfSeshiro.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/EpharasRadiance.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/FUT/CompulsoryRest.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/CSP/ResplendentMentor.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/SadisticObsession.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/TSP/TelekineticSliver.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/UDS/CitanulHierophants.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/USG/HermeticStudy.json` — PASS. Spans byte-exact.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/FDN/FindThePath.json` — PASS. Spans byte-exact.

## Glossary gaps

None surfaced — this batch is a pure span-provenance fix with no new discriminators, effect types, or terminology.

## Projection decision (initiative 03)

Not applicable — this branch introduces no new discriminator (effect/cost type, trigger event, or restriction). It only corrects a `SourceSpan` computation; no new `PortGraph`/`PortWalkProjection` decision is required.

## Process notes

This is a pure provenance/QA fix (`SourceSpan` values only — verified via diff that no other field changed in any of the 15 fixtures) rather than a rules-modeling change, so the judging bar here is numerical/positional accuracy against the real oracle text rather than CR-fidelity of the AST shape. I computed all 15 (not just the required 5) fixtures' corrected spans against their real `Input.OracleText` programmatically and confirmed byte-exact matches in every case — no wrong-occurrence `IndexOf` failures materialized. The one narrow residual risk (a body substring that legitimately duplicates earlier in the same pre-split clause) is flagged for awareness but is not present in this batch and does not block.
