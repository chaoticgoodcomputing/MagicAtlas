# 06 — Track B: Quantity Completion (SCOPE)

Scoping/measurement pass for Initiative 06's quantity arm. No engine/operator/AST/parser/gold
changes here — only this plan plus two `[Ignore]` target tests that document the desired behavior.

## TL;DR

- **The AST quantity model is already rich.** `libs/magic-ast/AST/Quantities/Quantity.cs` carries
  `literal`, `variable`, `derived`, `count` (with `CountOf: ObjectFilter` — the spec's "CountOf"),
  `counterCount`, `keywordCostPaidCount`, `countersRemovedThisWay`, `anyAmount`, `upTo`,
  `calculated`. **No new node family is warranted.**
- **The real gap is engine threading, in two places:**
  1. `PortGraph.Qty` (`PortGraph.cs:489-496`) maps only `literal`/`fixed` → a value and collapses
     **everything else → `null` (symbolic)`.
  2. The §8 balance/productivity (`PortGraphEngine.cs:515-643`) treats symbolic quantities
     conservatively (returns "can't prove" → no floor), and the doubler **Modifier edge**
     (`PortGraphEngine.cs:161-164`) intercepts a token emission **without multiplying** the
     loop's per-iteration production. So a token-doubler's surplus is invisible to balance.
- **A `Product`/`Sum` arithmetic node is NOT needed.** The only doubling in the gold is already a
  typed `ReplacementModifier {Type:"double"}` or a `calculated {Operation:"match"}` replacement —
  fold into `EdgeFamily.Modifier`, do not add a node. Corpus evidence below.

## The precise gap

### Gap 1 — `PortGraph.Qty` collapse

```csharp
private static int? Qty(JsonNode? quantity) =>
  quantity is null ? 1
  : quantity["QuantityType"]?.ToString() switch
  {
    "literal" or "fixed" => quantity["Value"]?.GetValue<int>(),
    _ => null,                       // ← everything else → symbolic
  };
```

`null` becomes `PortNode.Quantity = null`, the §8 "symbolic" sentinel. Used at five sites
(`PortGraph.cs:289,305,353,376` + the EmitPort token count). `count`/`calculated` quantities that
ARE resolvable in-loop are flattened here, before the engine ever sees their structure.

### Gap 2 — §8 balance / flooring is symbolic-blind, and the doubler doesn't multiply

`GatherManaFlow` returns `null` (→ `ManaBalanced`/`ManaProductive` both return `true`, no floor) the
moment any cost or producer quantity is `null` (`PortGraphEngine.cs:627,640`). So a symbolic
quantity does **not** floor a loop directly — it makes the balance test abstain. The actual flooring
that pins the bench Amber cohort is the **operator/Overlap** straddle (Squirrel ⊄ creature on the
death hop — see `PortGraphEngineTest.Reconstructs_the_chatterfang_pitiless_free_loop_as_amber`),
NOT the quantity collapse. **This is the load-bearing correction to the spec's framing:** the
quantity collapse blocks *certification of net-positive production*, it doesn't itself add the floor.

The doubler-specific gap: the Modifier edge (`replace:token-creation`) intercepts a token emission
but the intercept port carries no multiplier, so a Doubling-Season / Anointed-Procession doubler
contributes nothing to the loop's net token production. A loop that is net-zero tokens without the
doubler but net-positive WITH it cannot be certified, because the doubling lives only in
`ReplacementModifier.Type` and never reaches `PortNode.Quantity`.

## Measured cohort numbers

### QuantityType discriminators across the gold corpus (1126 fixtures, 572 carry quantities)

| QuantityType            | All fixtures | HandParsedCards | Interactions | KeywordExpansions |
|-------------------------|-------------:|----------------:|-------------:|------------------:|
| literal                 | 826          | 789             | 7            | 28                |
| variable                | 25           | 22              | 3            | 0                 |
| derived                 | 22           | 21              | 0            | 1                 |
| calculated              | 21           | 17              | 2            | 2                 |
| count (CountOf)         | 14           | 14              | 0            | 0                 |
| upTo                    | 9            | 9               | 0            | 0                 |
| keywordCostPaidCount    | 4            | 4               | 0            | 0                 |
| counterCount            | 4            | 4               | 0            | 0                 |
| countersRemovedThisWay  | 2            | 2               | 0            | 0                 |
| anyAmount               | 2            | 2               | 0            | 0                 |

Only `literal` (and absent → 1) resolves today; **every other form → symbolic.**

### Bench cohort (`tools/bench/MagicAtlas.Bench/bench-report.json`)

33 eligible combos · 0 Green · 8 Amber · 25 Missed (recall@Amber 0.2424). The 8 Amber are all the
Chatterfang / Warren-Soultrader / Pitiless-Plunderer token loops. Their Amber is the **operator
straddle** (Squirrel ⊄ creature death hop) or **mana-balance** (Chatterfang × Ruthless Knave
{2}{B}=3 vs 2 Treasures), per the existing engine tests — **not** a quantity collapse. The 25
Missed are flicker/storm/copy combos (Kiki-Jiki, Dualcaster, Ghostly Flicker, Displacer Kitten,
life-drain pairs) that the port model doesn't reconstruct at all — out of this track's scope.

**Conclusion:** the quantity threading converts *no* current bench combo Amber→Green on its own;
its payoff is gold-corpus loops with `count`/doubler production (the doubler tests below) and
correctly *keeping* unbounded-X loops Amber. The spec's "~1,400-card cohort" is the operator arm
(Track A), not this one.

### CountOf-resolvable-in-loop

All 14 `count` quantities in the gold are **static P/T derivations** ("gets +1/+0 for each artifact
you control" — Nim Lasher, Cephalopod Sentry, Zendikar Incarnate, Earth Servant, Benalish Honor
Guard, Sanctum of Stone Fangs, Rat Colony, Stoneforge Masterwork; "+2 for each Aura attached" —
Kor Spiritdancer, Strong Back). **None is a loop-emission-fed `create N tokens for each …` count.**
So today there is **zero** CountOf-resolvable-in-loop evidence in the corpus. The threading plan
must therefore implement the *capability* (a `count` whose `CountOf` is fed by the loop's own
per-iteration emissions resolves to that emission count) but is exercised by the synthetic doubler
test, not by an existing gold. The "for each token created" doubling that DOES appear is the
Chatterfang shape (a replacement, below), which is the Modifier path, not a `count`.

## Is a Product/Sum arithmetic node genuinely needed? — NO

Every "twice that many" / doubling gold (exhaustive enumeration):

1. **Doubling Season** (`HandParsedCards/RAV/DoublingSeason.json`) — "creates twice that many of
   those tokens instead" → `replacement`, `Modifier:{Type:"double"}`. Token + counter arms.
2. **Anointed Procession** (`HandParsedCards/AKH/AnointedProcession.json`) — token arm only,
   `Modifier:{Type:"double"}`.
3. **Bruvac the Grandiloquent** (`HandParsedCards/BruvacTheGrandiloquent.json`) — mill-doubler,
   `Type:"double"` (not a combo-token producer; irrelevant to loop balance but proves the pattern).
4. **Chatterfang** (`HandParsedCards/MH2/Chatterfang.json`, dup `Interactions/cards/Chatterfang.json`)
   — "those tokens plus that many Squirrels" → `replacement`, `OriginalEventOccurs:true`, with a
   `Replacement.createToken.Count = {calculated, Expression:"that many", Operation:"match"}`.

That is the complete set: **3 distinct doubler cards**, all already typed. The doubling is carried
by `ReplacementModifier.Type` ("double"/"triple") and `CalculatedQuantity.Operation` ("match"/
"multiply", with `Operand`) — both EXISTING shapes. A `Product(expr, n)` node would duplicate
`CalculatedQuantity {Operation:"multiply", Operand:n, BaseQuantity:…}`, which the corpus already
uses (Strong Back `multiply ×2`, Dread Slag `multiply ×-4`, Kyren Toy `add 1`). **Verdict: fold
doubling into `EdgeFamily.Modifier` (the implementation arm multiplies the intercepted emission's
quantity by the modifier's factor); add no Product/Sum node.**

## The threading plan (implementation arm — NOT done in this scope pass)

1. **`PortGraph.Qty` learns the existing nodes.** Extend the switch:
   - `literal`/`fixed` → `Value` (unchanged).
   - `count` → resolvable-in-loop sentinel: keep `null` for the static-derivation case, but tag the
     port with its `CountOf` filter so the engine can resolve it to a per-iteration emission count
     when the loop's own emissions match `CountOf` (the "create a token for each token created"
     case). Implemented as a dedicated resolution pass, not a literal in `Qty`.
   - `calculated {Operation:"multiply"/"double", Operand:n}` over a resolvable base → base × n.
   - `variable` (unbounded X), `anyAmount`, `derived`, `upTo`, `counterCount`,
     `keywordCostPaidCount`, `countersRemovedThisWay` → stay `null` (symbolic → Amber). Unbounded-X
     is deliberately NOT resolved: its value is a free choice, not a loop invariant.
2. **The §8 balance/Modifier edge learns the doubler.** When a Modifier edge intercepts a token
   emission with `ReplacementModifier.Type ∈ {double, triple}` (or a `match` replacement that adds a
   second equal batch), the engine multiplies the intercepted emission's per-iteration `Quantity`
   by the factor (×2 / ×3 / +1× for `match`) before the productivity test. A loop net-zero without
   the doubler but net-positive with it then certifies Green; a loop with no doubler and a symbolic
   X stays Amber.
3. **Conservative invariant preserved:** any remaining symbolic quantity on a relevant cost/producer
   still makes `GatherManaFlow` abstain (no false floor, no false Green). Unbounded variable →
   Amber, always.

## Deliverables in this scope pass

- This document.
- `tests/magic-ast-tests/Tests/Interaction/QuantityThreadingTargetTests.cs` — two `[Ignore("06 —
  pending quantity threading")]` tests: a known token-doubler loop certifies Green via a multiplied
  quantity; a known unbounded-X loop stays Amber. They SKIP (suite stays green) and document the
  target behavior.

## Out of scope

The operator arm (Track A: `IsSelf`/`Resource.Subject`), the flicker/copy/storm Missed combos, any
new AST node, the parser, the gold corpus, and the engine itself (this is measurement only).
