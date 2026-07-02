# Batch 25 briefing — 2026-05-26

Two parallel families. Rule citations pre-verified.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: ETB-create Food token | #4 | 13 | extend triggered CreateTokenRule for predefined Food token |
| B: Cost-reduction for type spells | #3 | 13 | new static parser surface using `CostReductionEffect` + `AffectedObjects` |

Skipping cluster #1 (Affinity, related to Family B but more complex with `for each X you control`), cluster #2 (Fear, AST shape deferred), cluster #5 (As-enters-choose-type, needs new effect shape).

---

## Family A: ETB-create-Food-token (cluster #4, +13 yield)

**Failure signal:** Oracle line `When this creature enters, create a Food token.` (with reminder `(It's an artifact with "{2}, {T}, Sacrifice this token: You gain 3 life.")`) — triggered CreateTokenRule exists but doesn't recognize Food. Food is a predefined token type (artifact subtype, with canonical activated ability defined in the rules — the activated is engine territory).

### Verified rule citation
- **107.10b** — predefined tokens. Food is in the "common predefined tokens" set with a fixed definition. Per `feedback_mast_describes_not_executes`: AST records the token type; the canonical activated ability is engine territory.

### Cards in this family
- **Greta, Sweettooth Scourge** — `When Greta enters, create a Food token. (reminder)`
- **Rosie Cotton of South Lane** — same shape.
- **Trail of Crumbs** — `When this enchantment enters, create a Food token. (reminder)`
- **Spider-Ham, Peter Porker** — same.

### Parser surface
Find triggered `CreateTokenRule` at `libs/magic-ast/Parsing/Parsers/Triggered/Rules/CreateTokenRule.cs`. Extend the regex to recognize `create a Food token` and emit `CreateTokenEffect { Count: 1, Token: TokenDefinition { Types: ["artifact"], Subtypes: ["Food"] } }` (no Power/Toughness — Food is non-creature). The trailing reminder text becomes the ability's `Reminder`.

Inspect existing creature-token fixtures (`INV/Sprout.json`, `FRF/SecureTheWastes.json`) for TokenDefinition shape and adapt for non-creature tokens.

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "Enters", "Filter": { "CardTypes": ["creature"] /* or enchantment */ } },
  "Effects": [{
    "EffectType": "createToken",
    "Count": { "QuantityType": "literal", "Value": 1 },
    "Token": {
      "Types": ["artifact"],
      "Subtypes": ["Food"],
      "IsCopy": false
    }
  }],
  "Reminder": { "Text": "(It's an artifact with \"...\")" }
}
```

### Anti-patterns
- Do NOT model Food's `{2}, {T}, Sacrifice: gain 3 life` activated ability on the TokenDefinition. The reminder text describes engine-territory behavior of a predefined token type.
- Do NOT confuse the entering creature ("When [Self] enters") with the created Food (`Subtypes: ["Food"]`).

---

## Family B: Cost-reduction for type spells (cluster #3, +13 yield)

**Failure signal:** Oracle line `Angel spells you cast cost {2} less to cast.` — `StaticAbilityParser` doesn't recognize the "X spells you cast cost {N} less" shape. `CostReductionEffect` AST exists with `Amount: Quantity`. `StaticAbility.AffectedObjects: ObjectFilter?` exists for naming what the static applies to.

### Verified rule citation
- **117.6** — cost modification rules. "Each effect that modifies how much it costs to cast a spell or activate an ability… effects that increase the cost are applied before effects that reduce the cost."
- Per `feedback_mast_describes_not_executes`: AST records the filter + reduction amount. Cost-calculation flow is engine.

### Cards in this family
- **Ruby Medallion** — `Red spells you cast cost {1} less to cast.`
- **Foundry Inspector** — `Artifact spells you cast cost {1} less to cast.`
- **Ugin, the Ineffable** — `Colorless spells you cast cost {2} less to cast.`
- **Hero of Iroas** — `Aura spells you cast cost {1} less to cast.` (Subtype filter)
- **Kethis, the Hidden Hand** — `Legendary spells you cast cost {1} less to cast.` (Supertype filter)

Pre-validate sibling abilities per fixture.

### Parser surface
Extend `StaticAbilityParser.cs` to recognize `^<filter> spells you cast cost \{<amount>\} less to cast\.?$`. Parse the filter into an `ObjectFilter` (handles Subtypes like "Angel" / "Aura", CardTypes like "Artifact", Colors like "Red"/"White"/"Colorless", Supertypes like "Legendary"). Emit:
```csharp
new StaticAbility {
  AffectedObjects = parsedFilter,
  Effects = [new CostReductionEffect { Amount = LiteralQuantity.Of(N), IsOptional = false }]
}
```

The "you cast" qualifier maps to `AffectedObjects.Controller = ControllerFilter.You` (or whatever existing convention exists).

### Gold AST shape
```json
{
  "Kind": "static",
  "AffectedObjects": {
    "Subtypes": ["Angel"],
    "Controller": "You"
  },
  "Effects": [{
    "EffectType": "costReduction",
    "Amount": { "QuantityType": "literal", "Value": 2 }
  }]
}
```

(Verify field shape; check an existing AffectedObjects fixture for casing — e.g., the GaddockTeeg fixture mentioned in prior batches.)

### Anti-patterns
- Do NOT add fields to `CostReductionEffect` (e.g., `AppliesTo`). The static-ability's `AffectedObjects` field is the canonical "what this applies to" hook.
- Do NOT model the "amount can only reduce colored mana" subclause (e.g., Ragemonger). That's a separate variant — skip Ragemonger and pick cleaner candidates.
- Do NOT confuse "cost less" (Foundry Inspector) with "cost more" (Andradite Leech). They're opposite-polarity; this batch handles "less" only.

---

## Cross-family notes

- **Disjoint files.** A: `Triggered/Rules/CreateTokenRule.cs`. B: `StaticAbilityParser.cs`.
- **AST already in place** for both families. No new AST types in this batch.
- **Pre-verified rule citations:** Food = 107.10b; CostReduction = 117.6. Cite verbatim in source-side doc-comments if you cite.
