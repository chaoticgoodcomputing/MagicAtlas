# MAST judge — delta verdict: mast-tdd/2026-07-18-tap-cost-span

**Date:** 2026-07-18
**Base:** 87ad2052fc075b80638c172d0df981a4b2a5ae4f
**Scope:** `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs` (offset-tracking fix) + 6 gold fixtures (MoxOpal, BloomTender, RiverchurnMonument, RubblebeltMaaka, ShiftingWoodland, WastelandViper)
**Result:** PASS

## Summary

- PASS: 8
- FAIL: 0

Track: Error-check (span-provenance QA), not rules-modeling. Nothing in this diff touches AST node shape, discriminators, or effect semantics — it fixes the absolute-position basis of `Cost`/`Effect` `SourceSpan`s when a clause carries a DashPrefix (ability word or printed flavor label). Judged as a pure correctness delta.

## (1) Offset-tracking logic

Walked `TryParse`'s control flow end to end:

```csharp
var offset = 0;
// paren-strip branch: does NOT touch offset (see (2) below)
if (dashPrefix is not null) {
  var emDashIndex = text.IndexOf('—');
  if (emDashIndex >= 0) {
    var afterDash = text[(emDashIndex + 1)..];
    var afterDashTrimmed = afterDash.TrimStart();
    offset += (emDashIndex + 1) + (afterDash.Length - afterDashTrimmed.Length);
    text = afterDashTrimmed;
  }
}
```

