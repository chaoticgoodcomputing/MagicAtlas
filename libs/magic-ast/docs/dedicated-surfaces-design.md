# Dedicated-surface designs — the deferred hard cards (2026-06-17)

Implementation-ready specs for the cards the fan-out pilot deferred because they need bespoke
parser-surface design (not batch-cards). Grounded in the actual AST surfaces (verified present), so
implementation is a focused vertical slice per card, not open-ended design. See
[`parser-coverage-pilot.md`](parser-coverage-pilot.md) for why these were deferred (each failed twice
under fan-out: unanchored-regex overfit, then free-text residual).

**This is a value-ranked queue, not a graveyard.** "Deferred" is a *scheduling* verdict — fan-out can't
close these — never a value verdict; the hard cards are disproportionately the high-combo-value ones
(Rings of Brighthearth gates **124 combos**, The One Ring **58**). Each entry is titled with its combo
count, and the queue is worked **highest-value-first**: whenever the top entry out-values the current
batch's `fusedScore` leaders, the orchestrator runs it as a **dedicated single-card effort** (a batch of
one, on Opus, with the design budget the spec below already scopes) instead of another parse-family
batch. Refresh the counts from `interaction-triage-report.json` (`allComboBlockingCards` — the same
combo-value axis the parse and projection pick surfaces rank by). The list only shrinks by *closing*
cards, never by forgetting them.

## Rings of Brighthearth (124 combos)

> Whenever you activate an ability, if it isn't a mana ability, you may pay {2}. If you do, copy that
> ability. You may choose new targets for the copy.

**BUILD STATUS (2026-06-17):** Increment 1 **DONE + committed** — the 2 new AST surfaces landed
(`TriggeringAbilityIsManaCondition` + `ObjectReferenceKind.TriggeringAbility`; `TriggerEvent.AbilityActivated`
already existed). Compiles, schema regenerated, suite green. **Remaining:** the gold + the anchored parser
rule (the FAIL-prone piece — both prior attempts died on unanchored-regex overfit + free-text intervening-if).
The exact gold is now fully specified below (the `OptionalEffect{Inner: conditionalPay, IfYouDo}` wrapper is
the Nim-Deathmantle pattern, judge-verified), so the rule is the only open work.

**Reuses (verified present):** `ConditionalPayEffect` is wrapped by `OptionalEffect` — the canonical
"you may pay [cost]. If you do, [Y]" shape is `OptionalEffect { Inner: ConditionalPayEffect{Cost}, IfYouDo: Y }`
(see `Nim Deathmantle`, judge-PASS). `CopyEffect` already has `MayChooseNewTargets: bool?` + `Target`.

**Exact gold Output (use verbatim):**
```
TriggeredAbility {
  Trigger:       { Timing: Whenever, Event: AbilityActivated, Filter: { Controller: You } },
  InterveningIf: { ConditionType: triggeringAbilityIsMana, IsManaAbility: false },
  Effects: [ OptionalEffect {
      Inner:   ConditionalPayEffect { Cost: { CostType: mana, Symbols: [{ Kind: generic, GenericAmount: 2 }] } },
      IfYouDo: CopyEffect { Target: { Kind: TriggeringAbility }, MayChooseNewTargets: true } } ]
}
```
Remaining rule work: an ANCHORED trigger-condition rule for "you activate an ability" → `AbilityActivated`
(+ `Controller: You`), the intervening-if "if it isn't a mana ability" → the new condition, and the
optional-conditional-pay-then-copy effect. Verify no mislabel across the 11 "activate an ability" corpus
cards. (Superseded outline below kept for context.)

**Gold Output shape:**
```
TriggeredAbility {
  Trigger:      { Timing: Whenever, Event: ActivatesAbility, Filter: { Controller: You } },
  InterveningIf: <triggering ability is NOT a mana ability>,
  Effects: [ ConditionalPayEffect {
      Cost:   ManaCost {2},
      IfYouDo: CopyEffect { Target: <the triggering ability>, MayChooseNewTargets: true } } ]
}
```

**New surfaces (3, small):**
1. `TriggerEvent.ActivatesAbility` — new enum value (+ its `PortWalkProjection`/`known-coarse` entry if it
   forms an interaction edge; likely coarse for now). CR 603.2 / 602.
2. **Is-mana-ability intervening-if condition.** A real `[ConditionKind("triggeringAbilityIsMana")]`
   record (NOT a free-text `other` residual — that was the batch-4 FAIL). Carries a bool so the negation
   ("isn't") is structured. CR 605 (mana abilities).
3. **"that ability" reference.** `CopyEffect.Target` needs an `ObjectReference` for the triggering
   activated/triggered ability on the stack (CR 113 — an ability is an object). Add an
   `ObjectReferenceKind.TriggeringAbility` (or a filter `CardTypes:["ability"]` on `It`). Prefer a named
   kind for clarity.

