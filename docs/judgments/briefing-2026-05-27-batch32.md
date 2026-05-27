# Batch 32 briefing — 2026-05-27

Single family — Fear (perennial #1 cluster, finally attempting). Pre-verified rule citations.

| Family | Cluster | Yield | Line freq | Surface |
|---|---|---|---|---|
| A: Fear keyword | #1 | 18 | 35 | new `FearEffect` OR extend EvasionEffect with disjunctive filter |

Skipping cluster #2 (Start your engines! — Max Speed GrantedAbility design needed), cluster #3 (As-enters-choose-type — new at-entry choice effect; queued next), cluster #4 (Unleash — embedded conditional), cluster #5 (Painlands — pay-life-or-enters-tapped; queued next).

---

## Family A: Fear keyword (cluster #1, +18 yield)

**Failure signal:** Oracle line `Fear (This creature can't be blocked except by artifact creatures and/or black creatures.)` — Fear is not registered. Parameterless evasion keyword. Reminder describes a disjunctive block-exception.

### Verified rule citation
- **702.36 Fear** — "Fear is an evasion ability. A creature with fear can't be blocked except by artifact creatures and/or black creatures." (Verify rule number against `rules-structure.json` before committing.)

Per `feedback_mast_describes_not_executes`: MAST records the keyword's presence; the block-restriction enforcement is engine territory.

### Cards in this family
Classic evasion cards (Lingering Tormentor, Undercity Shade, Squirming Mass, Wormwood Treefolk, etc.).

### AST shape decision (critical)

The Fear reminder is `This creature can't be blocked except by artifact creatures and/or black creatures.` — a **disjunction of two filter shapes** (artifact-creature OR black-creature). `ObjectFilter` doesn't natively support filter-level disjunction.

**Option 1 (pragmatic-stretch, recommended):** Use `EvasionEffect.CanBeBlockedBy: ObjectFilter { CardTypes: ["creature"], Characteristics: ["artifact", "black"] }`. This mirrors Flying's emit convention (`Characteristics: ["flying", "reach"]` — keyword-name disjunction). The `Characteristics` field is documented as "additional characteristics" — pragmatically stretches to include type ("artifact") and color ("black") as discriminating qualifiers. Existing fixtures with Characteristics-based evasion (Flying, Menace) establish the pattern.

**Option 2 (purist):** Add filter-level disjunction to ObjectFilter or create a new EvasionEffect-side disjunction structure. Architectural scope expansion.

**Option 3 (new effect):** Create `FearEffect` (parameterless, mirror Flanking/Indestructible) and bypass ObjectFilter entirely. The reminder text describes the semantic; the keyword's presence is the only AST record. Loses the descriptive content of "what can block."

**Pick Option 1.** It matches the existing Flying convention. Slight axis-conflation accepted as the descriptive cost of the disjunction. Document in commit message; if judge rejects, escalate to Option 2.

### Parser surface
- New `KeywordDefinition Fear` in `KeywordDefinitions.cs`. `HasParameter = false`, `Category = KeywordCategory.Static`, `RuleReference = "702.36"`. CreateExpansion:
  ```csharp
  CreateExpansion = _ => new StaticAbility
  {
    KeywordSource = "Fear",
    Effects = [new EvasionEffect
    {
      CanBeBlockedBy = new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = ["artifact", "black"],
      },
    }],
  };
  ```
- Add to `All` collection.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Fear",
  "Reminder": { "Text": "(This creature can't be blocked except by artifact creatures and/or black creatures.)" },
  "Effects": [{
    "EffectType": "evasion",
    "CanBeBlockedBy": {
      "CardTypes": ["creature"],
      "Characteristics": ["artifact", "black"]
    }
  }]
}
```

### Anti-patterns
- Do NOT create a separate `FearEffect` — Fear is a member of the evasion family that EvasionEffect already documents.
- Do NOT model "artifact creatures and/or black creatures" via two separate EvasionEffects (would mean AND-semantics; wrong).
- Do NOT model the "and/or" as a structured disjunction beyond the Characteristics list — that's an Option-2 architectural expansion.

### Cards to fixture (3)
Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("^Fear|\\nFear(?=\\s)"))) | .name + "|" + (.oracle_text | split("\n") | first)' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -15
```

Pre-validate sibling abilities. Pick clean printings.

---

## Cross-family notes

- Single-family batch.
- **Pre-verified rule citation:** 702.36 (Fear).
- AST shape choice is doctrinally non-trivial — judge will scrutinize Option-1's axis-conflation. The Flying precedent is the strongest defense.
