# Batch 17 briefing — 2026-05-26

Single family this batch: **CantAttackOrBlock** (cluster #4, +15 yield). Resumes the work that bailed in batch 16 on the trait-boundary call. The migration that unblocked it (pluralizing `StaticAbility.Effect` → `Effects`) landed in commits between batch 16 close and this briefing — verify on main via `git log --oneline -3 libs/magic-ast/AST/Abilities/StaticAbility.cs`.

---

## Family 1: Enchanted-creature can't attack or block (cluster #4)

**Failure signal:** Aura body `Enchanted creature can't attack or block.` — subject `Enchanted creature`, two restrictions joined by "or." `CantBlockEffect` exists (`AST/Effects/Combat/CantBlockEffect.cs`) but `CantAttackEffect` does not. No parser in `StaticAbilityParser.cs` handles this Aura-attached dual-restriction shape.

### Cards in this family
1. **Luminous Bonds** — Aura, single-line body. Cleanest fixture.
2. **Compulsory Rest** — Aura, body line only.
3. **Cage of Hands** — Aura, body line + bounce-cost activated sibling. Sibling-shape complication; verify activated parses before committing.
4. **Cooped Up** — Aura, body line + rummaging activated sibling.
5. **Choking Restraints** — Aura, body line + tap-on-upkeep sibling.

Helper-novel should prefer cards in order 1→5; bail on any whose siblings don't already parse (run `Parser_ProducesExpectedOutput` after writing each fixture to confirm).

### Relevant rules
- **509.1d Combat restriction (attack side)** — a creature must attack when able unless a restriction prevents it; "can't attack" is the canonical negation.
- **509.1c Block restriction** — `CantBlockEffect` already encodes this (rule 509.1c blocker-side restriction).
- **702.5 / 303 Aura** — Aura permanents attach to objects via Enchant. "Enchanted [type] [effect]" is the canonical static-ability body shape on Auras; subject = `ObjectReference.EnchantedOrEquipped()`.
- **`feedback_mast_multi_effect_per_clause`** — "Enchanted creature can't attack or block." is multi-effect-per-clause: two effects in one `StaticAbility.Effects` list, NOT a single combined `CantAttackOrBlockEffect`.

### Schema precondition (verify before starting)
`StaticAbility.Effects` is now `IReadOnlyList<Effect>` (post-migration). Verify:
```bash
grep "IReadOnlyList<Effect> Effects" libs/magic-ast/AST/Abilities/StaticAbility.cs
```
If the grep returns no match, the migration didn't land and the family must HALT.

### AST types to write
- **`CantAttackEffect`** at `libs/magic-ast/AST/Effects/Combat/CantAttackEffect.cs`. Direct mirror of `CantBlockEffect`:
  - `[OracleEffect("cantAttack")]`
  - Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`
  - Required field `Target: ObjectReference`

### Parser surface (mech wave)
A new `TryParse…` method in `StaticAbilityParser.cs` (or a slight extension to `TryParseCantBlock`) that handles:
- Subject `Enchanted creature` → `ObjectReference.EnchantedOrEquipped()`.
- The dual-restriction `can't attack or block` parses to two effects: `[CantAttackEffect { Target }, CantBlockEffect { Target }]`.

Existing `TryParseCantBlock` only handles single-restriction `This creature can't block` (self subject). The new method is its sibling for the Aura attack-AND-block case. Insert in the dispatch chain after the existing `TryParseCantBlock`.

### Expected generalization (mech)
ONE new parser method covers all 5 fixtures. The subject extraction `Enchanted creature` and the dual-restriction parse happen in the same method.

### Gold AST shape for body line
```json
{
  "Kind": "static",
  "Effects": [
    {
      "EffectType": "cantAttack",
      "Target": { "Kind": "enchantedOrEquipped" }
    },
    {
      "EffectType": "cantBlock",
      "Target": { "Kind": "enchantedOrEquipped" }
    }
  ]
}
```

(Verify `Kind` enum value for the `enchantedOrEquipped` reference — it might be `enchanted` or `equipped` or a single combined value; consult `ObjectReference` discriminators in `GLOSSARY.md`.)

### Anti-patterns
- Do NOT invent a single `CantAttackOrBlockEffect` combined type.
- Do NOT extend `CantBlockEffect` with a flag to "also can't attack."
- Do NOT model the engine-side declared-attackers / declared-blockers consequences.

### Glossary gaps
- None.

### Sibling-shape note
For multi-ability Auras (Cage of Hands' bounce-cost-activated, Cooped Up's rummaging-activated, Choking Restraints' tap-on-upkeep-triggered), helper-novel verifies the sibling abilities already parse before committing the fixture. If a sibling reds unexpectedly, BAIL and swap the card from the cluster's 15 alternatives.
