# MAST judge — batch verdict

**Date:** 2026-07-18
**Branch:** `mast-tdd/2026-07-18-granted-ability-span-equip` (base `87ad2052`)
**Scope:** 7 files (4 parser rules, 3 gold fixtures) — delta-judge, span-provenance QA (Error-check track)
**Result:** PASS

## Summary

- PASS: 7
- FAIL: 0

## The bug, restated

Four Equipment/anthem rules extracted a quoted granted-ability body (`"Whenever this
creature deals damage, ..."`) and built the inner clause's `SourceSpan` as
`TextSpan(0, body.Length)` — 0-based *within the substring*, disconnected from the
card's real oracle text. Since `OracleParser.StampProvenance` never recurses into
`GainAbilityEffect.GainedAbility` to correct it, and `PortGraph.cs`'s null-span
fallback never fires (the span is non-null, just wrong), every nested trigger/effect
span under a granted ability silently pointed at the wrong slice of the card's
oracle text.

## Fix verification

### (1) Is `clause.RawText.IndexOf(body, Ordinal)` safe?

Yes, for the shapes these four rules cover. `body` is the full quoted ability text
(typically 30-100+ chars — a whole activated/triggered-ability clause), and
`clause.RawText` is a single oracle-text line/clause, not the whole card. For the
string to be found at the wrong (earlier) offset, the exact body text — or a prefix
long enough to be found before the real quote — would have to already appear
verbatim in the clause's preamble (`"Equipped creature has "`, `"Other blue
creatures you control have "`, etc.), which doesn't happen in real oracle-text
templating. `EquippedCreatureHasTwoQuotedAbilitiesRule` goes further and explicitly
guards the two-body case: `body2`'s search is started at `body1OffsetInClause +
body1.Length`, so a duplicate substring between the two quoted bodies can't cause
`body2` to mis-resolve to inside `body1`'s span. The only latent residual risk (not
exercised by any of the three touched fixtures, and not a regression introduced by
this branch — the same limitation existed before) is if `StripReminderText` strips
parenthetical reminder text that lives *inside* the quoted body itself before
`body` is captured; `IndexOf` would then fail to find that stripped-down `body`
verbatim in the untouched `clause.RawText`, falling back to
`clause.SourceSpan.Start` (start of the whole clause) rather than the correct
interior offset. This is a plausible future edge case, not a defect in what's
shipped here — flagged as a process note, not a FAIL.

### (2) Are all 4 files internally consistent, and is the added file's bug real?

Yes on both counts. All four files apply the identical convention:
`clause.RawText.IndexOf(body, Ordinal)` → `clause.SourceSpan.Start +
Math.Max(offset, 0)` → passed through to `TryParseGrantedBody(body,
absoluteBodyStart)` → `SourceSpan = new TextSpan(absoluteBodyStart, body.Length)`.

`EquippedPTKeywordAndGrantedAbilityRule.cs` (the scope addition) was independently
read in full: prior to this branch it had the exact same
`TryParseGrantedBody(string body)` signature building
`SourceSpan = new TextSpan(0, body.Length)`, i.e. the identical bug, and the diff
applies the identical fix shape. This is a correctly-diagnosed, in-scope addition
(The Reaver Cleaver's rule), not scope creep.

A repo-wide grep confirms nine *other* rules (`AsLongAsControlledObjectsHaveAbilityRule`,
`AsLongAsStaticGrantRule`, `CreatureTokensHaveQuotedAbilityRule`,
`EnchantedHasGrantedTriggeredAbilityRule`, `EnchantedPTAndGrantedAbilityRule`,
`GrantMultipleLoyaltyAbilitiesRule`, `GrantedAbilityRule`,
`OtherCreaturesAreSubtypeAndHaveAbilityRule`, `SubtypeCreaturesHaveQuotedAbilityRule`)
still carry `TextSpan(0, body.Length)`. This is expected and not a fault of this
branch — it fixed exactly the rules whose gold fixtures required the fix, and the
remainder is legitimate follow-up work for a future batch, not a defect in this one.

### (3) Are the 3 corrected gold fixtures span-accurate against real oracle text?

Verified against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`
(Scryfall snapshot already in the repo) — all three fixtures' `Input.OracleText`
matches the real card text exactly, and every corrected span was checked
byte-for-byte:

