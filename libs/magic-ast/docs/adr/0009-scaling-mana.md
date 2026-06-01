# Scaling mana: counter-driven mana production

## Status

Accepted (2026-06-01) — implemented in the same batch.

## Context

`AddManaEffectRule` **deliberately bails** on scaling-mana: its `UnmodeledManaClause` guard returns `null` on `for each` / `then add` / `spend this mana only` so the clause surfaces as an honest `UnparsedEffect` (a "pickable family") instead of being silently swallowed into the `Mana` scalar. This ADR closes that seam for the **counter-driven** mana family — the storage-counter lands (Hollow Trees, Calciform Pools, Fountain of Cho, Crucible of the Spirit Dragon, …) and charge-counter artifacts (Kyren Toy, the Mana Batteries, Everflowing Chalice, Gyre Sage, …).

Five oracle shapes recur (all currently `UnparsedEffect`):

| | Shape | Exemplar |
|---|---|---|
| S1 | `Add {C} for each [type] counter on this [noun]` | Gyre Sage, Everflowing Chalice |
| S2 | `Remove any number of [type] counters …: Add {color} for each [type] counter removed this way` | Hollow Trees, Fountain of Cho |
| S3 | `Remove X [type] counters …: Add X mana in any combination of {W} and/or {U}` (or "of colors") | Calciform Pools, Crucible |
| S4 | `Remove any number …: Add {color}, then add an additional {color} for each … removed this way` | Black/Blue/… Mana Battery |
| S5 | `Remove X [type] counters …: Add an amount of {C} equal to X plus one` | Kyren Toy |

Two halves are involved: the **activation cost** ("Remove …counters") and the **effect amount** ("Add …"). `RemoveCountersCost` and `AddManaEffect.Amount` (a `Quantity?`) already exist; `CounterCountQuantity` (counters *on* an object) and `CalculatedQuantity` (arithmetic) already exist. The gaps are narrow.

Relevant rules (verbatim):
- **CR 106.4:** "When an effect instructs a player to add mana, that mana goes into a player's mana pool…"
- **CR 122.1:** "A counter is a marker placed on an object or player that modifies its characteristics and/or interacts with a rule, ability, or effect. Counters are not objects and have no characteristics…"
- **CR 107.3:** "Many objects use the letter X as a placeholder for a number that needs to be determined. Some objects have abilities that define the value of X; the rest let their controller choose the value of X."
- **CR 605.1a:** "An activated ability is a mana ability if it meets all of the following criteria: it doesn't require a target…, it could add mana to a player's mana pool when it resolves, and it's not a loyalty ability." → every `{T}, Remove …: Add …` ability here is a **mana ability** (`IsManaAbility = true`).

## Decision

MAST **describes** the counter-to-mana relationship the text states; it does not execute the pool mechanics. Three additions, no infrastructure/trait changes:

### 1. New `Quantity`: `CountersRemovedThisWayQuantity` (discriminator `countersRemovedThisWay`)
Models "for each [type] counter **removed this way**" — a cost-linked count, the sibling of `CounterCountQuantity` (counters *on* an object) and `KeywordCostPaidCountQuantity` (a keyword cost paid). The "this way" links it to the `RemoveCountersCost` on the *same* ability (reference-not-resolution, ADR 0004 — not a variable threaded from the cost).
- Field: `string CounterType` (e.g. `"storage"`, `"charge"`).

### 2. New `Quantity`: `AnyAmountQuantity` (discriminator `anyAmount`)
Models the cost "Remove **any number of** [type] counters" — an unbounded player choice. Distinct from `UpToQuantity` (bounded "up to N") and `VariableQuantity` (a named X with a defined value). Field-less. Reusable for any "any number of" text.

