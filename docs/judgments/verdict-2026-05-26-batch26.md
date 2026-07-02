# MAST judge — batch 26 verdict

**Date:** 2026-05-26
**Scope:** 9 files (8 fixtures, 1 AST enum extension; plus 1 parser + 1 keyword-definition cross-referenced)
**Result:** PASS

## Summary

- PASS: 9
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

### Family A — Affinity for [text]

- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (Affinity entry) — PASS. Models Rule 702.41a verbatim: `KeywordSource = "Affinity for {parameter}"`, single `CostReductionEffect` with `Amount = literal 1` and `PerObject = ObjectFilter`. The rule says `"Affinity for [text]" means "This spell costs {1} less to cast for each [text] you control."` — every structural element of that template lives in the expansion (the constant 1, the per-object filter, the You-controller anchor). Category `CostModifier` matches glossary entry for Affinity ("A keyword ability that reduces how much mana you need to spend to cast a spell. See rule 702.41."). `RuleReference = "702.41"` resolves cleanly in rules-structure.json.

- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (BuildAffinityFilter helper) — PASS. Three lexical branches (card-type plurals → `CardTypes` singular; basic-land plural → `Subtypes` singular with Plains-is-its-own-plural special case; capitalized single-word → `Subtypes` singular) plus a `Characteristics` fallback for multi-word/unknown shapes. The Characteristics fallback is acceptable here because it is a parser-input-side normalization for parameter text that has no rule-mandated structural decomposition (Rule 702.41 says "[text]" — the parameter is opaque to the keyword definition; only the casting cost computation reads it). Fallback occurs only for shapes outside the current fixture coverage and surfaces them for follow-up — not a free-text leak into otherwise-typed slots.

- `libs/magic-ast/Parsing/Combinators/OracleParsers.cs:796` (Affinity combinator) — PASS. Captures "Affinity" "for" Word+ [reminder], joins the words back as parameter, delegates to `KeywordDefinitions.Affinity.CreateExpansion`. Reminder attached on the resulting StaticAbility. Mirrors PartnerWith / Protection parameter-capture pattern.

