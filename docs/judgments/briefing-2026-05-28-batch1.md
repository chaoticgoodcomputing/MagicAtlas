# MAST TDD — Batch 1 briefing (2026-05-28)

Autonomous run, batch 1 of 20. Theme: **alternative-cost keyword abilities** (the dominant top-yield surface — each bails at `StaticAbilityParser.Parse` because its combinator is unregistered) plus four non-keyword rule families. All 20 families create/extend **distinct files** — no cross-agent collisions.

## Architecture facts (read once — applies to all keyword families)

A keyword ability is recognized by the reflection-discovered **`KeywordRegistry`**. Each keyword is one file under `libs/magic-ast/Keywords/Definitions/{Name}Keyword.cs`, a `[Keyword]` class implementing `IKeyword`:

- `Tier` — `KeywordTier.Parameterized` (carries a cost/number) or `KeywordTier.Simple` (bare word).
- `Definition` — return `null` (combinator-only; the legacy expander is dead).
- `Combinator` — a Superpower `TokenListParser<OracleToken, StaticAbility>` producing a `StaticAbility { KeywordSource = "Name", Effects = [new XEffect{...}], Reminder = reminder }`.

**Reference implementations to copy:** `Keywords/Definitions/CyclingKeyword.cs` and `BestowKeyword.cs` (pure mana-cost keywords), `TypecyclingKeyword.cs` (keyword-prefix extraction). Shared combinator helpers in `Keywords/Definitions/KeywordCombinators.cs`: `Keyword("Name")`, `ManaCostSymbols` (→ `ManaCost`), `OptionalReminder` (→ `Parenthetical?`). Number token is `OracleToken.Number`; em/en-dash is `OracleToken.EmDash`.

