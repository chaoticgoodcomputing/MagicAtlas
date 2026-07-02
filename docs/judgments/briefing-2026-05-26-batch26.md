# Batch 26 briefing — 2026-05-26

Two parallel families. Affinity (perennial tarpit) finally unblocked because batch 25 Family B established the `StaticAbility.AffectedObjects` + `CostReductionEffect` pattern.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Affinity for [type/subtype] | #1 + #5 | ~36 combined | new `Affinity` KeywordDefinition with parameter; CostReductionEffect + PerObject |
| B: ETB-bounceland | #4 | 12 | new TriggeredRule using ReturnToHandEffect |

Skipping cluster #2 (Fear, AST shape deferred) and cluster #3 (As-enters-choose-type, needs new effect shape).

---

## Family A: Affinity for [type/subtype] (clusters #1 + #5, ~36 yield combined)

**Failure signal:** Oracle line `Affinity for artifacts (This spell costs {1} less to cast for each artifact you control.)` — `Affinity` is not registered as a keyword. Affinity is parameterized by a type/subtype phrase.

The 24+12 split between clusters #1 and #5 reflects card-type filters ("artifacts", "Cats", "Humans") vs subtype filters with specific subtypes ("Plains" — basic land type, "Towns", "Lizards"). Both fall under rule 702.41a's "Affinity for [text]" template — one keyword variant, one parameter.

### Verified rule citation
- **702.41a Affinity** — "Affinity is a static ability that functions while the spell with affinity is on the stack. 'Affinity for [text]' means 'This spell costs {1} less to cast for each [text] you control.'"
- **702.41b** — "If a spell has multiple instances of affinity, each of them applies." (Multi-affinity stacks.)

### Cards in these clusters
- **Refurbished Familiar** — `Affinity for artifacts (...)`
- **Into Thin Air** — `Affinity for artifacts (...)`
- **Valkyrie Aerial Unit** — `Affinity for artifacts (...)`
- Various subtype variants: `Affinity for Cats`, `Affinity for Humans`, `Affinity for Plains`.

### Existing infrastructure
- `CostReductionEffect.PerObject: ObjectFilter?` — already exists; this is exactly the "for each X you control" hook.
- `KeywordDefinition` with parameterized text (mirror existing `PartnerWith` from `KeywordDefinitions.cs`).

### AST type
**No new AST.** Reuse `CostReductionEffect` with:
- `Amount: LiteralQuantity.Of(1)` (Affinity always reduces by 1).
- `PerObject: ObjectFilter { CardTypes: ["artifact"], Controller: You }` for "Affinity for artifacts".
- `PerObject: ObjectFilter { Subtypes: ["Cat"], Controller: You }` for "Affinity for Cats".
- `PerObject: ObjectFilter { Supertypes: ["Basic"], CardTypes: ["land"], Subtypes: ["Plains"] }` for "Affinity for Plains" — basic land type. (Verify Plains' filter shape.)

### Parser surface
Add `KeywordDefinition Affinity` to `KeywordDefinitions.cs`. Mirror `PartnerWith` (parameterized keyword with text parameter):
```csharp
public static KeywordDefinition Affinity { get; } =
  new()
  {
    Name = "Affinity",
    RuleReference = "702.41",
    Category = KeywordCategory.Static,
    HasParameter = true,
    ParameterType = KeywordParameterType.Name,  // or a custom "filter text" type
    CreateExpansion = parameter => new StaticAbility
    {
      KeywordSource = $"Affinity for {parameter}",  // verify convention
      Effects = [new CostReductionEffect
      {
        Amount = LiteralQuantity.Of(1),
        PerObject = ParseAffinityFilter(parameter),  // helper to map "artifacts" → CardTypes: ["artifact"]
      }],
    },
  };
```

Add `Affinity` to `All`. Add a parser combinator to `OracleParsers.cs` that captures "Affinity for [text-up-to-paren]" — the parameter is the text between "for" and the reminder paren or end-of-line.

### Filter mapping helper
`ParseAffinityFilter(text)` should:
- Lowercase the text.
- If the text is a card-type plural ("artifacts", "creatures", "lands"), strip the plural and emit `CardTypes: [singular]`.
- If the text is a known subtype plural ("Cats", "Humans", "Lizards", "Saprolings"), emit `Subtypes: [singular]`.
- If the text is a basic land type plural ("Plains", "Islands", "Swamps", "Mountains", "Forests"), emit `Subtypes: [singular]` (basic land subtypes serve as land subtypes).
- If unknown, emit a fallback with the raw text in `Characteristics` (or report a parser gap).

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Affinity for artifacts",
  "Reminder": { "Text": "(This spell costs {1} less to cast for each artifact you control.)" },
  "Effects": [{
    "EffectType": "costReduction",
    "Amount": { "QuantityType": "literal", "Value": 1 },
    "PerObject": {
      "CardTypes": ["artifact"],
      "Controller": "You"
    }
  }]
}
```

### Anti-patterns
- Do NOT model the "applies while on stack" semantics (engine territory).
- Do NOT add a new AST type for Affinity — CostReductionEffect with PerObject is the canonical shape.
- Do NOT confuse Affinity with type-cost-reduction (batch 25 Family B): Affinity is `for each X you control` (variable, on stack); type-cost-reduction is `<filter> spells you cast cost N less` (fixed, on the cost-reducer card).

### Cards to fixture (5)
- 2-3 card-type variants ("artifacts").
- 1-2 subtype variants ("Cats", "Humans").
- 1 basic-land-type variant ("Plains" or "Mountains").

Pre-validate sibling abilities per card.

---

## Family B: ETB-bounceland (cluster #4, +12 yield)

**Failure signal:** Oracle line `When this land enters, return a land you control to its owner's hand.` — bounceland pattern (Azorius Chancery, Boros Garrison, etc.). Trigger detection works (ETB self for land). No `[TriggeredRule]` handles the effect "return a land you control to its owner's hand."