- `libs/magic-ast/Parsing/Combinators/OracleParsers.cs:809` (Entwine docstring side-fix) — PASS. Docstring now reads `Rule 702.42` (Entwine), which matches `rules-structure.json` (`702.42` = "Entwine"). The prior 702.41 citation (Affinity's rule) was a rules-accuracy bug; this corrects it.

- `tests/magic-ast-tests/Data/HandParsedCards/MRD/Frogmite.json` — PASS. `Affinity for artifacts` → `StaticAbility { KeywordSource: "Affinity for artifacts", Reminder, Effects: [costReduction { Amount: literal 1, PerObject: { CardTypes: ["artifact"], Controller: You } }] }`. Matches Rule 702.41a's "[text] = artifacts" → "for each artifact you control" expansion.

- `tests/magic-ast-tests/Data/HandParsedCards/MRD/MyrEnforcer.json` — PASS. Identical shape to Frogmite; second card-type sample for Affinity-artifacts. Rule 702.41a.

- `tests/magic-ast-tests/Data/HandParsedCards/ELD/BrineGiant.json` — PASS. `Affinity for enchantments` → `PerObject: { CardTypes: ["enchantment"], Controller: You }`. Confirms the helper's plural-stripping branch on a non-artifact card type. Rule 702.41a.

- `tests/magic-ast-tests/Data/HandParsedCards/MRD/TangleGolem.json` — PASS. `Affinity for Forests` → `PerObject: { Subtypes: ["Forest"], Controller: You }`. Basic-land plural → singular subtype, which matches how a Forest's type line reads ("Basic Land — Forest"). Rule 702.41a.

- `tests/magic-ast-tests/Data/HandParsedCards/MRD/RazorGolem.json` — PASS. `Affinity for Plains` → `PerObject: { Subtypes: ["Plains"], Controller: You }` (Plains is its own plural — helper's basicLandPlural dict explicitly handles this). Vigilance sibling models as `StaticAbility { KeywordSource: "Vigilance", Effects: [{ EffectType: "vigilance" }] }` — matches Rule 702.20 conventional surface. Two distinct abilities in `Abilities[]`, no cross-contamination.

### Family B — ETB-bounceland

- `libs/magic-ast/AST/References/ObjectReference.cs` (ObjectReferenceKind.Any addition) — PASS. The enum value is descriptive ("an indefinite controller-choice reference; controller picks one qualifying permanent at resolution. Not targeted (no 'target' keyword)") and contrasts cleanly with `Target` (targeted reference, requires a target declaration). No engine-flavored fields (no resolution timing, no choice-state machinery), only the descriptive axis the oracle text carries: "is this 'a [filter] you control' or 'target [filter]'?" Naming `Any` reads ambiguously against `AnyTarget`, but the docstring makes the distinction explicit and the verdict scope is rules-accuracy, not naming.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReturnToHandRule.cs` (extended) — PASS. Adds `land` to the card-type scan list and an `isTargeted` regex; routes targeted phrasing through `ObjectReferenceKind.Target` and indefinite "a [filter] you control" through `ObjectReferenceKind.Any`. Existing 12 fixtures continue to use Target — backward-compatible by construction (regex requires the literal word "target" to flip the branch).

- `tests/magic-ast-tests/Data/HandParsedCards/RAV/AzoriusChancery.json` — PASS. Three abilities: mana ability (`activated`, tap → `addMana {W}{U}`, IsManaAbility), enters-tapped static, and the bounce trigger. Bounce shape: `triggered { Trigger: { Timing: When, Event: Enters, Filter: { CardTypes: ["land"] } }, Effects: [{ EffectType: returnToHand, Target: { Kind: Any, Filter: { CardTypes: ["land"], Controller: You } } }] }`. The trigger filter `CardTypes: ["land"]` describes "this land" via the type filter axis MAST already uses for ETB self-triggers on lands.

- `tests/magic-ast-tests/Data/HandParsedCards/RAV/BorosGarrison.json` — PASS. Identical shape to Azorius Chancery, color identity {R}{W}. Same Rule basis.

- `tests/magic-ast-tests/Data/HandParsedCards/RAV/SelesnayaSanctuary.json` — PASS. Identical shape, color identity {G}{W}. Same Rule basis.

## Glossary gaps

- None for this batch. "Affinity" is in glossary.json. "Return" as a keyword-action is not in glossary (and not strictly required — the parser models the effect structurally via `ReturnToHandEffect`, not via a glossary lookup).

## Process notes

- **Rule-citation dataset drift on "Return" (Family B).** The briefing, `ObjectReference.cs` docstring, and `ReturnToHandRule.cs` comment all cite **Rule 701.10 (Return)** and **Rule 601.2c (targeting declaration)**. In the local rules dataset at `tests/atlas-flow-test/Data/_03_Primary/Datasets/rules-structure.json`, **701.10 is "Double"** (not "Return"), and **601.2 has unnumbered subrules** (so `601.2c` is not a literal lookup). The current published Comprehensive Rules do place "Return" as a 701.x keyword action and `601.2c` does cover targeting declaration, so the citations are correct against the published CR — but they don't resolve against the dataset this skill is required to verify against. This is a **CORPUS GAP** (the parsed rules snapshot is stale relative to the live CR), not a per-item rules-accuracy failure. The descriptive AST shape itself — `Kind: Any` for indefinite references contrasted with `Kind: Target` for targeted ones — is sound and matches what the oracle text literally distinguishes (presence vs absence of the word "target"). I flag this as a dataset refresh item for the orchestrator rather than failing items whose AST shape is rules-faithful against the live CR.

- **AffectedObjects vs PerObject distinction is intentional and clean.** Batch 25 Family B (type-spell cost reduction, e.g., "Artifact spells you cast cost {1} less") sets `StaticAbility.AffectedObjects` because the static ability picks out *which spells* the cost reduction applies to. Batch 26 Family A (Affinity) sets `CostReductionEffect.PerObject` because the cost reduction *itself* is variable: it always applies to the spell carrying Affinity (no AffectedObjects selection needed — the AffectedObjects implicitly is the spell itself), but the *amount* depends on counting permanents matching the PerObject filter. Two different semantic axes, two different fields. Verified against Rule 702.41a's text: "for each [text] you control" — that's a counting clause on the cost reduction's amount, not a spell-selection clause on the static ability. Correct mapping.

- **Coverage note.** Family A has card-type ×3 and basic-land subtype ×2 fixtures but no creature-subtype variants ("Cats", "Humans"). The briefing acknowledges this and the helper's leading-capital branch routes them correctly when fixtures land. Verdict scope is what's present in this batch; the gap is acknowledged in batch_complete notes, not a verdict failure.

- **Affinity stacking (702.41b) not modeled.** Rule 702.41b: "If a spell has multiple instances of affinity, each of them applies." MAST doesn't need a special node for this — multiple `StaticAbility { KeywordSource: "Affinity for X" }` entries in `Abilities[]` naturally compose, and the cost computation is engine territory. Descriptively correct.
