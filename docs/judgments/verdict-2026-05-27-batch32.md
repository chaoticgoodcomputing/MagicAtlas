# MAST judge — batch verdict

**Date:** 2026-05-27
**Scope:** 5 files (3 fixtures, 2 AST/parser surfaces — `KeywordDefinitions.Fear`, `OracleParsers.Fear`; `EvasionEffect` doc-comment touch)
**Batch:** 32 — Family A (Fear keyword)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## Doctrinal scrutiny — Characteristics-stretch (Option 1)

The briefing explicitly asks the judge to rule on whether using `Characteristics: ["artifact", "black"]` in `ObjectFilter` — encoding a card type and a color as entries in the Characteristics list — is doctrinally acceptable, or whether it should be flagged for escalation to Option 2 (filter-level disjunction).

**Verdict: ACCEPT as Option 1.** Doctrinally clean enough to pass on the merits stated below. Process note flagged for future escalation.

**Why it passes the rules-accuracy bar:**

1. **CR 109.3 sanctions the terminology directly.** The Comprehensive Rules text reads: *"An object's characteristics are name, mana cost, **color**, color indicator, **card type**, subtype, supertype, rules text, abilities, power, toughness, loyalty, defense, hand modifier, and life modifier."* Both "artifact" (card type) and "black" (color) are characteristics by the CR's own enumeration. Calling them "Characteristics" in the AST is *not* a terminology drift — it is precisely the rules-text vocabulary.

2. **Flying precedent is exact, not analogical.** Flying's emit shape is `Characteristics: ["flying", "reach"]` — keyword-ability names treated as a disjunction. CR 109.3 lists "abilities" as a characteristic on the same footing as "card type" and "color." Both uses are pulling from the same enumerated field of CR 109.3. No new doctrinal axis is being introduced.

3. **The 702.36b disjunction is recoverable.** Rule 702.36b says *"can't be blocked except by artifact creatures and/or black creatures."* The consumer-side semantic — "blocker passes if it has any listed characteristic AND is in the listed card types" — is a sound reading of the gold shape. The "and/or" in the rule is exactly the disjunction Characteristics encodes.

4. **Descriptive principle preserved.** MAST records the keyword's presence and the rule-grounded filter shape; engine evaluates the filter at declare-blockers (Rule 509). No engine-flavored axis was added (see memory item `feedback_mast_describes_not_executes`).

**The narrow concession (flagged, not blocking):**

The pragmatic stretch is that `ObjectFilter` has structured `CardTypes` and `Colors` fields *in addition to* `Characteristics`. Encoding "artifact" via `Characteristics` rather than `CardTypes`, and "black" rather than `Colors`, loses the type-vs-color discrimination on the field level. A purist refactor (Option 2) would emit `Or: [{ CardTypes: ["artifact", "creature"] }, { Colors: ["B"], CardTypes: ["creature"] }]` — filter-level disjunction.

That's an architectural expansion of `ObjectFilter`, and the briefing correctly judged it out of scope for a 1-family batch. The Flying precedent justifies Option 1 today; the third user beyond Flying + Fear should trigger reconsideration. Flagged in process notes.

## PASS verdicts

- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (Fear entry) — PASS. `Name = "Fear"`, `RuleReference = "702.36"`, `Category = Static`, `HasParameter = false` all match CR 702.36a–c. CreateExpansion emits the gold shape verbatim. Citation precise to the parent rule (702.36); the relevant subrule for block-restriction text is 702.36b, but parent-rule citation here is appropriate because the keyword *is* the parent rule — 702.36a (evasion classification), 702.36b (block restriction), 702.36c (redundancy) all describe facets of "Fear" as a unit.
- `libs/magic-ast/Parsing/Combinators/OracleParsers.cs` (Fear combinator + `.Or(Fear)` chain entry) — PASS. Parameterless `Keyword("Fear")` matches CR 702.36a's classification as a static evasion ability; reminder-text capture preserves the oracle-text disjunction verbatim.
- `tests/magic-ast-tests/Data/HandParsedCards/UDS/SquirmingMass.json` — PASS. Gold AST matches briefing shape; reminder text identical to oracle; `Kind: "static"`, `KeywordSource: "Fear"`, `EffectType: "evasion"`, `CanBeBlockedBy.{CardTypes, Characteristics}` all conform to CR 702.36b's "artifact creatures and/or black creatures" disjunction.
- `tests/magic-ast-tests/Data/HandParsedCards/10E/SeveredLegion.json` — PASS. Identical shape; vanilla-Fear card, no sibling abilities to scrutinize.
- `tests/magic-ast-tests/Data/HandParsedCards/9ED/RazortoothRats.json` — PASS. Identical shape; vanilla-Fear card.

## FAIL verdicts

None.

## Glossary gaps

None. `Fear` is in `glossary.json`: *"A keyword ability that restricts how a creature may be blocked. See rule 702.36, 'Fear.'"* `Characteristics` is in `glossary.json` and points to CR 109.3. Both terms used by the batch are covered.

## Process notes

1. **Characteristics-stretch is now established convention with two users (Flying, Fear).** If a third keyword arrives that needs filter-level disjunction *and* its disjuncts cross the type/color/ability axes in a way that makes the Characteristics-list reading semantically lossy (e.g., a hypothetical "can't be blocked except by creatures with flying and/or blue creatures" where the disjunction mixes ability-names with colors), escalate to Option 2 (`ObjectFilter.Or` disjunction) before adding the third user. Today's two users are doctrinally consistent — both pull from CR 109.3's enumerated characteristics — but the convention does not generalize cleanly to *arbitrary* predicate disjunctions.

2. **Reminder-text variance spot-check.** All three fixtures use the identical reminder text *"(This creature can't be blocked except by artifact creatures and/or black creatures.)"* — matches the briefing's pre-merge confirmation. No variant Fear reminders observed.

3. **CR 702.36c redundancy clause.** Multiple instances of Fear are redundant per 702.36c. None of the three fixtures stacks Fear, so the redundancy semantic is not exercised by this batch. Consistent with descriptive doctrine: the AST records keyword presence, the engine handles redundancy.

4. **No `unparsed` nodes anywhere in the three fixtures.** Verified.

5. **`EvasionEffect` doc-comment.** Already lists Fear in its covered-keywords list (line 12 of `EvasionEffect.cs`). No doc-comment edit needed for this batch — the family extension is purely additive via the new `KeywordDefinition` and parser combinator.

---

PROCEED