**Parser rule:** ONE new `[TriggeredRule]`/`[TriggerConditionRule]` matching the full clause, **anchored**
(`^…$`) — the two prior FAILs were an unanchored `\byou activate an ability\b` that matched as a substring
inside more-specific triggers and dropped their filters. After implementing, run the parser over the 11
corpus cards containing "activate an ability" and confirm none are mislabeled.

**Note:** This card is the strongest argument for the `FANOUT §1.4` `[QualifierAxis]`/trigger-condition
reflection registry — the overfit hazard is structural, not a worker mistake.

## The One Ring (58 combos) — DEFERRED 2026-06-17: blocked on a parser-infrastructure ticket

> Indestructible
> When The One Ring enters, if you cast it, you gain protection from everything until your next turn.
> At the beginning of your upkeep, you lose 1 life for each burden counter on The One Ring.
> {T}: Put a burden counter on The One Ring, then draw a card for each burden counter on The One Ring.

**STATUS (2026-06-17):** Investigated end-to-end (corpus-seeded Input + actual-parser diff). Three of the
four abilities are clean new-file work; the **ETB trigger is blocked on a shared parser-infrastructure gap**
(self-by-name reference type for a NON-creature), so the card is **deferred to that infra ticket** rather
than force-fit (forcing it would mislabel the artifact as a creature — a false-correctness the judge guards
against). Indestructible already parses. The exact surfaces, verified against the AST + the live parser:

