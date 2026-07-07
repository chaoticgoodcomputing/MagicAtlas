# MAST judge — batch verdict (batch4-devotion, Ephara)

**Date:** 2026-07-07
**Branch:** mast-tdd/2026-07-07-devotion-ephara (base b6b3d402)
**Scope:** 3 surfaces (1 shared rule, 1 whitelist entry, 1 gold fixture)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/DevotionConditionalLoseCreatureTypeRule.cs` — PASS.
  Regex broadening is safe and CR-accurate (CR 700.5 / 700.5a / 205.1a).
- `tests/magic-ast-tests/Fixtures/whitelist-freetext.json#BNG/EpharaGodOfThePolis:OtherCondition` — PASS.
  Justified debt carve-out, structurally equivalent to existing Snake/Boromir OtherCondition entries.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/BNG/EpharaGodOfThePolis.json` — PASS.
  All three lines fully structured; line 3 is a complete triggered ability, not a lossy collapse.

## Detail

### 1. Regex-broadening / sibling-mislabel (the #1 recurring FAIL) — CLEARED

The added group is `(?:\s+and\s+(?<color2>white|blue|black|red|green))?` inserted between
`(?<color>...)` and `\s+is\s+less\s+than`. It is:

- **Optional and non-capturing** — for single-colour gods ("...devotion to white **is** less
  than five...") the token after the first colour is "is", not "and", so the group matches zero
  times and control falls straight through to `\s+is\s+less\s+than`. Heliod (W), Thassa (U),
  Erebos (B), Purphoros (R), Nylea (G) and every other mono god parse **byte-identically** to
  before.
- **Still fully `^...$`-anchored** and hemmed in on every side by god-specific literals
  ("As long as your devotion to", "is less than", "isn't a creature"). The only additional lines
  it now matches are exactly "...devotion to [color] and [color] is less than [N], [name] isn't a
  creature." — i.e. the ten BNG/JOU two-colour guild gods. No other "as long as … less than …"
  line can be mislabelled, because the second-colour alternation only fires on a literal
  " and [WUBRG-word]" wedged between the first colour and "is less than". No false positives.

### 2. Two-colour devotion — CR 700.5 — CORRECT

CR 700.5: "A player's devotion to [color 1] and [color 2] is equal to the number of mana symbols
among the mana costs of permanents that player controls that are [color 1], [color 2], or both
colors." The model records `DevotionQuantity { Colors: ["W", "U"] }` — a descriptive two-element
colour list — inside `QuantityComparisonCondition { LessThan, 7 }`. Both counted colours are
present, threshold "seven" → 7, operator LessThan. The "or both colors" counting semantics is
engine execution, not MAST's descriptive job. Surface colour order [W,U] matches the oracle
"white and blue". Matches the rule.

### 3. LoseTypeEffect / AsLongAsDuration / comparison — CR 205.1a / 604.3 — CORRECT

Line 2 → `StaticAbility{ Effects:[ LoseTypeEffect{ Subject:Self, LostType:"creature",
Duration: AsLongAsDuration{ Condition: QuantityComparison(...) } } ] }`. This is a proper
*composite*: the effect names the action (loseType), a separate `AsLongAsDuration` node carries
the "when/for-how-long", and a separate `QuantityComparisonCondition` carries the predicate — no
timing baked into the effect discriminator. CR 205.1a governs removing a card type ("isn't a
creature" ⇒ lose the creature card type). CR 604.3 (characteristic-defining abilities) also
exists and is consistent with the god type-defining ability; it is not cited by the node, so no
citation FAIL. The doc-comment's cited rules (700.5, 700.5a, 205.1a; 201.4 as an inline
self-reference aside) all exist in rules-structure.json and match the modeling.

### 4. Whitelist carve-out — JUSTIFIED, not hiding structure

New entry: `{ card: BNG/EpharaGodOfThePolis, sink: OtherCondition, tag: debt }` for line 3's
intervening-if "you had another creature enter the battlefield under your control last turn".
- An **equivalent debt entry genuinely exists**: `BNG/SnakeOfTheGoldenGrove` (OtherCondition/debt,
  intervening-if "tribute wasn't paid") and `BoromirWardenoftheTower` (OtherCondition/debt,
  "no mana was spent to cast it"). All three are intervening-if *history/state predicates* parked
  in the same OtherCondition bucket pending a structured node.
- The Ephara predicate is a genuine **last-turn history predicate** ("had another creature enter …
  last turn") for which no structured node exists yet — structurally the same class as Snake's
  payment-history predicate. Tagged `debt` (future-structurable), not silently dropped.
- Only the intervening-if is free text; the surrounding trigger and effect are fully structured.
  So it is **not hiding parseable structure** — it is a tracked, consistent carve-out.

### 5. Line 3 is NOT a lossy collapse — CONFIRMED

The upkeep ability is a **full triggered ability**:
`Kind:"triggered"` + `Trigger{Timing:"At", Event:{Part:"Upkeep", Edge:"Beginning"}}`
(unqualified upkeep = "each upkeep") + `InterveningIf{ConditionType:"other", ...}`
+ `Effects:[ drawCards{ Count: literal 1, Player: You } ]`.
No `IUnparsed`/`UnparsedEffect`/`Diagnostics`, no describe-vs-execute, no dropped sibling. The
only `Raw` fields in the fixture are verbatim-by-design (type line, mana cost, P/T). The draw is
fully modeled; only the intervening-if predicate is the whitelisted OtherCondition free text.

## Projection decision (initiative 03)

**N/A — no new discriminator.** The diff touches only an existing rule regex, a new gold fixture,
and a whitelist entry. Every AST node is reused (`DevotionQuantity` with its pre-existing plural
`Colors` list, `LoseTypeEffect`, `AsLongAsDuration`, `QuantityComparisonCondition`,
`drawCards` trigger). No new effect/cost type, trigger event, or restriction is introduced, so no
PortWalk projection entry is required and none is missing.

## Glossary gaps

None surfaced.

## Process notes

The line-3 intervening-if "you had another creature enter … last turn" is the third card
(after Snake, Boromir) to bank an OtherCondition/debt for a last-turn / cast-history predicate.
That's a recurring cluster the structured-condition-bucket initiative (PB-7) may want to promote
to a dedicated history-predicate node, but it is correctly out of scope for this devotion-status
family (line 2) and correctly parked as debt.

## Closing

**Result:** ALL PASS (3 PASS / 0 FAIL). Orchestrator may **PROCEED**.
