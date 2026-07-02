# MAST judge — batch verdict

**Date:** 2026-06-16
**Scope:** 1 file (1 fixture, 0 AST nodes)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WOE/HagOfNoxiousNightmares.json` — PASS. Models "Warlocks you control have menace" as a single `static` ability with one `gainAbility` effect over `Target.Kind: Each` filtered `Subtypes:["Warlock"], Controller:"You"`, granting a `static` ability with an `evasion` effect (`CanBeBlockedBy: CardTypes:["creature"]`, `MinimumBlockers: 2`, `KeywordSource: "Menace"`). Menace evasion + minimum-blocker count is grounded in CR 702.111a/702.111b ("a creature with menace can't be blocked except by two or more creatures") and CR 509.1; the count-not-filter shape matches the house convention (`MenaceKeyword`, GLOSSARY.md line 3447). Absence of `ExcludeSelf` is correct: the oracle text says "Warlocks you control," not "Other Warlocks," so Hag (itself a Warlock) grants menace to itself (CR 109.5 — "other" would be the only trigger for `ExcludeSelf`). Nothing dropped, nothing added vs the verbatim one-line card; `RawText` round-trips the Scryfall text exactly.

## Glossary gaps

(none — "menace" is in glossary.json, citing rule 702.111)

## Process notes

- **Data files relocated.** `rules-structure.json` and `glossary.json` are no longer at `tests/atlas-flow-test/Data/_03_Primary/Datasets/` (that Datasets dir is gone); they now live at `libs/mtg-rules/Data/_03_Primary/Datasets/`. SKILL.md's file quick-reference table is stale — surface to orchestrator. Judged against the relocated copies.
- **Dispatch citation correction (not a fixture FAIL).** The dispatch prompt cited "CR 702.110 Menace." In this corpus 702.110 is **Exploit**; **Menace is 702.111** (glossary.json points "Menace" -> 702.111, and rules-structure.json's 702.111 text is Menace). The fixture JSON carries no rule-citation field, so this does not affect the PASS — a missing citation does not block PASS, and the modeling is correct against 702.111b. Flagging so the orchestrator's brief uses 702.111 going forward.
- The "509.1b" the dispatch mentions resolves to 509.1 in this corpus (the menace/evasion example text lives in the 509.1 parent body rather than a 509.1b subrule, which here is the restrictions-check step). Still grounds the modeling.
