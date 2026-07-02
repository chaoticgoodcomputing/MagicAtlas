# Combo-driven expressibility & fixture-migration audit (2026-06-01)

Combo cards are in combos *because* they're strange — they probe the AST's edges. Two distinct risks surface, and both compound the longer they sit:

1. **Fixture-migration debt** — landed gold that "passes" (`ParsedAbilities == TotalAbilities`) but encodes a value as **free text where structure exists or should**. Migrating means changing the emitting rule *and* re-authoring every affected gold. Cost scales with how long the convention accretes. (This is the [[project_mast_triage_false_coverage]] failure mode, now quantified.)
2. **Expressibility gaps** — shapes the AST genuinely cannot say yet. These need ADR-level design *before* a rule can parse them; attempting them without design produces either an `UnparsedEffect` (honest) or a lossy free-text parse (debt, per #1).

Counts below are over the **950 gold fixtures** + the corpus residual-debt metric (29,614 cards).

---

## Part 1 — Fixture-migration debt (free-text where structure exists)

| Convention | Gold fixtures | Corpus occurrences | Has structured home? |
|---|---|---|---|
| `Characteristic.Other` | **109** | 2,596 | **mostly yes** (see split) |
| `Condition.Other` | 53 | 1,051 | partly (ADR 0007 union) |
| `AbilityAdder/Token AbilityText` | 15 | 210 | partly (`KeywordAbility`) |
| `CalculatedQuantity.Expression` | 6 | — | partly (derived/operand) |
| `*.Instructions` | 3 | 109 | rarely (sanctioned tail) |

### 1a. `Characteristic.Other` — the priority migration (109 fixtures)
Sampling the distinct descriptions, the bucket splits ~60/40:

**Home exists today → mechanical migration** (change rule to emit the structured axis, re-author the gold):
- `noncreature` (7), `nonland` (9), `nonblue` (2), `nonwhite` (1) → `ObjectFilter.ExcludedCardTypes` / `ExcludedColors`. *(This is exactly the Displacer Kitten / Young Pyromancer / Spellgorger Weird convention the round-2 judge flagged — already replicated across landed gold.)*
- `artifact` (6), `black` (4), `sorcery` (1), `token` (5) → `CardTypes` / `Colors` / `IsToken`.
- `this permanent` (15), `this card` (4), `another` (7) → `Self` / `ExcludeSelf`.
- `shares a color` (1) → `SharesColorWith`; `with power less than this creature's power` (4) → `PowerComparison`; `with a +1/+1 counter` (2) → counter filter; `enchanted` (3) → the enchanted axis.

**No home → needs a new structured characteristic kind first** (~40%):
- **Combat state: `attacking` (9), `attacking or blocking` (5), `attacking alone` (4) = 18** — the single largest unstructured sub-bucket. Warrants a structured combat-state characteristic (attacking / blocking / attacking-alone).
- `without[keyword]`: `withoutFlanking` (3), `withoutFlying` (1) → a "lacks keyword" axis.
- `targeting this creature` (1), `with {X} in their mana costs` (1), `your commander` (1), `you own` (1).
- `other` (20) — generic, case-by-case.

**Recommendation:** a dedicated **`Characteristic.Other` migration pass** — the ~70 home-exists occurrences are largely scriptable (rule emits the structured axis; gold re-authored); the combat-state sub-bucket (18) is a small new-kind ADR that then unblocks its own migration. Do this **before** more combo batches add to it.

### 1b. `Condition.Other` (53 fixtures)
Per [ADR 0007](../../libs/magic-ast/docs/adr/0007-conditions-are-one-union.md) conditions are one union; these are the un-migrated tail. Sub-shapes: history ("an opponent lost life this turn", "you cast it", "you attacked this turn") → `HistoryPredicate` arms; counter-state ("it had no +1/+1 counters") → `CountCondition` over counters == 0; designations ("you have the city's blessing", "this permanent is saddled", "tribute wasn't paid") → designation/keyword-paid conditions. Mixed: ~half migratable to existing arms, half need new arms.

### 1c. `AbilityText` (15 fixtures)
Granted/token keywords as free text — `AbilityAdder.AbilityText: "haste"` where `KeywordAbility.Haste` exists (Kiki-Jiki added one this session). Where the granted ability is a known keyword, migrate to a structured `KeywordAbility`; genuinely-custom token abilities (rare) stay free-text by doctrine.

---

## Part 2 — Expressibility gaps (no home; need ADR-level design)

Each blocks specific top-combo cards and should get a design **before** a worker attempts it (else lossy parse):

| Gap | Blocks (top-combo) | Sketch |
|---|---|---|
| **Extra phases / turns** | Aggravated Assault ("additional combat phase, then additional main phase"); extra-turn spells | No AST for added turn structure. New `Effect` for added phase/turn (descriptive — what the text grants, not turn-state execution). |
| **Grant keyword to a zone w/ derived cost** | Underworld Breach ("each nonland card in your graveyard has escape; the escape cost is [mana] plus exile three…") | Grant a `KeywordAbility` to all cards in a zone, with a derived/composed cost. Hardest; likely its own ADR. |
| **Half-of-X derived quantities + rounding** | Peer into the Abyss ("half their library… half their life, round up"); Maddening Cacophony ("mill half their library, rounded up") | `DerivedQuantity` over a zone-count / life-total with a half + rounding modifier. `CalculatedQuantity` has half/round but the *base* (library size, life) needs a derived source. |
| **History-derived count** | Fraying Sanity ("mill X = cards put into graveyard this turn") | A this-turn-history count quantity (cards-to-graveyard-this-turn). |
| **Token-creation replacement** | Peregrin Took ("those tokens plus an additional Food are created instead") | `ReplacementEffect` on a token-creation event (the `TokenCreationEvent` precedent exists — feasible-ish). |
| **Put-from-hand w/ comparison** | Kodama of the East Tree ("put a permanent with equal or lesser mana value from your hand onto the battlefield") | A put-onto-battlefield-from-hand effect with an MV comparison gate. |

---

## Recommended sequencing

1. **Now (cheap, compounding): the `Characteristic.Other` migration pass** — ~70 mechanical migrations to existing axes (`ExcludedCardTypes`/`CardTypes`/`Colors`/`Self`/`ExcludeSelf`/`SharesColorWith`/`PowerComparison`). Biggest debt, mostly scriptable, and every future combo/anthem/filter batch keeps adding to it until the *rules* stop emitting `Characteristic.Other`.
2. **Small ADR + migration: combat-state characteristic** (attacking / blocking / attacking-alone, 18 occ) — unblocks the largest no-home sub-bucket and is self-contained.
3. **Per-gap ADRs, demand-driven:** design the Part-2 gaps as combo work reaches them (extra-phases and escape-to-zone are the heaviest; half-X and history-count are moderate). Author forward-looking red golds per the staged-red-gold doctrine ([[feedback_mast_staged_red_golds]]).
4. **Condition.Other / AbilityText** migrations: fold into the relevant family batches opportunistically, or a second migration pass after #1.

**Bottom line:** the expensive risk isn't worker errors (the judge catches those, e.g. Narset's `Kind:Target`→`Kind:It`); it's the ~180 gold fixtures already encoding free-text where structure exists. The `Characteristic.Other` pass is the highest-leverage, do-it-now item.
