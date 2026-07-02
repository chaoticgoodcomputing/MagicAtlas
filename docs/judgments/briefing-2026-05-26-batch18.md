# Batch 18 briefing — 2026-05-26

Three small parallel families this batch. All three are mechanical: AST types exist; only spell-rule files / KeywordDefinitions entries are missing. Each is small enough that the fixture-then-parser handoff would be heavy ceremony — dispatch one Sonnet agent per family handling BOTH fixtures and the parser surface.

| Family | Cluster | Marginal Yield | Surface | Parser/AST files |
|---|---|---|---|---|
| A: ExileTargetSimpleRule | #2 | ~13 of 17 | `Spell/Rules/` | new `ExileTargetSimpleRule.cs`; `ExileEffect` exists |
| B: UntapTargetRule | #2 | ~4 of 17 | `Spell/Rules/` | new `UntapTargetRule.cs`; `UntapEffect` exists |
| C: Indestructible keyword | #4 | 15 | `KeywordDefinitions.cs` | new entry; `IndestructibleEffect` exists |

Skipping cluster #1 (Affinity tarpit), cluster #3 (attack-trigger +N/+N — `ModifyPTTriggeredRule` exists; cluster persists because of an upstream gap, deserves a dedicated investigation batch), and cluster #5 (Fear — would need either a structured "blocker-exception" representation on `EvasionEffect` or a new `FearEffect`; defer pending an AST-shape decision).

---

## Family A: ExileTargetSimpleRule (cluster #2)

**Failure signal:** `Exile target creature.`, `Exile target artifact.`, `Exile target enchantment.`, `Exile target permanent.` — all parse as `UnparsedSpell`. The existing `ExileTargetLandRule` covers only "land". A generic ExileTarget rule mirroring `DestroyTargetSimpleRule` handles the rest.

### Cards to fixture (5)
1. **Final Death** — `Exile target creature.`
2. **Scour from Existence** — `Exile target permanent.`
3. **Erase** — `Exile target enchantment.`
4. **Shattering Blow** — `Exile target artifact.` (verify: cluster contains this exact line)
5. **Wander Off** — `Exile target creature.` (single-line)

Pre-validate each card's full oracle text from `oracle-cards.json` before committing; pick single-line printings if available. Cluster #2 has 22 lines so plenty of alternatives.

### Relevant rules
- **701.10 Exile** — "To exile an object, put it into the exile zone from wherever it is. An exiled object is an object in the exile zone." Per `feedback_mast_describes_not_executes`: AST records the verb invocation + target; the zone-move sequencing is engine territory.
- **109.1 / 109.2 "target [filter]"** — same target/filter semantics as Destroy. Use `ObjectReference { Kind = Target, Filter = { CardTypes = [filter] } }`.

### Parser surface
New file `libs/magic-ast/Parsing/Parsers/Spell/Rules/ExileTargetSimpleRule.cs`. Mirror `DestroyTargetSimpleRule` shape — reuse `SpellRuleHelpers.ParseDestroyFilter` (already covers `land | artifact | enchantment | creature | permanent`) if it's named generically enough, or factor out a shared filter helper. The rule emits `ExileEffect { Target = ObjectReference { Kind = Target, Filter = <parsed filter> } }`.

**Verify:** confirm `ExileTargetLandRule` doesn't preempt the generic rule's dispatch on "land" subtargets — if it does, leave it alone (more-specific rules first); if generic-only also handles land, deprecate the land-specific rule in a follow-up.

### Anti-patterns
- Do not invent a new `ExileEffect` variant. The existing record carries the target; that's enough.
- Do not duplicate filter-parse logic; share with the destroy path.

---

## Family B: UntapTargetRule (cluster #2)

**Failure signal:** `Untap target creature.`, `Untap target permanent.` — fewer lines than the exile group (~4 in cluster #2), but a clean shape.

### Cards to fixture (3)
1. **Refocus** — `Untap target creature.\nDraw a card.` (two lines; `DrawCardsSimpleRule` exists for line 2)
2. **Burst of Energy** — `Untap target permanent.`
3. *(pick a third single-line untap target from oracle corpus — preferably "Untap target [filter]" without siblings)*

### Relevant rules
- **701.20 Untap** — "To untap a permanent, turn it from its tapped position to its untapped position." Symmetric of Tap (701.21a).

### Parser surface
New file `libs/magic-ast/Parsing/Parsers/Spell/Rules/UntapTargetRule.cs`. Pattern matches `Untap target [filter].`. Filter vocabulary: typically `creature` or `permanent`; can also be more exotic (`tapped artifact`, `[color] creature`) — start minimal, accept those bare cases, bail if richer filters appear.

Emits `UntapEffect { Target = ObjectReference { Kind = Target, Filter = <parsed filter> } }`.

---

## Family C: Indestructible keyword (cluster #4)

**Failure signal:** Oracle line `Indestructible` (bare keyword, no reminder) is not registered in `KeywordDefinitions.cs`. The AST type `IndestructibleEffect` exists.

### Cards to fixture (3)
1. **Spearbreaker Behemoth** — has Indestructible as one ability among others; verify siblings parse.
2. **Silverbluff Bridge** — Land with Indestructible (single-line bare keyword on the relevant clause).
3. **Darkmoss Bridge** — Land with Indestructible.

Cluster has 15 cards. Pre-validate fixtures; pick clean single-keyword cases when possible.

### Relevant rules
- **702.12 Indestructible** — "A keyword ability that grants protection from being destroyed. Permanents with indestructible can't be destroyed; damage and 'destroy' effects don't remove them."
- Glossary already documents: "Permanents with indestructible can't be destroyed; damage and 'destroy' effects don't remove them. Conventionally modeled as a keyword effect, matching haste/trample/vigilance."

### Parser surface
Add a `KeywordDefinition Indestructible` entry to `libs/magic-ast/Keywords/KeywordDefinitions.cs`. Mirror `Vigilance`/`Lifelink` (parameterless SimpleKeyword). Pattern:

```csharp
public static KeywordDefinition Indestructible { get; } =
  new()
  {
    Name = "Indestructible",
    RuleReference = "702.12",
    Category = KeywordCategory.Static,
    HasParameter = false,
    CreateExpansion = _ => new StaticAbility
    {
      KeywordSource = "Indestructible",
      Effects = [new IndestructibleEffect()],
    },
  };
```

### Anti-patterns
- Do not model the "damage / destroy can't remove" semantics as fields on the AST. The keyword's presence is the whole MAST record; the rules engine consults the keyword's rule for behavior.

---

## Cross-family notes

- **No file overlap.** Family A writes `Spell/Rules/ExileTargetSimpleRule.cs`. Family B writes `Spell/Rules/UntapTargetRule.cs`. Family C edits `KeywordDefinitions.cs` (single entry insertion).
- **Reflection registration** — both new spell rules are auto-discovered by attribute. KeywordDefinitions has its own registry pattern (verify the registry index — likely a static `All` list or reflection over public static properties).
- **Fixture key-order** — when writing gold ASTs that include the new `StaticAbility.Effects` list, follow recent batch 17 examples (e.g., `tests/magic-ast-tests/Data/HandParsedCards/XLN/LuminousBonds.json`) for property ordering.