- **The Reaver Cleaver** — `s[50:120]` = `"Whenever this creature deals combat
  damage to a player or planeswalker"` (trigger span, Start 50 Len 70); `s[121:155]`
  = `" create that many Treasure tokens."` (effect span, Start 121 Len 34). Both
  correct.
- **Thornbite Staff** — `s[23:26]`=`"{2}"`, `s[28:31]`=`"{T}"`,
  `s[32:75]`=`" This creature deals 1 damage to any target"` (body1's cost+effect);
  `s[82:106]`=`"Whenever a creature dies"`, `s[107:128]`=`" untap this creature."`
  (body2's trigger+effect). All five spans correct. The unmodified sibling
  `SourceSpan{Start:0,Length:129}` on the enclosing static ability confirms
  `clause.SourceSpan.Start` is already anchored to whole-card absolute offsets, which
  is the invariant the fix relies on.
- **Unctus, Grand Metatect** — `s[39:76]` = `"Whenever this creature becomes
  tapped"`, `s[77:111]` = `" draw a card, then discard a card."`. Both correct.

Only `SourceSpan.Start` values changed in all three fixtures; `Input.OracleText` and
every other field (discriminators, filters, targets) are untouched, so this is a
pure provenance fix with no collateral rules-modeling drift.

### (4) Any CR-incorrectness introduced?

None. No AST discriminators, effect types, or target filters were touched — only
`SourceSpan` provenance metadata. `OtherColorCreaturesGrantQuotedTriggeredAbilityRule`'s
doc-comment cites CR 611.3 (continuous effects granting abilities to other
permanents) and CR 109.5 (self-exclusion for "other"); both exist in
`rules-structure.json` and are pre-existing, unaffected by this diff, and consistent
with the modeling.

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/EquippedCreatureHasQuotedAbilityRule.cs` — PASS. Correct span-rebase convention, consistent with siblings.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/EquippedCreatureHasTwoQuotedAbilitiesRule.cs` — PASS. Correct span-rebase convention plus explicit duplicate-substring guard for body2; verified against Thornbite Staff.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/EquippedPTKeywordAndGrantedAbilityRule.cs` — PASS. Independently confirmed to carry the same bug and the same correct fix; verified against The Reaver Cleaver.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/OtherColorCreaturesGrantQuotedTriggeredAbilityRule.cs` — PASS. Correct fix; CR 611.3/109.5 citations valid; verified against Unctus, Grand Metatect.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/TheReaverCleaver.json` — PASS. Corrected spans byte-exact against Scryfall oracle text.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ThornbiteStaff.json` — PASS. Corrected spans byte-exact against Scryfall oracle text.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/UnctusGrandMetatect.json` — PASS. Corrected spans byte-exact against Scryfall oracle text.

## Glossary gaps

None — this batch touches provenance metadata only, no new terminology.

## Process notes

- Nine other rules still carry the same `TextSpan(0, body.Length)` pattern
  (`AsLongAsControlledObjectsHaveAbilityRule`, `AsLongAsStaticGrantRule`,
  `CreatureTokensHaveQuotedAbilityRule`, `EnchantedHasGrantedTriggeredAbilityRule`,
  `EnchantedPTAndGrantedAbilityRule`, `GrantMultipleLoyaltyAbilitiesRule`,
  `GrantedAbilityRule`, `OtherCreaturesAreSubtypeAndHaveAbilityRule`,
  `SubtypeCreaturesHaveQuotedAbilityRule`). Legitimate follow-up scope for a future
  Error-check batch — not a defect in this one.
- Latent edge case (not exercised, not a regression): if a future card's quoted
  granted-ability body itself contains reminder text that `StripReminderText`
  removes before capture, `IndexOf` on the untouched `clause.RawText` would fail to
  find the stripped-down `body` and silently fall back to `clause.SourceSpan.Start`
  (start of the whole clause) rather than the true interior offset. Worth a `-1`
  guard/assertion if a future card hits it, but out of scope for this delta.

## Verdict

**ALL PASS**