**The keyword Effect node** lives at `libs/magic-ast/AST/Effects/Keyword/{Name}Effect.cs`: a `[OracleEffect("name")] sealed record XEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Copy `CyclingEffect.cs` verbatim and adjust the discriminator + parameter fields. Most carry `public required Cost Cost { get; init; }`. The discriminator string is the lowercase keyword name.

**Invariants (all agents):**
- MAST **describes, does not execute.** Record the keyword's presence and its printed parameters (cost, number, type). The runtime semantics (face-down 2/2, time counters, stacking, token copies) are conventionally inferred from the rules and captured as the `Reminder` parenthetical — do NOT model them as effect subtrees.
- **Card-scope:** every ability on the fixture card must be gold-modeled (the other lines on these cards — Flying, ETB, vanilla P/T — already parse, so model them with existing AST types).
- **Do NOT edit `KeywordCombinators.cs`.** If you need a custom combinator (number+dash+cost), define it as a `private static readonly` field inside your own keyword file.
- **Do NOT edit `OracleTokenizer.cs`, `OracleParser.cs`, `AbilityParserRegistry.cs`, or any `[PolymorphicBase]`.** If you think you need to, STOP and report.
- Fixtures immutable; never `"Kind":"unparsed"`; never touch `GLOSSARY.md`.

---

## Keyword families (pure mana-cost — Cycling/Bestow clones)

These are the simplest: `from kw in Keyword("Name") from cost in ManaCostSymbols from reminder in OptionalReminder select new StaticAbility {...}`. Create the `{Name}Effect` node (mirror `CyclingEffect`) with a `required Cost Cost`.

### Family 1: Disguise — `StaticAbilityParser.Parse`
**Fixtures:** Nightdrinker Moroii, Concealed Weapon. **Rule 702.168.** "Disguise {cost}" — cast face down as a 2/2 with ward {2}. New `DisguiseEffect { Cost }`, discriminator `"disguise"`. Other lines (Flying, ETB lose-life, equip-style P/T buff) use existing AST. Anti-pattern: don't model the face-down/ward mechanics — that's reminder text.

### Family 2: Mutate — `StaticAbilityParser.Parse`
**Fixtures:** Sea-Dasher Octopus, Porcuparrot. **Rule 702.140.** "Mutate {cost}". New `MutateEffect { Cost }`, discriminator `"mutate"`. Don't model the over/under-stacking — reminder text.

### Family 4: Encore — `StaticAbilityParser.Parse`
**Fixtures:** Exquisite Huntmaster. **Rule 702.142 (per glossary).** "Encore {cost}" — exile from graveyard to make token copies attacking each opponent. New `EncoreEffect { Cost }`, discriminator `"encore"`. The "When this dies, create a token" line is a separate triggered ability — model with existing `CreateTokenEffect`. Don't model the copy-per-opponent — reminder.

### Family 9: Offspring — `StaticAbilityParser.Parse`
**Fixtures:** Pawpatch Recruit, Bushy Bodyguard. Glossary: pay an additional cost as you cast to create a 1/1 copy token on entry. "Offspring {cost}". New `OffspringEffect { Cost }`, discriminator `"offspring"`. Don't model the token-copy — reminder.

### Family 10: Overload — `StaticAbilityParser.Parse`
**Fixtures:** Mind Rake, Blustersquall. **Rule 702.96.** "Overload {cost}" — a spell modifier. New `OverloadEffect { Cost }`, discriminator `"overload"`. The base spell line ("Target player discards two cards" / "Tap target creature you don't control") parses via existing spell rules. Don't model the target→each rewrite — reminder.

### Family 11: Scavenge — `StaticAbilityParser.Parse`
**Fixtures:** Terrus Wurm, Drudge Beetle. **Rule 702.97.** "Scavenge {cost}" — exile from graveyard, put +1/+1 counters equal to power on a creature. New `ScavengeEffect { Cost }`, discriminator `"scavenge"`. Don't model the counter placement — reminder.

### Family 14: Blitz — `StaticAbilityParser.Parse`
**Fixtures:** Girder Goons, Riveteers Decoy. **Rule 702.152.** "Blitz {cost}" — alternative cost granting haste + a death-draw + sacrifice. New `BlitzEffect { Cost }`, discriminator `"blitz"`. The other line (must-be-blocked / dies-create-token) parses via existing rules. Don't model the granted haste/draw — reminder.

### Family 15: Emerge — `StaticAbilityParser.Parse`
**Fixtures:** Wretched Gryff, Adipose Offspring. **Rule 702.119.** "Emerge {cost}" — cast by sacrificing a creature, cost reduced by its mana value. New `EmergeEffect { Cost }`, discriminator `"emerge"`. Don't model the sacrifice/reduction — reminder.

### Family 16: Miracle — `StaticAbilityParser.Parse`
**Fixtures:** Vanishment, Thunderous Wrath. **Rule 702.94.** "Miracle {cost}" — reduced cost if first card drawn this turn. New `MiracleEffect { Cost }`, discriminator `"miracle"`. The base spell line parses via existing rules. Don't model the draw-trigger — reminder.

### Family 13: Assist — `StaticAbilityParser.Parse`
**Fixtures:** Bring Down, Charging Binox. **Rule 702.132.** "Assist" — **bare keyword, no printed cost** (the amount lives in reminder text: "Another player can pay up to {3}…"). `Tier = Simple`. Combinator: `Keyword("Assist") + OptionalReminder`. New `AssistEffect { }` (no Cost field), discriminator `"assist"`. The base spell/other lines parse via existing rules.

---

## Keyword families (custom combinator — number/dash/prefix)

### Family 3: Suspend — `StaticAbilityParser.Parse`  [reuses existing Effect]
**Fixtures:** Durkwood Baloth (Suspend only), Keldon Halberdier (First strike + Suspend). **Rule 702.62.** Printed "Suspend N—{cost}" (e.g. "Suspend 2—{G}"). **`SuspendEffect` ALREADY EXISTS** (`AST/Effects/Keyword/SuspendEffect.cs`) with `Quantity? N` and `Cost? Cost` — reuse it, do NOT create a new effect. Combinator must parse: `Keyword("Suspend")`, then `OracleToken.Number` → `N` (use `LiteralQuantity.Of(int)`), then `OracleToken.EmDash`, then `ManaCostSymbols` → `Cost`, then `OptionalReminder`. Define the number/dash parsing inline in your file.

### Family 12: Awaken — `StaticAbilityParser.Parse`
**Fixtures:** Clutch of Currents, Coastal Discovery. **Rule 702.113.** Printed "Awaken N—{cost}" (e.g. "Awaken 4—{5}{U}"). Same N—cost shape as Suspend. New `AwakenEffect { Quantity? N; Cost Cost }`, discriminator `"awaken"` (mirror `SuspendEffect`'s N+Cost shape). The base spell line ("Return target creature…" / "Draw two cards") parses via existing rules. Don't model the +1/+1-counters-on-land / becomes-creature — reminder text.

### Family 6: Splice — `StaticAbilityParser.Parse`
**Fixtures:** Kodama's Might, Into the Fray. **Rule 702.47.** Printed "Splice onto Arcane {cost}". Combinator: `Keyword("Splice")`, then word "onto", then a subtype word ("Arcane"), then `ManaCostSymbols`, then reminder. New `SpliceEffect { string Subtype; Cost Cost }`, discriminator `"splice"` (record the spliced-onto subtype, e.g. "Arcane", and the cost). The base spell line parses via existing rules. Don't model the text-grafting — reminder.

### Family 5: Escape — `StaticAbilityParser.Parse`
**Fixtures:** Glimpse of Freedom (Draw a card + Escape), Sweet Oblivion. **Rule 702.138.** Printed "Escape—{cost}, Exile N other cards from your graveyard." The escape cost is the mana cost PLUS the graveyard exile. Combinator: `Keyword("Escape")`, `EmDash`, `ManaCostSymbols`, then the comma + "Exile N other cards…" clause, then reminder. New `EscapeEffect { Cost Cost; Quantity? CardsToExile }`, discriminator `"escape"` — record the mana cost and the count of cards to exile (the additional cost). The base spell line parses via existing rules. **Judgment call:** how much of the "Exile N other cards" clause to structure vs. leave to reminder — record the count as a quantity; do not model the exile as a separate `ExileEffect` subtree.

### Family 7: Prototype — `StaticAbilityParser.Parse`
**Fixtures:** Goring Warplow, Combat Thresher. **Rule 702.160 / Rule 718.** Printed "Prototype {cost} — P/T" (e.g. "Prototype {1}{B} — 1/1"). Combinator: `Keyword("Prototype")`, `ManaCostSymbols`, `EmDash`, then power `Number` `/` toughness `Number`, then reminder. New `PrototypeEffect { Cost Cost; string Power; string Toughness }`, discriminator `"prototype"` — record the alternate cost and the prototype P/T. The other lines (Deathtouch etc.) parse via existing rules. Don't model the "keeps abilities and types" — reminder.

### Family 8: Landcycling — `StaticAbilityParser.Parse`  [extends existing keyword]
**Fixtures:** Mental Journey (Draw three + Basic landcycling), Ash Barrens. **Rule 702.29 (cycling variant).** Printed "Basic landcycling {cost}" and plain "Landcycling {cost}". **Reuse the existing `TypecyclingEffect`** (`Type` string + `Cost`). The existing `TypecyclingKeyword.cs` catch-all matches single words ending in "cycling" (Plainscycling, Forestcycling) but NOT the two-word "Basic landcycling" (first token is "Basic") nor bare "Landcycling". **Extend `TypecyclingKeyword.cs`** (you are the only agent touching it) to also recognize: `Keyword("Basic") + Keyword("landcycling")` → `Type = "Basic land"`, and bare `Keyword("Landcycling")` → `Type = "Land"`. Keep the existing catch-all working. Don't create a parallel effect type.

---

## Non-keyword rule families

### Family 17: Sacrifice-unless-pay upkeep trigger — `TriggeredAbilityParser.Parse`
**Fixtures:** Whipstitched Zombie, Wild Leotau (both single-line). Oracle: "At the beginning of your upkeep, sacrifice this creature unless you pay {cost}." The trigger timing ("beginning of your upkeep") already parses; the gap is the **effect side**: "sacrifice <self> unless you pay <cost>". This is a new `[TriggeredRule]` effect file under `Parsing/Parsers/Triggered/Rules/`. Use the existing self-sacrifice effect + the `UnlessClause` trait (`IPreventableEffect`) for "unless you pay {cost}" — consult GLOSSARY.md for the sacrifice effect type and `UnlessClause` shape. Fixture cards' Flying line parses already. Anti-pattern: don't invent a new trigger condition — the timing is handled; this is purely an effect-clause rule.

### Family 18: Create-N-typed-tokens-with-keyword spell — `SpellAbilityParser`
**Fixtures:** Talrand's Invocation ("Create two 2/2 blue Drake creature tokens with flying."), Flurry of Horns ("Create two 2/3 red Minotaur creature tokens with haste."). `CreateTokenEffect` (`AST/Effects/TokenCopy/CreateTokenEffect.cs`) and a spell-side `CreateTokenRule.cs` already exist — the gap is the **plural count + granted keyword ("with flying")** shape. Extend `CreateTokenRule.cs` (you are the only agent touching it) or add one new `[SpellRule]` covering "Create N [P/T] [color] [subtype] creature tokens with [keyword]." Read `CreateTokenEffect` for the count/granted-keyword fields. Anti-pattern: one consolidated rule for both fixtures — not two.

### Family 19: Spell deals N damage to attacking/blocking creature — `SpellAbilityParser.YouLoseLifeSpellRule`
**Fixtures:** Divine Arrow ("Divine Arrow deals 4 damage to target attacking or blocking creature."), Cosmium Blast (same shape). A `SelfDealsDamageToAttackingOrBlockingCreature` rule exists on the **activated** side; this is the **spell** analogue (the source is the spell itself, named, not an activated permanent). Add one new `[SpellRule]` under `Parsing/Parsers/Spell/Rules/` using existing `DealDamageEffect` (`AST/Effects/Damage/DealDamageEffect.cs`) targeting an attacking-or-blocking creature filter. Mirror the existing activated rule's ObjectFilter for "attacking or blocking creature."

### Family 20: Can't-be-blocked-by-[subtype] static — `StaticAbilityParser.Parse`
**Fixtures:** Bog Rats ("This creature can't be blocked by Walls."), Rampart Crawler (same). An `EvasionRule.cs` + `EvasionEffect`/`EvasionCondition` already exist (`Parsing/Parsers/Static/Rules/EvasionRule.cs`, `AST/Effects/Keyword/EvasionEffect.cs`). The gap is the **"can't be blocked by [creature subtype]"** condition. Extend `EvasionRule.cs` (you are the only agent touching it) or add one new `[StaticRule]` producing an `EvasionEffect` with the appropriate `EvasionCondition` carrying the blocker subtype. Read `EvasionCondition`/`EvasionConditionType` for the available shapes before authoring.
