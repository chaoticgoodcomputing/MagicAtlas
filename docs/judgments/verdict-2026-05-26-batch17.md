# MAST judge — batch verdict

**Date:** 2026-05-26
**Scope:** 7 files (5 fixtures, 2 AST nodes) + 1 parser surface (informational)
**Result:** FAIL

> **Pre-flight note (BoundInSilence TypeLine fix):** The orchestrator corrected
> `BoundInSilence.json` so that `Kindred` lives in `Types` (alongside
> `Enchantment`) and is not present in `Supertypes`. Verified against
> **Rule 205.2a** (`The card types are artifact, battle, conspiracy, creature,
> dungeon, enchantment, instant, **kindred**, land, phenomenon, plane,
> planeswalker, scheme, sorcery, and vanguard.`). The fixture's TypeLine is
> rules-accurate as on main.

## Summary

- PASS: 6
- FAIL: 1

## FAIL verdicts

### `libs/magic-ast/AST/Effects/Combat/CantAttackEffect.cs`
**Verdict:** FAIL
**Issue:** Rule citation is wrong; "can't attack" restrictions live in section 508 (Declare Attackers Step), not section 509 (Declare Blockers Step).
**Rule citation:** 508.1c (correct); the doc-comment cites 509.1d and "508/509.1d boundary".
**Rule text:**
> 508.1c — "The active player checks each creature they control to see whether it's affected by any restrictions (effects that say a creature can't attack, or that it can't attack unless some condition is met). If any restrictions are being disobeyed, the declaration of attackers is illegal."
>
> 509.1d (what the AST currently cites) — "If any of the chosen creatures require paying costs to block, the defending player determines the total cost to block…" (block-cost determination, no relation to attack restrictions).
**What the AST says:**
> Summary tag: `Rule 509.1d (declare-attackers step; attacking restrictions constrain the set of legal attacker declarations the active player can make).`
>
> Remarks: `This is the dual of MustAttackEffect — same rule (508/509.1d boundary), opposite polarity`
**Why this misrepresents the rule:** Section 509 is the Declare **Blockers** Step. The subrule 509.1d the doc cites is specifically about determining block costs and has nothing to do with the attack side. The canonical attack-side restriction rule is **508.1c** under section 508 (Declare Attackers Step), whose text literally enumerates "effects that say a creature can't attack" — i.e. exactly what this AST node describes. The "508/509.1d boundary" phrasing is incoherent because 508 and 509 are separate combat steps, not a shared boundary; the dual of 509.1b is 508.1c, not 509.1d.
**Suggested fix:** Replace the two rule citations in the doc-comment:
- Summary line: change `Rule 509.1d (declare-attackers step; attacking restrictions constrain the set of legal attacker declarations the active player can make)` → `Rule 508.1c (declare-attackers step; attacking restrictions constrain the set of legal attacker declarations the active player can make)`.
- MustAttackEffect dual remark: change `same rule (508/509.1d boundary)` → `same rule (508.1c restrictions / 508.1d requirements boundary)`.
- Also update the inline parser doc-comment in `StaticAbilityParser.cs::TryParseEnchantedCantAttackOrBlock` which currently says `Rule 509.1d / 509.1c` — the correct pair is `508.1c / 509.1b`.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Combat/CantBlockEffect.cs` — PASS. Nullable `Target` change is minimal, descriptively justified (null = self-subject "This creature can't block."; set = Aura-attached "Enchanted creature can't block."), and backward-compatible with existing self-subject fixtures. Rule citation (509.1c) is pre-existing and out of scope for re-judgment; the schema delta itself is rules-grounded and the doc-comment update correctly describes the new dual usage.
- `tests/magic-ast-tests/Data/HandParsedCards/XLN/LuminousBonds.json` — PASS. Models Enchant (`enchantRestriction` with `CardTypes: ["creature"]`) plus a single `StaticAbility` bundling `cantAttack` + `cantBlock` both targeting `EnchantedOrEquipped` — multi-effect-per-clause doctrine applied correctly, no unparsed nodes, no free-text leakage.
- `tests/magic-ast-tests/Data/HandParsedCards/FUT/CompulsoryRest.json` — PASS. Body line matches the cluster gold shape; sibling third ability "Enchanted creature has '{2}, Sacrifice this creature: You gain 2 life.'" is gold-modeled as `gainAbility` targeting `EnchantedOrEquipped` with the inner activated ability fully decomposed (mana cost + sacrifice cost + gainLife effect). No unparsed leakage.
- `tests/magic-ast-tests/Data/HandParsedCards/10E/Pacifism.json` — PASS. Cleanest two-ability Aura fixture; matches the gold shape exactly.
- `tests/magic-ast-tests/Data/HandParsedCards/RTR/DetainedByLegionnaires.json` — PASS. Identical body shape to Pacifism; rules-consistent.
- `tests/magic-ast-tests/Data/HandParsedCards/MH3/BoundInSilence.json` — PASS. TypeLine correctly models Kindred as a card type per Rule 205.2a (`Types: ["Kindred", "Enchantment"]`, `Subtypes: ["Rebel", "Aura"]`, no `Supertypes` key). Oracle abilities match the cluster gold shape.

## Glossary gaps

None. `Rebel` is absent from `glossary.json` (returns `null`), but it's an oracle subtype, not a mechanic, and the fixture handles it as plain-string data in the `Subtypes` array — no AST-level semantics depend on it.

## Process notes

The rule-citation FAIL is narrow and surface-level: the AST structure (discriminator string, fields, optionality, trait composition) and parser surface are correct. Only the doc-comment rule numbers need editing. Recommend a follow-up sub-agent (one-shot, ~3 lines of doc-string edits across two files) to fix and re-render before merging family 1.

The parser surface itself (`TryParseEnchantedCantAttackOrBlock` in `StaticAbilityParser.cs`) is descriptively correct: matches "Enchanted/Equipped creature can't attack or block." with a single regex, emits exactly two effects sharing the same `Target` instance, both `IsOptional = false`, no free-text fallthrough. The doc-comment carries the same 509.1d/509.1c miscite and should be updated together with the AST node.