### Verified rule citation
- **701.10 Return** — "To return an object to a zone, move it from its current zone to that zone." (Generic zone-change verb.)

### Cards in this family
- **Azorius Chancery** — `({T}: Add {W}{U}.)\nThis land enters tapped.\nWhen this land enters, return a land you control to its owner's hand.`
- **Boros Garrison**, **Dimir Aqueduct**, **Golgari Rot Farm**, **Gruul Turf**, **Izzet Boilerworks**, **Orzhov Basilica**, **Rakdos Carnarium**, **Selesnya Sanctuary**, **Simic Growth Chamber**, etc.

The Ravnica "bouncelands" — all 10 guild variants share this body. Pre-validate that the mana ability and "enters tapped" siblings already parse.

### Existing infrastructure
- `ReturnToHandEffect` exists in `AST/Effects/ZoneChange/`.

### Parser surface
New `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReturnLandToHandTriggeredRule.cs` (or extend the existing `ReturnToHandRule`). Match `^return a land you control to its owner's hand\.?$`. Emit `ReturnToHandEffect { Target: ObjectReference { Kind: Any, Filter: { CardTypes: ["land"], Controller: You } } }`.

Verify the existing `ReturnToHandRule` doesn't already cover this — it might just need extension for "a land you control" subject pattern instead of "target creature an opponent controls" (from batch 15).

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "Enters", "Filter": { "CardTypes": ["land"] } },
  "Effects": [{
    "EffectType": "returnToHand",
    "Target": {
      "Kind": "Any",
      "Filter": { "CardTypes": ["land"], "Controller": "You" }
    }
  }]
}
```

(Verify the `Kind` for "a land you control" — non-target indefinite reference. Likely a new `Kind: Any` or `Kind: Indefinite`; check existing fixtures for the convention.)

### Anti-patterns
- Do NOT model the choice-by-controller semantics (which land they return — engine territory).
- Do NOT confuse "a land you control" (indefinite, you pick) with "target land" (targeted) or "target land you control" (targeted, your control).

---

## Cross-family notes

- **Disjoint files.** A: `KeywordDefinitions.cs` + `OracleParsers.cs` + helper. B: new `Triggered/Rules/` file (or extend `ReturnToHandRule.cs`).
- **No new AST types in either family.**
- **Pre-verified rule citations:** A cites 702.41; B cites 701.10.
- Family A unlocks the largest remaining yield (~36 cards) — clusters 1 + 5 combined.
