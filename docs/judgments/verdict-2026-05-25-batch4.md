# MAST judge — batch 4 verdict

**Date:** 2026-05-25
**Mode:** verify (in-process — falling back from sub-agent dispatch due to prior 529 patterns; batch is small enough for safe in-process judgment)
**Scope:** 5 items (1 new AST type, 2 parser surfaces, 2 new fixtures)
**Briefing:** `docs/judgments/briefing-2026-05-25-batch4.md`
**Doctrine:** strict binary PASS/FAIL per `.claude/skills/mast-judge/SKILL.md`
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

### AST nodes

- `libs/magic-ast/AST/Effects/Combat/MustBlockEffect.cs` — PASS. Cites **Rule 509.1c** (defender-side block requirement) to clause. Doc-comment explicitly contrasts with `MustBeBlockedEffect` (attacker-side, same rule, opposite side of the requirement) and `MustAttackEffect` (Rule 508.1d, different combat step). Field shape `Target: ObjectReference` mirrors MustAttackEffect; same trait fields. Discriminator `mustBlock` is camelCase and matches the rule's vocabulary.

### Parsers

- `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (TryParseMustBlock + TryParseConditionalSpellCostReduction) — PASS. The must-block recognizer mirrors must-attack via the shared `ClassifyCombatRequirementSubject` helper, dispatches Self vs Each-creatures correctly. The conditional-cost-reduction recognizer cites **Rule 117.6** (cost reductions) and uses the existing `StaticAbility.Condition` shape mirroring the Zurgo "During your turn, has indestructible" precedent.

- `libs/magic-ast/Parsing/Parsers/SpellAbilityParser.cs` (TryParseSpellTapTargetEffect) — PASS. Cites **Rule 701.26a** (Tap) to clause. Uses the existing `ObjectFilter.CardTypes` multi-element-list convention for type disjunction (matches Demolish's destroy work). No free-text characteristics.

### Fixtures

- `tests/magic-ast-tests/Data/HandParsedCards/MOM/MentalModulation.json` — PASS. Three abilities: one Static (cost reduction with "during your turn" Condition), two Spell (one per oracle-text line). Gold split into per-clause abilities matches every existing multi-line spell fixture in the corpus; the helper's original bundling was reverted by the orchestrator since (a) the parser produces per-clause abilities universally, (b) MTG CR 113.3a uses plural "abilities" so multiple per spell is rules-faithful, and (c) bundling would have required ClauseSplitter changes out of batch scope. (See process notes below.)

- `tests/magic-ast-tests/Data/HandParsedCards/USG/GrandMelee.json` — PASS. Two Static abilities with `ObjectReference.Kind = Each` + `CardTypes: ["creature"]` filters, one wrapping MustAttackEffect, one wrapping the new MustBlockEffect. No `Controller` qualifier (global, not "you control") — matches Rule 508.1d/509.1c phrasing.

## Glossary gaps

None.

## Process notes

### Doctrine clarification: multi-effect-per-clause

During this batch a structural pattern surfaced that the existing doctrine didn't fully cover. The corpus contains ~10,691 oracle lines that are **multi-sentence single-line** entries (one `\n`-bounded clause with multiple `". [A-Z]"` sentence boundaries). Examples:
- Aura Graft: "Gain control of target Aura. Attach it to another permanent it can enchant." — one spell, two effects.
- Stern Marshal: "{T}: Target creature gets +2/+2 until end of turn. Activate only during your turn." — one activated ability with restriction tail.

For these cases, the gold AST SHOULD bundle multiple effects into one ability's `Effects` list. The per-clause-one-ability convention applies at the line level — within a clause, multiple sentences yield multiple effects on the same ability.

**Mental Modulation is NOT one of these cases** — its effects are `\n`-separated, so per-clause-one-ability gives three abilities (one static + two spell). The orchestrator's revision of the helper's bundling was correct for this card.

But the new doctrine note (saved to memory as `feedback_mast_multi_effect_per_clause`) instructs future batches: when picking candidates with multi-sentence single-line content, the helper should bundle in gold and the mechanical sub-agent needs multi-effect-per-clause parser support.

### Mechanical sub-agent kill

The dispatched Mental Modulation mechanical sub-agent was killed mid-investigation (no commits, no work products). Scope was small enough for the orchestrator to finish in-process — recovery cost was ~10 minutes. Future batches should monitor for re-occurrence; if mech sub-agents are killed at a consistent point, that's a sub-agent infrastructure signal.

---

## Closing

Path: `docs/judgments/verdict-2026-05-25-batch4.md`
Counts: **5 PASS / 0 FAIL**
**Verdict: PROCEED** — Batch 4 cleared. Glossary regen + retriage next, then Batch 5.
