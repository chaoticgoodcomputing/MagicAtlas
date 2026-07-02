# MAST judge — batch 23 verdict

**Date:** 2026-05-26
**Scope:** 8 files (6 fixtures, 1 AST node, 1 parser pre-filter)
**Result:** PASS

## Summary

- PASS: 8
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

### AST / parser surfaces

- `libs/magic-ast/AST/Effects/Keyword/UnearthEffect.cs` — PASS. Models Rule **702.84a** (Unearth as activated ability with mana-cost parameter). Doc-comment quotes the subrule near-verbatim; cost-only field shape (`Cost: ManaCost`) mirrors KickerEffect/BestowEffect/EchoEffect and stays descriptive — no engine-flavored fields for the rule's return-from-graveyard, haste-grant, or exile-at-end-step semantics. `[OracleEffect("unearth")]` discriminator matches the rule's terminology word-for-word. Polymorphic `Cost` base accommodates future variants without committing to non-mana shapes today (intentionally cautious, consistent with the family).

- `libs/magic-ast/Parsing/ClauseSplitter.cs` (`IsManaSymbolReminder` + call site) — PASS. Cites Rule **107.4** in doc-comment; rule text confirms `{W/U}…{G/U}`, `{W/P}…{G/P}`, `{2/W}…{C/G}`, and `{S}` all fall under 107.4. Regex `^\s*\(\s*\{[^}]+\}\s+can\s+be\s+paid\s+with\b[^)]*\)\s*\.?\s*$` is shape-anchored (requires `(` then `{...}` then literal "can be paid with") and confirmed via spot-check NOT to match Echo, Bestow, Persist, or Flanking reminders (all of which open with non-brace text). Drop-the-clause behaviour is descriptive-doctrine pure: the mana symbol's payability is already structurally captured on `ManaCostAttribute.Symbols` (hybrid → `Kind: "hybrid"`, Phyrexian → `IsPhyrexian: true`); the reminder line is cosmetic gloss.

### Fixtures — Family A (Unearth, Rule 702.84)

- `tests/magic-ast-tests/Data/HandParsedCards/ALA/DregscapeZombie.json` — PASS. Mono `Unearth {B}` modelled as `StaticAbility { KeywordSource: "Unearth", Effects: [{ EffectType: "unearth", Cost: ManaCost{B} }], Reminder: {...} }`. No engine fields; gold-modeled reminder kept on the ability per convention. Matches 702.84a literally.

- `tests/magic-ast-tests/Data/HandParsedCards/ALA/EtheriumAbomination.json` — PASS. Three-symbol `Unearth {1}{U}{B}` with correctly-ordered symbol list (generic 1, U, B). Same descriptive shape; no unparsed nodes.

- `tests/magic-ast-tests/Data/HandParsedCards/CON/FireFieldOgre.json` — PASS. Sibling abilities cleanly separated: First strike (`combatDamageTiming: "First"`) and Unearth `{U}{B}{R}` each as their own `StaticAbility`. Per-line convention (`feedback_mast_multi_effect_per_clause`) respected because each line is a separate ability.

### Fixtures — Family B (mana-symbol reminder skip, Rule 107.4)

- `tests/magic-ast-tests/Data/HandParsedCards/RAV/BorosRecruit.json` — PASS. Oracle has `({R/W} can be paid with either {R} or {W}.)\nFirst strike`; gold has exactly one ability (First strike) — reminder is dropped. `ManaCostAttribute.Symbols[0]` is `Kind: "hybrid", Colors: ["W","R"]`, so the payability structure is preserved where the rule (107.4e) puts it.

- `tests/magic-ast-tests/Data/HandParsedCards/NPH/PorcelainLegionnaire.json` — PASS. `{W/P}` Phyrexian reminder dropped; gold has only First strike. `ManaCostAttribute.Symbols[1]` carries `IsPhyrexian: true` (107.4f).

- `tests/magic-ast-tests/Data/HandParsedCards/NPH/ThunderingTanadon.json` — PASS. Double `{G/P}{G/P}` reminder dropped; gold has only Trample. Both Phyrexian symbols on `ManaCostAttribute` carry `IsPhyrexian: true`.

## Glossary gaps

None. `Unearth` is in `glossary.json` and cites 702.84 verbatim. Mana symbols (hybrid, Phyrexian, snow) are all defined in 107.4 subrules a–h; no missing terminology.

## Process notes

- Rule-citation discipline restored. Family A cites 702.84 (Unearth); Family B cites 107.4 (Mana symbols). Both verified against `rules-structure.json` before render. This is the discipline the last three batches' HALTs flagged was missing.
- Regex safety: I confirmed the pattern matches `{S}` ("snow mana from a snow source") in addition to the briefing's hybrid/Phyrexian examples. That's intentional and correctly cited under 107.4h. No corpus-coverage concern surfaces.
- The `Reminder` field on Unearth fixtures retains the parenthetical reminder text on the ability — this is the established convention for keyword-with-reminder cards (Echo, Bestow, Persist all do the same). The Family B drop is different: those reminders attach to *no* ability, so there is nowhere to hang them.
- Descriptive doctrine (`feedback_mast_describes_not_executes`) preserved across both families. UnearthEffect carries only `Cost`; the mana-symbol filter is a no-emit operation that defers the payability structure to where the rule actually puts it (the mana cost itself).
- 570 → 582 with 6 new fixture tests and 6 fixture-driven parser regressions, zero baseline regressions — full-suite green per orchestrator brief. No verdict-affecting issues.

---

PROCEED
