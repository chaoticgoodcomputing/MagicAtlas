# MAST judge — batch verdict

**Date:** 2026-07-18
**Scope:** 2 files (1 fixture, 1 parser rule) — delta-judge of `mast-tdd/2026-07-18-named-mode-span`
**Track:** Error-check (span-provenance QA)
**Base:** `87ad2052fc075b80638c172d0df981a4b2a5ae4f`
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/NamedModeGatedAbilityRule.cs` — PASS. The rebase
  formula `bodyOffset = clause.SourceSpan.Start + bodyGroup.Index + leadingTrim` correctly recovers
  `body`'s absolute offset in the card's oracle text.
  - Confirmed `ClauseSplitter.CreateClause` (`ClauseSplitter.cs:1139-1161`) stamps
    `clause.SourceSpan = TextSpan(startOffset, text.Length)` where `text == clause.RawText`
    verbatim, and `startOffset` traces back through `ProcessParagraph`/`Split(oracleText)` to an
    absolute index into the *full* oracle text (not paragraph- or line-relative) — so
    `clause.SourceSpan.Start` is a valid absolute anchor.
    `bodyGroup.Index` is `body`'s offset within `clause.RawText` (Regex match group index is
    always relative to the matched-against string), so the sum is body's real absolute start.
  - `leadingTrim = bodyGroup.Value.Length - bodyGroup.Value.TrimStart().Length` correctly
    compensates for `Trim()` eating any leading whitespace the `\s*` before `(?<body>.+)` left
    uncaptured — checked this is a no-op (0) for the em-dash-space-word shape actually seen on
    Phenomenon Investigators, and is a correct defensive term in general (no double-counting: it
    adds exactly the chars `Trim()` removed from the *front* of the captured group, not from the
    already-offset `clause.SourceSpan.Start`).
  - `Rebase()` is applied uniformly and only where warranted: the top-level re-parsed
    `TriggeredAbility.SourceSpan`, its nested `Trigger.SourceSpan`, and (only on the fallback path
    where `BuildModeEffects` returns null) each `Effect.SourceSpan` in `baseTrigger.Effects` — all
    using the same `bodyOffset`, since all of them were stamped relative to the same fresh
    `_bodyParser.Value.Parse(body)` call. Null propagates rather than fabricating a span.
  - No off-by-one or double-counting risk found: hand-verified against Phenomenon Investigators'
    real text below, exact character match.

- `tests/magic-ast-tests/Fixtures/HandParsedCards/PhenomenonInvestigators.json` — PASS. Verified
  `Input.OracleText` is byte-identical to Scryfall's `oracle_text` for Phenomenon Investigators
  (`tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`, id
  `57195155-6bdf-43c3-9f2e-11c31aef6b9c`):
  ```
  As this creature enters, choose Believe or Doubt.
  • Believe — Whenever a nontoken creature you control dies, create a 2/2 black Horror enchantment creature token.
  • Doubt — At the beginning of your end step, you may return a nonland permanent you own to your hand. If you do, draw a card.
  ```
  Recomputed real absolute offsets independently (Python, not from the code under test) and
  compared against every changed field:

  | Node | Field | Gold (new) | Real substring at that offset | Match |
  |---|---|---|---|---|
  | static preamble | SourceSpan | `{0,49}` (unchanged) | `As this creature enters, choose Believe or Doubt.` | yes |
  | Believe trigger | Trigger.SourceSpan | `{62,45}` | `Whenever a nontoken creature you control dies` | yes |
  | Believe ability | SourceSpan | `{62,100}` | `Whenever a nontoken creature you control dies, create a 2/2 black Horror enchantment creature token.` | yes, ends exactly at the `\n` before the Doubt bullet |
  | Believe ability | OracleLineIndex | `1` | 2nd `\n`-split line (0=preamble, 1=Believe bullet, 2=Doubt bullet) | yes |
  | Doubt trigger | Trigger.SourceSpan | `{173,33}` | `At the beginning of your end step` | yes |
  | Doubt ability | SourceSpan | `{173,115}` | `At the beginning of your end step, you may return a nonland permanent you own to your hand. If you do, draw a card.` | yes, ends exactly at end-of-text (len=288) |
  | Doubt ability | OracleLineIndex | `2` | 3rd line | yes |

  No span overlaps into the modal-choice preamble (`As this creature enters...`) or into the
  sibling bullet's text — Believe's span stops exactly at the `\n` before `• Doubt`, and Doubt's
  span runs exactly to end-of-string. `BuildModeEffects` produces the `createToken` /
  `optional(returnToHand, IfYouDo: drawCards)` effects directly with no `SourceSpan` field (matches
  the fixture, which has none on those nodes — consistent with the code comment that
  `BuildModeEffects`-constructed effects are never rebased because they were never stamped).

## Glossary gaps

None surfaced by this delta (no new terminology introduced — this is a pure span-provenance fix).

## Process notes

- Scope was intentionally narrow per the dispatch brief: judged only span-correctness, not the
  rule's overall AST shape (pre-existing and out of scope) or the linked-ability-variable gap in
  `ChosenModeCondition`/`chooseNamedOption` (also pre-existing, not touched by this branch).
- Noted in passing, NOT a FAIL and NOT introduced by this branch: `ClauseSplitter.SplitIntoParagraphs`
  (`ClauseSplitter.cs:947-962`) computes `paragraphStart` from the *untrimmed* line start but
  `paragraphText` from `line.Trim()` — if a future card's oracle text had a bullet line with
  leading whitespace, `clause.SourceSpan.Start` would be off by the leading-whitespace length. This
  file is unmodified by this branch and the premise holds exactly for all real Scryfall bullet
  lines observed (no leading whitespace), including Phenomenon Investigators — flagging only as a
  latent, pre-existing, unexercised edge case for awareness, not a defect of this delta.
- Only one fixture in the repo currently exercises `NamedModeGatedAbilityRule`
  (`ChosenModeCondition`/`chosenMode`), so there was no sibling gold to cross-check for consistency
  regressions.

## Verdict

**ALL PASS**