1. **ETB triggered** — `When The One Ring enters, if you cast it, you gain protection from everything until
   your next turn.` Gold: `Trigger{Enters, Filter:{IsSelf:true}}`, `InterveningIf:{castThisObject}`,
   `Effects:[ GainAbilityEffect{ Target:{You}, GainedAbility: StaticAbility{Protection,
   ProtectionEffect{From:[{Everything}]}}, Duration: UntilTime YourNextTurn } ]`.
   - ✅ **`ProtectionEffect` + `ProtectionQualityKind.Everything` already exist**; `GainAbilityEffect`
     (`Target`+`GainedAbility`+`Duration`) exists; `Duration.YourNextTurn` exists; `ObjectReferenceKind.You`
     exists. So "you gain protection from everything until your next turn" is a clean new-file `[TriggeredRule]`
     (player-gains-protection-with-duration).
   - ✅ "if you cast it" intervening-if → a new `[ConditionKind("castThisObject")]` boolean Condition + a
     `ConditionParser` arm. Small, new-file-ish (one AST node + one arm). CR 603.4.
   - ⛔ **BLOCKER — the trigger's self filter.** "When The One Ring enters" is a self-by-NAME reference.
     The parser resolves self-by-name via `TriggeredRuleHelpers.IsSelfByNameTrigger` / the self-by-name
     branch of `ParseObjectFilter`, which **hardcodes `CardTypes:["creature"]`** (the parser has no type-line
     access at that layer — `OracleParser.Parse(oracleText)` takes only text). For The One Ring (a pure
     Artifact) that yields a wrong `{creature, IsSelf}` filter. The fix is to make self-by-name **type-aware**
     (default creature; use the card's actual type when it has NO creature type — which leaves every existing
     creature/artifact-creature self-by-name gold UNCHANGED, verified regression-safe: no pure-non-creature
     self-by-name gold relies on the creature filter today). But the plumbing is **infrastructure**: the type
     would have to thread `CardParser → OracleParser → ParseTriggerCondition → ITriggerConditionRule.Match
     (~100 rules) → ParseObjectFilter`, i.e. an interface change across the whole condition-rule registry, or
     ambient parser state (concurrency-risky given `ParseCorpusStep`'s `AsParallel` note). That is a flagged
     Stop-condition (`OracleParser`/shared-helper infra) with corpus-wide blast radius (every self-by-name
     trigger reprojects) → re-parse + re-judge + corpus-edge-diff carve-outs. **It is its own ticket, not a
     batch/Phase-A slice.** (Note: existing non-creature self-ETB golds — Pirate's Cutlass, Bramble Armor,
     Ripclaw Wrangler — dodge this because they use "this Equipment/Vehicle enters", the `this [subtype]`
     path, not the by-name path.)

2. **Upkeep triggered** — `you lose 1 life for each burden counter on The One Ring.` Gold:
   `LoseLifeEffect{ Player:{You}, Amount: CounterCountQuantity{CounterType:"burden", On:{Self}} }`.
   ✅ All nodes exist (`LoseLifeEffect{Amount,Player}`, `CounterCountQuantity{CounterType,On}`); needs one
   new-file `[TriggeredRule]` for "you lose N life for each [counter] counter on [self]".

3. **Activated `{T}:`** — `Put a burden counter on The One Ring, then draw a card for each burden counter on
   The One Ring.` Gold: composite `[ PutCountersEffect{Target:{Self}, CounterType:"burden", Count: literal 1},
   DrawCardsEffect{ Player:{You}, Count: CounterCountQuantity{CounterType:"burden", On:{Self}} } ]`.
   ✅ All nodes exist (`PutCountersEffect{Target,CounterType,Count}` supports the named "burden" counter via
   its string `CounterType`). The current parser PARTIALLY parses this — it drops the put-counter clause and
   mis-scales the draw to literal 1 — so it needs a new-file activated rule (the "put a [named] counter on
   [self], then draw a card for each [named] counter on [self]" composite).

**Why deferred, not landed:** the gold is all-or-nothing (a gold may carry no `IUnparsed`), so all four
abilities must parse cleanly to land it. Surfaces (1-effect), (2), (3) are tractable new-file rules, but the
ETB trigger's correct self-filter requires the type-awareness infra above. Landing #1's effect/condition +
#2 + #3 without the trigger fix can't produce a valid gold. **Next step: do the self-by-name-type infra
ticket (regression-safe per the analysis above), then The One Ring is a clean ~4-rule slice.**

**Note:** `protection from everything` (CR 702.16) is reusable beyond this card; `CounterCountQuantity` over a
named counter is the reusable scaling primitive (Serum-Core Chimera "oil counter", etc.).

### JUDGE REVIEW (2026-06-17) — design validated; the heavy infra ticket is NOT strictly required

A rules judge (web + local CR) reviewed the rationale + design against CR and Scryfall/Gatherer rulings.
Verdict highlights:

1. **Citation fix:** the self-reference rule is **CR 201.5** ("text that refers to the object it's on by
   name means just that particular object…"), not CR 201.4 (which is "choose a card name"). The in-code
   comment at `ParseObjectFilter` (self-by-name branch) mis-cites 201.4 → fix to 201.5 when the slice lands.
   CR 707.10b reinforces: a copy's by-name self-reference tracks its own source.

2. **The owner's copy-semantics point is CORRECT and *confirms* the `IsSelf` model (not a gap).** By-name
   and by-type self-references both denote the SOURCE object; a copy's by-name ETB refers to the copy and
   fires on the copy's entry (CR 707.2 + 201.5); the legend rule (CR 704.5j, an SBA at CR 704.3) does NOT
   suppress an already-triggered ETB (CR 603.2/603.3). Modeling the trigger as `IsSelf:true` (+ the engine's
   separate "copy of X" node materialization) is exactly what makes "both ETBs fire" fall out correctly.
   Modeling by-name as a name-broadcast (non-self) filter would be a RULES ERROR.

3. **`castThisObject` intervening-if is correct + necessary.** A copy/reanimated/blinked entry is NOT
   "cast" (CR 601 vs 603; CR 707.10), so for those entries the ETB triggers but the "if you cast it"
   intervening-if FAILS (CR 603.4) → no protection. `IsSelf` and `castThisObject` are orthogonal and both
   correct; only the cast-from-hand entry grants protection.

4. **The deferral diagnosis is right, but the "infra-required" framing is OVERSTATED.** Since `IsSelf` is
   what the interaction engine actually gates on, the type label on a *self* filter is largely descriptive
   for reconstruction — it is NOT what bridges triggers. So a **scoped, regression-safe fix lands The One
   Ring without the corpus-wide type-threading infra:** make the by-name self-filter **omit the wrong
   `creature` type** for the non-creature case (or post-correct it in `CardParser`, which already has the
   type line). Existing creature golds keep `["creature"]`; no pure-non-creature self-by-name gold relies on
   the creature label (verified), so the scoped change is gold-regression-safe. The full
   `CardParser → OracleParser → ITriggerConditionRule` type-threading remains the right *general* solution
   (Stop-condition flag stands), but it is NOT a prerequisite to land this card correctly.

**Updated plan:** The One Ring is landable as a focused slice = (a) the scoped by-name self-filter
type-correction (omit creature for non-creatures), (b) the `castThisObject` ConditionKind + ConditionParser
arm, (c) the player-gains-protection-with-duration `[TriggeredRule]`, (d) the lose-life-per-counter
`[TriggeredRule]`, (e) the burden-counter put-then-draw activated rule. All AST nodes already exist.

## Carried FAILs (simpler — orchestrator fixes, branches preserved)

- **Hapatra, Vizier of Poisons** (`mast-tdd/parse-hapatra-vizier`): gold clean; the shared
  `TriggeredRuleHelpers.cs` change is an unanchored overfit mislabeling siblings. Fix = anchor the matcher
  (same class as Rings; fold into the §1.4 registry work).
- **Ulalek, Fused Atrocity** (`mast-tdd/parse-ulalek-fused-atrocity`): `CopyEffect` target filter drops
  the "other" qualifier (CR 109.5); fix = add `ExcludeSelf: true` to the copy filter (the `ExcludeSelf`
  machinery is already wired through `ObjectFilterRelations`).

## Recommended implementation order

1. The `FANOUT §1.4` reflection-seam registry first (it makes Rings' anchored trigger-condition new-file
   and retires the overfit class that blocks Rings + Hapatra).
2. Rings (3 surfaces, mostly reuse) → land it + Hapatra (same anchor pattern).
3. The One Ring (protection-from-everything is the one genuinely-new keyword).
4. Ulalek `ExcludeSelf` fix (trivial, independent).
