# MAST judge — batch verdict

**Date:** 2026-07-07
**Batch:** batch2-untap
**Branch:** mast-tdd/2026-07-07-untap-artifact-creature (base 02bae0fd)
**Scope:** 2 targets (1 rule, 1 fixture)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Activated/Rules/UntapTargetCardTypeActivatedEffectRule.cs` — PASS. The regex now captures a compound card-type noun `(?<types>T(?:\s+T)*)` and emits an ordered conjunctive `CardTypes` list. Sensible per CR 205.1 (a permanent can carry multiple card types — its own examples are "artifact land creatures" / "artifact enchantment creature") and CR 205.1a/205.2a; activated-effect context matches CR 602.1.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/USG/VoltaicConstruct.json` — PASS. `{2}: Untap target artifact creature.` → activated ability, mana cost `{2}` (generic 2), reused `UntapEffect`, `Target` filter `CardTypes: ["artifact","creature"]`, `IsManaAbility: false`. Conjunctive filter, correct shape per CR 602.1 / 205.1a.

## Judgment against the named risks

**Regex-broadening sibling-mislabel risk (the #1 recurring FAIL class) — CLEARED.**
Old pattern captured a single `type`; new pattern captures `(?:T)(?:\s+(?:T))*` where `T` is `creature|artifact|enchantment|land|planeswalker|permanent|spell`. The rule stays end-anchored (`^…\s*\.?\s*$`, no `Multiline`), so the *entire* effect clause must be `Untap [another ]target <typeword>(<space><typeword>)*[.]`. Consequences:
- A disjunctive "artifact or creature" carries an explicit "or" (not a type word) → not matched, as the doc-comment claims.
- Any trailing qualifier ("... you control.", "... an opponent controls.") sits after the type words → `\s*\.?\s*$` fails → not matched.
- Non-adjacent "target" ("Untap two other target legendary creatures.", "Untap up to four target permanents.") → `target` doesn't follow the optional `another ` → not matched.
Empirical sweep of the fixture inputs and `oracle-text-quarantine.json` found the ONLY newly-matched line is the intended "Untap target artifact creature." Single-type lines still yield a one-element array (identical semantics). End-anchoring is sufficient because MTG templating never places adjacent bare card-type nouns except to denote a single multi-type object.

**Conjunctive vs disjunctive — CORRECT.**
`CardTypes: ["artifact","creature"]` is a single `ObjectFilter` listing both types = an object that is BOTH (conjunction), matching "artifact creature". CR 205.1's examples ("artifact land creatures", "artifact enchantment creature") confirm adjacent type nouns are conjunctive multi-type, not a disjunction. Disjunctive "artifact or creature" is deliberately excluded.

**Activated-ability shape — CORRECT (CR 602.1).**
Cost `{2}` mana + effect `untap`, written `[Cost]: [Effect]`. `IsManaAbility: false` is right — the ability adds no mana (CR 605.1a), it untaps a target.

**Escape hatches / unparsed / free text — NONE.**
No `unparsed` Kind, no `UnparsedEffect`, no `Diagnostics`, no describe-vs-execute string, no dropped sibling. The `Raw`/`RawText` fields are the standard verbatim fixture envelope (input mirror), not rules-bearing free text.

**Projection decision (initiative 03) — N/A, already satisfied.**
No new discriminator: the branch reuses the existing `untap` effect type. That type already has a semantic projection — `PortWalkProjection.cs:33` (`"untap"` → `emit:untap[:self]`) and `PortGraph.cs:753-765` emit `emit:untap` carrying the target filter as the port `Subject` (the code even cites Corridor Monitor's "untap target artifact or creature" renewing Kiki). So the conjunctive `CardTypes` filter this branch produces is consumed downstream; the projection is present and sensible.

## Citation cross-reference

- **CR 602.1** — exists; text matches verbatim the doc-comment quote ("Activated abilities have a cost and an effect… '[Cost]: [Effect.]…'"). Grounds the activated-ability shape. OK.
- **CR 205.1 / 205.1a** — exist. 205.1 (parent) directly supports multi-type conjunction via its own examples; 205.1b explicitly discusses "artifact creature". 205.1a is a card-type subrule and does not contradict the modeling. Per judge doctrine, parent-vs-subrule imprecision is not a FAIL — the concept is well-grounded. OK.
- **CR 205.2a** — exists; enumerates the true card types (note "permanent"/"spell" in the alternation are object/spell categories, not card types, but that is pre-existing and correct as filter words). OK.

## Glossary gaps

_none._

## Process notes

The `PortGraph` untap projection carries the target filter as the port Subject specifically for copy-inheritance tap-renewal (Decision 4b), so getting the conjunctive `CardTypes` right is load-bearing, not cosmetic — an extra reason the fixture's precise `["artifact","creature"]` is the correct gold.
