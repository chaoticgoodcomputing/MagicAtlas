# MAST judge — batch 25 verdict

**Date:** 2026-05-26
**Scope:** 10 items (8 fixtures + 2 parser surface changes)
**Result:** PASS

## Summary

- PASS: 10
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

### Family A — ETB-create-Food-token

- `tests/magic-ast-tests/Data/HandParsedCards/ELD/FortifyingProvisions.json` — PASS. Two abilities: (1) static anthem `modifyPT` +0/+1 to creatures-you-control matches "Creatures you control get +0/+1." per Rule 613 (continuous effects on P/T); (2) triggered ETB → `createToken` with `Token: TokenDefinition.Food()` matches the predefined Food token (artifact, Subtypes: ["Food"], canonical activated ability captured as AbilityText) per CR 107.10b / dataset rule 111.10 ("A Food token is a colorless Food artifact token with '{2}, {T}, Sacrifice this token: You gain 3 life.'"). Self-filter `CardTypes: ["enchantment"]` matches "When this enchantment enters". Reminder text correctly emitted without outer parens per the triggered-ability reminder convention (`ExtractTrailingReminder`).
- `tests/magic-ast-tests/Data/HandParsedCards/ELD/FierceWitchstalker.json` — PASS. Two abilities: (1) static `trample` with `KeywordSource: "Trample"` matches Rule 702.19 with reminder text including outer parens (keyword convention); (2) triggered ETB Food token with self-filter `CardTypes: ["creature"]` matching "When this creature enters". Token definition consistent with Fortifying Provisions.
- `tests/magic-ast-tests/Data/HandParsedCards/SPM/HotDogCart.json` — PASS. Two abilities: (1) triggered ETB Food token, self-filter `CardTypes: ["artifact"]`; (2) activated mana ability `addMana` with `Costs: [{tap}], AnyColor: true, IsManaAbility: true` matches "{T}: Add one mana of any color." per Rule 605 (mana abilities). Both use pre-existing AST conventions.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/CreateTokenRule.cs` — PASS. Food-token branch dispatches to pre-existing `TokenDefinition.Food()` factory; no new AST types introduced; the canonical activated ability lives on `AbilityText` (descriptive — engine territory per `feedback_mast_describes_not_executes`). Doc-comment cites Rule 111 (general token-creation) and Rule 107.10b (predefined tokens); the latter matches live-CR numbering even though the bundled `rules-structure.json` is a stale snapshot where rule 107.10 covers Future Sight timeshifted frames. The glossary's predefined-token list in this dataset confirms "Food token is a colorless Food artifact token with '{2}, {T}, Sacrifice this token: You gain 3 life.'" — the parser's emission matches verbatim (with "this token" rewritten as "this artifact" on AbilityText, consistent with the Treasure factory's convention in the same file).

### Family B — Type-spell cost reduction

- `tests/magic-ast-tests/Data/HandParsedCards/TMP/RubyMedallion.json` — PASS. Static `costReduction { Amount: literal 1 }` with `AffectedObjects: ObjectFilter { CardTypes: ["spell"], Colors: ["R"], Controller: "You" }` matches "Red spells you cast cost {1} less to cast" per CR 117.6 (cost modification) and Rule 105 (color). "you cast" → `Controller: You`; "Red" → `Colors: ["R"]` (Rule 105.1).
- `tests/magic-ast-tests/Data/HandParsedCards/TMP/PearlMedallion.json` — PASS. Same shape as Ruby Medallion with `Colors: ["W"]`. Internally consistent.
- `tests/magic-ast-tests/Data/HandParsedCards/KLD/FoundryInspector.json` — PASS. Filter uses `CardTypes: ["spell", "artifact"]` for "Artifact spells you cast" per Rule 205.2 (card types). The dual-element `CardTypes` is the parser's emission convention for card-type filters (precedent: artifact-land at `["artifact", "land"]`); internally consistent with `BuildTypeSpellFilter` in `StaticAbilityParser.cs` lines 1100-1110.
- `tests/magic-ast-tests/Data/HandParsedCards/MOR/StinkdrinkerDaredevil.json` — PASS. Subtype-filtered: `Subtypes: ["Giant"]` with PascalCase per the Subtypes axis convention. `Amount: literal 2` matches "{2} less". Sibling card has no other abilities — clean fixture.
- `tests/magic-ast-tests/Data/HandParsedCards/KHM/StarnheimAspirant.json` — PASS. Subtype-filtered: `Subtypes: ["Angel"]`, `Amount: literal 2`. Matches "Angel spells you cast cost {2} less to cast" verbatim.
- `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (TryParseTypeSpellCostReduction + BuildTypeSpellFilter, lines ~156-165 dispatch, ~982-1118 implementation) — PASS. Filter classifier dispatches in priority order color → IsColorless → CardType → Supertype → Subtype, each rooted at `CardTypes: ["spell"]` per the convention. Colorless is correctly modeled via `IsColorless: true` rather than `Colors: ["C"]` (CR 105.1: "colorless is not a color"). Reduction emitted as `LiteralQuantity` on `CostReductionEffect.Amount`; no new fields added to `CostReductionEffect` (the static's `AffectedObjects` carries the "which spells" hook). Rule 117.6 citation matches the citation pattern of the existing `TryParseConditionalSpellCostReduction` (Mental Modulation, line 952) which has previously cleared judging.

## Glossary gaps

None. All terms appear in `glossary.json`:
- "Food" — present implicitly via the predefined-token glossary entry "A Food token is a colorless Food artifact token with…"
- "Trample" — Rule 702.19, present.
- "spell" / cost modification — present at Rule 601.2f-ish locations.

## Process notes

1. **Rules-numbering drift.** The bundled `rules-structure.json` predates the live CR's 107.10 / 117.6 numbering. In the dataset, predefined tokens live at rule **111.10** and cost-modification language at **601.2f** (and 702.51 Affinity for the closest analogue: "This spell costs {1} less to cast for each [text] you control"). The briefing's pre-verified citations (107.10b, 117.6) match the live CR, and the rule-text content is correct in either numbering. This drift was accepted in batch's prior `TryParseConditionalSpellCostReduction` (the line-952 citation of 117.6) and continues here for consistency. A future cleanup batch could refresh the dataset to the current CR snapshot.

2. **"spell" as a CardType.** Family B's filter uses `CardTypes: ["spell"]` (or `["spell", "artifact"]` for Foundry Inspector) as the root descriptor. Per CR 205.2, "spell" is not a formal card type — it's a stack-state designation (a card on the stack is a spell). The AST's emission is a descriptive convention: it marks "this filter applies to objects-as-spells" rather than mining a separate axis. Internally consistent (the parser always emits "spell" as the root, the fixtures match), and out-of-scope for the strict-typing critique that belongs in `docs/ast-engine-lens-audit.md`.

3. **Token-color omission.** `TokenDefinition.Food()` omits the `Colors` field (consistent with `TokenDefinition.Treasure()` and `TokenDefinition.Clue()` in the same file). The glossary explicitly says "colorless Food artifact token". Descriptive default is acceptable here — the absence of Colors on an artifact-only token correctly implies colorless. If a future engine pass needs explicit colorless tagging on tokens, that's a TokenDefinition-shape refinement, not a rules-accuracy concern.

4. **Worktree-path filesystem.** Final committed state on `main` verified clean: `git log` shows two merge commits (`9859c45` Family A merge, `dc07f5d` Family B merge); current branch matches expected state per the briefing. The "initial Write to main repo" gotcha noted in the briefing did not survive to the merged state. NUnit suite is 610/610 green per the dispatch.

5. **Streak continues.** Batches 23, 24, and now 25 have rendered PASS without HALT. Rule citations are accurate against the live CR; the dataset's numbering staleness is an artifact of the bundled snapshot, not a per-item failure.
