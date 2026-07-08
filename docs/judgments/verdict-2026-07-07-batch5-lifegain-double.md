# MAST judge — batch verdict

**Date:** 2026-07-07
**Batch:** batch5-lifegain-double
**Branch:** `mast-tdd/2026-07-07-lifegain-double` (base `535fc7f`)
**Scope:** 3 files (1 fixture, 2 parser rules) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/LifeGainDoublingReplacementRule.cs` — PASS.
  "If you would gain life, you gain twice that much life instead." is modeled as a **static**
  `ReplacementEffect` (`Kind: static`), NOT a triggered "whenever you gain life" ability —
  correct per **CR 614.1** ("replacement effects apply continuously … watch for a particular
  event … and completely or partially replace that event"), which is verbatim-present in
  rules-structure.json. `Event = lifeChange/gain/Controller:You` grounds the replaced event
  (**CR 119.3**, present). "instead" ⇒ `OriginalEventOccurs = false` (full replacement),
  correctly contrasting the sibling `LifeGainAugmentationReplacementRule`'s `plus`/`true`
  augmentation shape. The doubling is a typed `ReplacementModifier{Type:"double"}` (reused
  value, sibling to Mill/NoncombatDamage doubling) — not baked into the discriminator, not
  free text.

- `libs/magic-ast/Parsing/Parsers/Activated/Rules/CreaturesYouControlGainKeywordsUntilEndOfTurnRule.cs` — PASS.
  Anchored `^Creatures you control gain (?<kws>.+?) until end of turn$`, then `Regex.Split` on
  ` and ` ⇒ one `GainAbilityEffect` per keyword, each with `Target = Each/creature/you-control`
  and `Duration = untilTime Turn/End`. This is the correct fix for the lossy path: the existing
  `GainAbilityEffectRule`'s branch (line 158) is the **unanchored** `"Creatures you control gain
  (\w+)"`, which captures only "flying" and silently drops "and lifelink". Grants are full-fledged
  gained abilities per **CR 113.10** ("An effect that adds an ability will state that the object
  'gains' … that ability"), present in the rules data. **Sibling-mislabel check:** for a genuine
  single-keyword card (e.g. Vito's "Creatures you control gain lifelink until end of turn") the
  split yields a one-element list ⇒ one `GainAbilityEffect` — the identical shape the single-effect
  path produced, so no over-decomposition. Deafening Clarion's grant is a spell-layer effect, not an
  activated ability, so this activated-layer rule never touches it. The keyword builder
  (`BuildGrantedKeywordAbility`) recognizes both `flying` and `lifelink`; an unrecognized keyword
  bails (returns false) so the fallback still owns those cards. `TryMatch` returns null so only the
  multi path is live — no double-emission.

- `tests/magic-ast-tests/Fixtures/HandParsedCards/LEG/TheWindCrystal.json` — PASS.
  No `unparsed` / `UnparsedEffect` / `Diagnostics` anywhere in `Output.Oracle.Abilities`
  (the sole "gain"/"twice" occurrence is the verbatim `RawText` mirror, which is exempt).
  All three lines are faithful with no dropped sibling:
    - Line 1 (`costReduction`, Amount literal 1, AffectedObjects White/spell/You) — present.
    - Line 2 — the doubling replacement exactly as the rule emits (`OriginalEventOccurs:false`,
      `Modifier{Type:"double"}`).
    - Line 3 carries **both** keywords: `flying` as an `evasion` effect blockable only by
      creatures with keyword Flying/Reach (**CR 702.9**), and `lifelink` as a `lifelink` effect
      (**CR 702.15**) — each with its own `Duration` (`untilTime` Turn/End). The "and lifelink"
      drop is genuinely repaired, not re-hidden.

- `mast-tdd/2026-07-07-lifegain-double#projection-decision` — PASS.
  No new discriminator. The `double` modifier value and the `replacement` / `gainAbility` /
  `lifeChange` / `evasion` / `lifelink` discriminators are all pre-existing (siblings to the
  Mill/NoncombatDamage doubling rules and to `GainAbilityEffectRule`). The exhaustiveness
  ratchet is not triggered, so no `PortGraph` case / `PortWalkProjection` entry /
  `known-coarse-projections.json` entry is required.

## Glossary gaps

None. (double / lifelink / flying / replacement effect all standard, cited above.)

## Process notes

- Line 1's cost-reduction filter encodes the affected set as `CardTypes: ["spell"]`. "Spell" is
  not a card type in CR 205 — it is an object-on-the-stack status (CR 111). This is a **pre-existing
  codebase convention** emitted by the existing `costReduction` rule (not new work in this branch),
  and it is not the load-bearing modeling under review, so it does not block this batch. Flagged for
  the engine-lens audit's attention as a broader terminology item, not a per-item FAIL here.
- The two new rules are *parser* rules (`Parsing/Parsers/...`), not AST node definitions; they were
  judged on the AST shape they emit against the gold, and on their doc-comment CR citations
  (614.1, 119.3, 113.6, 113.10 — all present and matching in rules-structure.json).