`offset` accumulates exactly two quantities: the index of the character immediately after the em-dash (`emDashIndex + 1`, i.e. everything up to and including the dash), plus the count of whitespace characters `TrimStart()` subsequently removes. That sum is precisely the number of characters missing from the front of `text` relative to `clause.RawText` (when paren-strip didn't also fire — see (2)). Nothing else can touch `offset`; there's no other assignment site.

Both consumers use it identically, added to `clause.SourceSpan.Start`:
- `costPartStart = clause.SourceSpan.Start + offset + (rawCostPart.Length - rawCostPart.TrimStart().Length)`
- `effectSpan.Start = clause.SourceSpan.Start + offset + colonIndex + 1`
- `effectSpan.Length = Math.Max(0, clause.RawText.Length - offset - (colonIndex + 1))` — this is the key fix beyond just adding `offset` to Start: subtracting `offset` from the length keeps the span's *end* pinned to the true end of `clause.RawText` (previously the length was computed without knowledge of the dash-strip, so on dash-prefixed clauses the old length formula would have run `offset` characters past the actual end of the text — the length fix is necessary, not just the start fix).

Downstream, `ParseCosts` derives each comma-split cost component's span from `costPartStart + cursor + leading`, where `cursor`/`leading` are computed purely from substrings of the (correctly re-based) `costPart`/`text` — so the fix at the two injection points propagates correctly to every per-component cost span without further changes needed there.

I hand-recomputed every span (Python, independent of the C# arithmetic) for all 6 corrected fixtures against their real `RawText` and got byte-for-byte agreement with the committed fixture values in every case (see (3)).

**Verdict: PASS.** The formula is correct and internally consistent between `costPartStart` and `effectSpan`.

## (2) Scoping to DashPrefix only — is the paren-wrap gap real?

The worker's claim is that extending `offset`-tracking to the paren-strip branch regresses 9 unrelated parenthetical-reminder mana-ability fixtures, and that DashPrefix + paren-wrap never co-occur, so scoping is safe. Verified by reading the classifier, not just trusting the claim:

- `IsParentheticalActivatedAbility(tokens, rawText)` (AbilityClassifier.cs:1638) requires `tokens.Count == 1 && tokens[0].Kind == ReminderText`, then strips outer parens and requires the **remaining inner text to match `^\{[^}]+\}:` immediately** — i.e. a cost symbol must be the very first thing inside the parens. There is no room for a `"Word — "` ability-word prefix inside the parens under this regex.
- `TryExtractAbilityWord(clause)` (AbilityClassifier.cs:1878) does an **exact-match** lookup of `clause.RawText[..emDashIndex].Trim()` against `_abilityWords`/`_printedLabels`. If the clause's raw text starts with `'('` (required for the paren-strip branch in `TryParse` to fire — `text.StartsWith('(') && text.EndsWith(')')`), the computed prefix is `"(Word"` (parens are not whitespace, so `.Trim()` doesn't remove them), which never matches a bare `"Word"` entry in either set. So `AbilityWord`/`PrintedLabel` (and therefore `DashPrefix`) is structurally `null` whenever the paren-strip branch can fire.

So the combination "clause wrapped end-to-end in parens" AND "clause carries a DashPrefix" is not just empirically rare — it is **impossible** given the current classifier's exact-match ability-word detection and the parenthetical-activated-ability regex. Scoping the offset fix to the DashPrefix path only, and reverting the paren-strip path to preserve its pre-existing (still out-of-scope, still not span-correct for its own reasons) behavior, leaves no real gap for any constructible card shape.

**Verdict: PASS.** No regression/gap; the scoping decision is correct and its rationale is verifiable in code, not just assumed.

## (3) Fixture-by-fixture oracle-text + span verification

Oracle text pulled from `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (not from memory, per corpus convention) and independently recomputed spans in Python against the diff's arithmetic. All 6 match exactly:

| Fixture | Ability line (within full multi-line RawText where applicable) | Cost span(s) → land on | Effect span → lands on |
|---|---|---|---|
| MoxOpal | `Metalcraft — {T}: Add one mana...artifacts.` | `{13,3}` → `{T}` | `{17,81}` → ` Add one mana of any color. Activate...` |
| BloomTender | `Vivid — {T}: For each color...` | `{8,3}` → `{T}` | `{12,73}` → ` For each color among permanents...` |
| RiverchurnMonument | 2nd line: `Exhaust — {2}{U}{U}, {T}: Any number...` | `{147,9}` → `{2}{U}{U}`; `{158,3}` → `{T}` | `{162,137}` → ` Any number of target players each mill cards...` |
| RubblebeltMaaka | `Bloodrush — {R}, Discard this card: Target...` | `{12,3}` → `{R}`; `{17,17}` → `Discard this card` | `{35,56}` → ` Target attacking creature gets +3/+3...` |
| ShiftingWoodland | 3rd line: `Delirium — {2}{G}{G}: This land becomes...` | `{78,9}` → `{2}{G}{G}` | `{88,169}` → ` This land becomes a copy of target permanent card...` |
| WastelandViper | 2nd line: `Bloodrush — {G}, Discard this card: Target...` | `{23,3}` → `{G}`; `{28,17}` → `Discard this card` | `{46,77}` → ` Target attacking creature gets +1/+2...` |

Every corrected Cost `SourceSpan` lands exactly on its cost symbol/component; every corrected Effect `SourceSpan` starts right after the colon and runs to the end of the clause's `RawText`, consistent with the pre-existing (unchanged) effect-span convention for non-dash-prefixed abilities.

The diff's full `--stat` touches only the parser file + these 6 fixtures — confirming the claimed 9 paren-wrap fixtures are untouched (out of scope, as intended).

**Verdict: PASS** on all 6.

## (4) CR correctness

This is a pure span/provenance fix — no AST shape, discriminator, or effect-semantics change. No new CR modeling is introduced. The surrounding (unmodified) code comments citing CR 207.2c (ability words — list includes metalcraft, bloodrush, delirium, vivid) and CR 702.177a (Exhaust: "[Cost]: [Effect]. Activate only once.") were spot-checked against `rules-structure.json` and are accurate; this diff doesn't touch or depend on any CR citation changing.

**Verdict: PASS.** No CR-incorrectness.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs#TryParse(offset tracking)` — PASS. Offset arithmetic is correct and consistently applied to `costPartStart` and `effectSpan` (both start and, critically, length).
- `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs#scoping-to-DashPrefix-only` — PASS. Paren-wrap + DashPrefix co-occurrence is provably impossible given `IsParentheticalActivatedAbility`'s regex and `TryExtractAbilityWord`'s exact-match lookup.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/MoxOpal.json` — PASS. Spans verified against real oracle text.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/BloomTender.json` — PASS. Spans verified against real oracle text.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/RiverchurnMonument.json` — PASS. Spans verified against real oracle text (multi-ability card, second line).
- `tests/magic-ast-tests/Fixtures/HandParsedCards/RubblebeltMaaka.json` — PASS. Spans verified against real oracle text.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ShiftingWoodland.json` — PASS. Spans verified against real oracle text (multi-ability card, third line).
- `tests/magic-ast-tests/Fixtures/HandParsedCards/WastelandViper.json` — PASS. Spans verified against real oracle text (multi-ability card, second line).

## FAIL verdicts

None.

## Glossary gaps

None — no new terminology introduced.

## Process notes

- The paren-wrap span bug (the 9 fixtures the worker chose not to touch) is a real, separate, still-open provenance gap for parenthetical-reminder-wrapped mana abilities (Sacred Foundry, Steam Vents, etc.) — out of scope for this delta but worth a follow-up ticket in the Error-check track, since those spans are presumably still off by the paren+internal-trim amount. Not a FAIL here because it's pre-existing behavior this branch correctly declines to touch mid-fix (touching it broke 9 golds, per the worker).
- Verified this is purely a provenance/telemetry fix (SourceSpan metadata), not an AST-shape or CR-modeling change — confirmed by the diff's `--stat` (parser + fixtures only, no `libs/magic-ast/AST/**` changes).

## Verdict

**ALL PASS**