### 3. New field on `AddManaEffect`: `AnyCombinationOf` (`IReadOnlyList<string>?`)
Models "Add X mana **in any combination of** {W} and/or {U}" / "…of colors" — the produced mana is `Amount` units, each freely chosen from this color set. `["W","U"]` for the restricted set; `["W","U","B","R","G"]` for "of colors". Parallels the existing `AnyColor` bool (single mana, all five) but carries a count (`Amount`) and a possibly-restricted set. `Mana = ""` in this shape (color is the choice, like the `AnyColor` branch).

### Cost rule extension
`RemoveCountersCostRule` accepts two more counts beyond the existing literals:
- `Remove X [type] counters from this [noun]` → `Quantity = VariableQuantity.X`
- `Remove any number of [type] counters from this [noun]` → `Quantity = AnyAmountQuantity`

### Effect rule
`AddManaEffectRule` (or sibling rules ahead of its bail) parses S1–S5 instead of bailing. The `UnmodeledManaClause` guard stays as the backstop for *still*-unmodeled shapes (e.g. "spend this mana only to…", "until end of turn").

## Worked AST per shape (the gold spec)

S1 — Gyre Sage `{T}: Add {G} for each +1/+1 counter on this creature.`
```
AddManaEffect{ Mana:"{G}", Amount: CounterCountQuantity{ CounterType:"+1/+1", On:{Kind:Self} } }   // ability IsManaAbility:true
```
S2 — Hollow Trees `{T}, Remove any number of storage counters from this land: Add {G} for each storage counter removed this way.`
```
Costs: [ TapCost, RemoveCountersCost{ CounterType:"storage", Quantity: AnyAmountQuantity, Target:{Kind:Self} } ]
Effects: [ AddManaEffect{ Mana:"{G}", Amount: CountersRemovedThisWayQuantity{ CounterType:"storage" } } ]   // IsManaAbility:true
```
S3 — Calciform Pools `{1}, Remove X storage counters from this land: Add X mana in any combination of {W} and/or {U}.`
```
Costs: [ ManaCost{1}, RemoveCountersCost{ CounterType:"storage", Quantity: VariableQuantity.X, Target:{Kind:Self} } ]
Effects: [ AddManaEffect{ Mana:"", Amount: VariableQuantity.X, AnyCombinationOf:["W","U"] } ]   // IsManaAbility:true
```
S4 — Black Mana Battery `{T}, Remove any number of charge counters from this artifact: Add {B}, then add an additional {B} for each charge counter removed this way.`
```
Costs: [ TapCost, RemoveCountersCost{ CounterType:"charge", Quantity: AnyAmountQuantity, Target:{Kind:Self} } ]
Effects: [ CompositeEffect{ Effects:[
            AddManaEffect{ Mana:"{B}", Amount: LiteralQuantity.Of(1) },
            AddManaEffect{ Mana:"{B}", Amount: CountersRemovedThisWayQuantity{ CounterType:"charge" } } ] } ]
```
S5 — Kyren Toy `{T}, Remove X charge counters from this artifact: Add an amount of {C} equal to X plus one.`
```
Costs: [ TapCost, RemoveCountersCost{ CounterType:"charge", Quantity: VariableQuantity.X, Target:{Kind:Self} } ]
Effects: [ AddManaEffect{ Mana:"{C}", Amount: CalculatedQuantity{ BaseQuantity: VariableQuantity.X, Operation:"add", Operand:1 } } ]
```

## Consequences

- Unblocks the deferred storage/charge counter families (~dozens of cards: every storage land, every Mana Battery, the charge-counter artifacts).
- `AddManaEffect.Amount` semantics are finalized: when present, the produced mana is `Amount` units of `Mana` (single colour) **or** `Amount` units freely chosen from `AnyCombinationOf`.
- The `UnmodeledManaClause` backstop shrinks to only the genuinely-unmodeled tail (spend-restrictions already structured via `SpendRestriction`; "until end of turn" mana, ritual-style, remains future work).
- No infrastructure, base-type, or Effect-trait changes — two `Quantity` arms and one nullable field, all reflection-discovered.
