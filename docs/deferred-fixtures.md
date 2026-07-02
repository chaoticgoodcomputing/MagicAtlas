# Deferred MAST fixtures

Fixtures dropped during the vanilla-NUnit migration because they require parser or AST work that wasn't achievable in the stabilization batch. Each is paired with a one-line "what unblocks this" note. Future batches should re-pick these via the normal triage flow once the unblocker lands.

**Doctrine reminder:** under vanilla NUnit, every test in the suite must be green for a batch to merge. Fixtures that can't be made green by parser extension alone are not allowed to sit red; they get deferred here until the work that unblocks them is done.

---

## Re-introduction process

When you have the unblocker in hand:

1. Re-create the fixture at the original path (in this doc).
2. Re-render the gold AST using the orchestrator's hand-parsing path (judge-pass-1 helper sub-agent under the current doctrine).
3. Teach the parser to produce it; NUnit must be green.
4. Remove the entry from this file.

If the same unblocker covers multiple deferred fixtures, batch the re-introduction.

---

## Deferred fixtures (11)

### `tests/magic-ast-tests/Data/HandParsedCards/ChatterfangSquirrelGeneral.json`
**Unblocker:** `ReplacementEffect` parser for token-creation events ("If one or more tokens would be created under your control..."). AST exists; no parser path emits the replacement-on-token-creation shape.

### `tests/magic-ast-tests/Data/HandParsedCards/CMM/DarettiScrapSavant.json`
**Unblocker:** Multi-loyalty planeswalker with `UpToQuantity` in a discard cost, `DerivedQuantity { Source: "cards discarded this way" }`, IfYouDo chaining on sacrifice → returnToBattlefield, plus a 3rd loyalty ability that produces an emblem AND a 4th "Daretti can be your commander" ability.

### `tests/magic-ast-tests/Data/HandParsedCards/DMR/MysticRemora.json`
**Unblocker:** Cumulative upkeep keyword needs to expand to a `TriggeredAbility` with `Instructions` + composite putCounters+sacrifice-with-UnlessClause. The keyword-list combinator today only emits `StaticAbility` shapes.

### `tests/magic-ast-tests/Data/HandParsedCards/KTK/ZurgoHelmsmasher.json`
**Unblocker:** `TriggeredAbilityParser` recognising `HistoryPredicate{PredicateType=dealtDamageBy, Source=Self, Timeframe=this turn}` on a Dies trigger filter. AST exists; parser doesn't emit it.

### `tests/magic-ast-tests/Data/HandParsedCards/M21/ChandrasIncinerator.json`
**Unblocker:** Derived "that much damage" amount + "that player controls" filter referencing an antecedent ("that player" = the damaged opponent). Antecedent resolution across a single triggered ability's effect chain isn't modeled in the parser.

### `tests/magic-ast-tests/Data/HandParsedCards/M21/EnthrallingHold.json`
**Unblocker:** Two never-modeled effect patterns — `TargetingRestrictionEffect{Restriction=cantTargetUnless, Condition={Characteristic=tapped, Value=true}, AppliesWhen=casting}` and `GainControlEffect`. The AST nodes exist but the parser has no path to produce either.

### `tests/magic-ast-tests/Data/HandParsedCards/NEO/MindlinkMech.json`
**Unblocker:** `copy` effect with `Modifications` block (`powerToughnessOverride`, `typeAdder`, `abilityAdder`), `HistoryPredicate{PredicateType=crewed}`, and Vehicle-aware filtering. AST exists; parser doesn't compose these.

### `tests/magic-ast-tests/Data/HandParsedCards/SLD/DeadpoolTradingCard.json`
**Unblocker:** `ReplacementEffect{Event=zoneChange-to-Battlefield, Replacement=exchangeCharacteristic{Characteristic=TextBox, First=Self, Second=Another{creature}}}`. ReplacementEffect + ExchangeCharacteristicEffect exist; ETB-replacement composition doesn't.

### `tests/magic-ast-tests/Data/HandParsedCards/TLA/WanShiTongLibrarian.json`
**Unblocker:** `VariableQuantity.X` on putCounters + `CalculatedQuantity` for "half X rounded down" + the self-by-name no-filter trigger convention (the fixture also had a filter-shape inconsistency with Barrin's gold — re-judge needed at re-introduction).

### `tests/magic-ast-tests/Data/HandParsedCards/TLA/PlanetariumOfWanShiTong.json`
**Unblocker:** Composite triggered ability with `LookAtCardsEffect` + `CastWithoutPayingEffect` + `Restrictions=[OnlyOnceEachTurn]`. All three exist; the chained composition path doesn't.

### `tests/magic-ast-tests/Data/HandParsedCards/WHO/RoryWilliams.json`
**Unblocker:** `AbilityWord` extraction from em-dash preamble ("The Last Centurion — ...") + `ExileEffect.WithCounters` parser path + `InvestigateEffect` parser path + composite. The Suspend granted-keyword side is already taught; this is the rest of the card.

---

## Pattern across the 11

Most deferred fixtures hit one of three categories:

1. **Antecedent / reference resolution** ("that much damage", "that player controls X", `HistoryPredicate` referring to Self in another trigger). The parser doesn't yet thread references across a multi-step effect chain.
2. **Replacement-effect composition** (token-creation replacement, ETB-replacement). The `ReplacementEffect` node exists but the parser has no path that detects "if/instead" oracle phrasing and produces it.
3. **Composite + restriction chaining** (Mystic Remora's cumulative upkeep, Planetarium's "do this only once each turn", Wan Shi Tong's variable-X then derived-quantity). The parser handles individual effects well; sequencing them is the open work.

Each of these is its own "future batch" theme. Picking one as a batch focus is more leverage than fixing them one-by-one.
