# Batch 15 briefing — 2026-05-26

Two families this batch: ETB-destroy (mech) and Echo (helper-novel). Both pull from `TopYieldClusters[]` after dismissing cluster #1 (Affinity) as the known cost-modifier tarpit and cluster #3 (attack-trigger +N/+N) pending an investigation pass — its parser surface (`ModifyPTTriggeredRule`) already exists, so the failure is upstream and a single mech could destabilize it without isolation.

The two chosen families touch different parser surfaces (Triggered rules under `Parsing/Parsers/Triggered/Rules/` vs. `Parsing/Combinators/OracleParsers.cs` keyword chain), so there is no file conflict if dispatched in parallel.

---

## Family 1: (cluster #4 ETB-destroy, `TriggeredAbilityParser.Parse`)

**Failure signal:** triggered-ability trigger detection succeeds for "When this creature enters" (the enters branch at `TriggeredAbilityParser.cs:556`), but no `[TriggeredRule]` rule matches the effect text `"destroy target [filter]."` — so the dispatcher falls through and the ability lands as `UnparsedTriggered` (or `UnparsedAbility`). Cluster reports 21 lines, 15 cards yield.

### Cards in this family
1. **Ogre Arsonist** — `When this creature enters, destroy target land.` (single-line, single-ability)
2. **Viridian Shaman** — `When this creature enters, destroy target artifact.` (single-line, single-ability)
3. **Monk Realist** — `When this creature enters, destroy target enchantment.` (single-line, single-ability)
4. **Goblin Settler** — `When this creature enters, destroy target land.` (single-line, single-ability)
5. **Angel of Despair** — `Flying\nWhen this creature enters, destroy target permanent.` (Flying is an existing keyword; "permanent" filter is the widest case)

### Relevant rules
- **701.8 Destroy** — "To destroy a permanent, move it from the battlefield to its owner's graveyard." Only effects using the word "destroy", lethal damage, or deathtouch destroy a permanent (701.8b). MAST describes the keyword action, not the move-to-graveyard sequence — that's engine territory.
- **603.6a Enters-the-battlefield trigger** — "Enters-the-battlefield abilities trigger when a permanent enters the battlefield. These are written, 'When [this object] enters, …' or 'Whenever a [type] enters, …'" Trigger detection already handled by `TriggeredAbilityParser.Parse` for the enters branch.
- **109.1 / 109.2 "target [filter]"** — `target` is a single-object reference shaped by the immediately-following filter (`land`, `artifact`, `enchantment`, `permanent`, `nonland permanent`, …). MAST models this via `ObjectReference { Kind = Target, Filter: ObjectFilter }`.

### AST types in scope
- **`DestroyEffect`** — `[OracleEffect("destroy")]`. `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Source: `libs/magic-ast/AST/Effects/ZoneChange/DestroyEffect.cs`. Already used by `DestroyTargetSimpleRule`, `DestroyAllRule`, etc. Carries `Target: ObjectReference`.
- **`ObjectReference`** — `Kind` enum (`Target`, `Self`, `It`, `Each`, …), `Filter: ObjectFilter` (carries `CardTypes`, `Subtypes`, qualifiers like `IsMonocolored`).
- **`TriggeredAbility`** — `Kind = triggered`. Carries `Trigger: TriggerCondition`, `Effects: Effect[]`, optional `Reminder`. The trigger field will be populated by the existing enters trigger; the rule's job is to produce the `DestroyEffect`.
- **`ITriggeredRule`** — the registration interface. New rule file lives under `libs/magic-ast/Parsing/Parsers/Triggered/Rules/` decorated `[TriggeredRule]`. Mirror `SurveilTriggeredRule`, `ReturnToHandRule`, `LoseLifeDerivedRule` for the file shape.

### Expected generalization
One new `[TriggeredRule]` rule file — call it `DestroyTargetTriggeredRule.cs` — whose `TryMatch` accepts the post-trigger effect text `"destroy target <filter>."` and emits a `DestroyEffect` with `Target = ObjectReference { Kind = Target, Filter = { CardTypes = [filter-word] } }`. Filter words to handle: `land`, `artifact`, `enchantment`, `creature`, `permanent`, `nonland permanent`. Reuse `DestroyTargetSimpleRule`'s `ParseDestroyFilter` if its scope is import-friendly — same lexical surface, different host (triggered vs. spell). The discipline is **one** parser surface covering all five fixtures.

If the only way to make all 5 green is N separate methods (e.g., one per filter word), bail with sub-patterns — that signals filter-parsing is the real gap, not the rule itself.

### Anti-patterns
- Do not duplicate `DestroyTargetSimpleRule`'s body into the new triggered rule unless the existing helper is genuinely not reusable. Prefer factoring the shared filter-parse into a static helper.
- Do not edit `TriggeredAbilityParser.cs`. The new rule lives in its own file under `Triggered/Rules/`. Registration is reflection-discovered via `[TriggeredRule]`.
- Do not collapse "land" / "artifact" / "enchantment" / "permanent" into a single regex group that swallows arbitrary words. The filter is a constrained vocabulary; an over-eager match risks regressing other triggered rules.

### Glossary gaps
- None for this family.

---

## Family 2: (Echo keyword, `OracleParsers` SimpleKeyword chain)

**Failure signal:** Echo is not registered in the `OracleParsers` keyword `.Or()` chain in `libs/magic-ast/Parsing/Combinators/OracleParsers.cs`. Triage cluster #5 reports 24 lines, 16 cards yield. The keyword shape mirrors Persist (just-keyword) and Bestow (keyword + mana-cost parameter); Echo is parameterized like Bestow.

### Cards in this family
1. **Viashino Outrider** — `Echo {2}{R} (At the beginning of your upkeep, if this came under your control since the beginning of your last upkeep, sacrifice it unless you pay its echo cost.)` (single keyword line — cleanest fixture)
2. **Goblin War Buggy** — `Haste\nEcho {1}{R} (…)` (existing keyword sibling: Haste)
3. **Albino Troll** — `Echo {1}{G} (…)\n{1}{G}: Regenerate this creature.` (activated-ability sibling — existing infrastructure)
4. **Stingscourger** — `Echo {3}{R} (…)\nWhen this creature enters, return target creature an opponent controls to its owner's hand.` (ETB-return-to-hand sibling — `ReturnToHandRule` already exists for triggered rules)
5. **Hunting Moa** — `Echo {2}{G} (…)\nWhen this creature enters or dies, put a +1/+1 counter on target creature.` (or-coordinated trigger — may bail; included as stretch)

### Relevant rules
- **702.30a Echo** — "Echo is a triggered ability. 'Echo [cost]' means 'At the beginning of your upkeep, if this permanent came under your control since the beginning of your last upkeep, sacrifice it unless you pay [cost].'" Echo is fully reducible to a triggered-ability shape, but MAST models it as a keyword effect carrying the cost — the upkeep-trigger + conditional sacrifice is engine territory.
- **702.30b** — Urza-block Echo cards lacked an explicit cost in print; Oracle errata gave each one a cost equal to its mana cost. Every modern Oracle line carries an explicit `Echo {cost}` and the parser must require it.

### AST types you'll write
- **`EchoEffect`** — `[OracleEffect("echo")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect` (copy the trait set from `BestowEffect`). Field: `Cost: ManaCost` (the echo cost; non-null, same parsing path as Bestow's cost). Source location: `libs/magic-ast/AST/Effects/Keyword/EchoEffect.cs`.

### Parser surface you'll write
- A new `public static readonly TokenListParser<OracleToken, StaticAbility> Echo` in `OracleParsers.cs`, mirroring **Bestow** at lines 1115-1156: keyword token, `AtLeastOnce()` mana symbols, optional reminder. Add it to the `.Or()` chain near line 1189-1217 (current chain ends with `.Or(Bestow.Try())`).

### Expected generalization
All 5 fixtures use **one** keyword parser. The `Effect` field on `StaticAbility` is the new `EchoEffect { Cost = … }`; the reminder text deserializes onto `StaticAbility.Reminder` as for every other keyword. No conditional / per-card divergence.

### Anti-patterns
- Do not model the upkeep-trigger / sacrifice-unless-pay flow in the AST. The keyword effect carries only the cost — the trigger expansion lives in 702.30a and is engine territory (cf. `feedback_mast_describes_not_executes` and how Persist, Bestow, Bushido handle this).
- Do not invent a new `EchoTriggeredAbility` node or expand Echo into an explicit upkeep trigger. Mirror Persist / Bestow exactly: keyword effect, optional reminder, done.
- Do not require the reminder text. The Urza-block 702.30b note implies Oracle always supplies it now, but the parser should accept its absence (use `_optionalReminder` as Bestow does).

### Glossary gaps
- None.

---

## Cross-family notes

- The two families do **not** touch overlapping parser files. Family 1 adds a new file under `Parsing/Parsers/Triggered/Rules/`. Family 2 adds a new keyword parser in `OracleParsers.cs` plus a new AST type under `AST/Effects/Keyword/`. The `OracleParsers.cs` keyword chain edit (Family 2) is single-line in the `.Or()` cascade and conflict-free.
- Both families originate from `TopYieldClusters[]` (data-derived clustering), not `TopGaps[]`. The `LastAttemptedRule` mappings cited above are inferred — sub-agents should verify against actual triage telemetry if their bail signal contradicts the briefing.
- Multi-ability cards are deliberately included to exercise the per-card `Parser_ProducesExpectedOutput` gate. Helper-novel must produce gold ASTs for every line on its assigned cards (including Hasted / activated-ability siblings); mech must produce gold parsing for every line on its assigned cards (including Flying / other sibling keywords). Existing infrastructure covers all listed siblings.
