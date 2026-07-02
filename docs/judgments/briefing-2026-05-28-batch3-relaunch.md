# MAST TDD — Batch 3 (relaunch) briefing — 2026-05-28

Fresh 20-family batch after discarding the prior batch-3 dispatch. Baseline (post batch-2): **Cards 13709/29614 (46.29%) · Lines 37394/56908 (65.71%) · Abilities 36061/55575 (64.89%)**. All 20 exemplars have `OtherUnparsedClusters=0` → each is a clean whole-card flip when its one target template parses. Every family creates/extends a **distinct file**; zero `AbilityClassifier.cs` edits expected → zero hot-file collisions.

## Invariants (all agents)
MAST DESCRIBES, does not execute. Gold = correct-parser output — NEVER `unparsed`/Diagnostics/Pattern/SourceSpan/RawText; never edit a fixture to pass. Card-scope: model EVERY ability on the card (non-target lines via existing AST — they already parse, since other=0). PascalCase props / camelCase discriminators; reuse existing nodes — check GLOSSARY.md before inventing. Do NOT edit infra (OracleParser, AbilityParserRegistry, PolymorphicReflectionConverter, OracleTokenizer, RuleRegistry, parser BODIES, `[PolymorphicBase]` base classes) or GLOSSARY.md. Reflection auto-discovers `[Keyword]`/`[SpellRule]`/`[StaticRule]`/`[TriggeredRule]`/`[TriggerConditionRule]`/`[Activated*Rule]` files. Reference keyword pattern: `CyclingEffect.cs` + `CyclingKeyword.cs`. Cite ONLY the rule numbers/text below — do not invent a number from memory.

---

## (A) Keywords — new `[Keyword]` + Effect node (Cycling-clone), zero collision. Model: Sonnet.

**1. Escalate** — exemplar **Savage Alliance** (`{2}{R}` Instant). Failure: `StaticAbilityParser.Parse`.
- **CR 702.120a:** "Escalate is a static ability of modal spells (see rule 700.2) that functions while the spell with escalate is on the stack. \"Escalate [cost]\" means \"For each mode you choose beyond the first as you cast this spell, you pay an additional [cost].\" Paying a spell's escalate cost follows the rules for paying additional costs in rules 601.2f–h."
- New `EscalateEffect{ Cost }`. Record cost only; the modal body ("Choose one or more —" + 3 modes) already parses (other=0 confirms `ModalEffect` handles it). Anti-pattern: re-modeling the modes, or treating Escalate as an alternative cost (it is *additional*).

**2. Mayhem** — exemplar **Electro's Bolt** (`{2}{R}` Sorcery, "deals 4 damage to target creature" + Mayhem). Failure: `StaticAbilityParser.Parse`.
- **CR 702.187b:** "\"Mayhem [cost]\" means \"As long as you discarded this card this turn, you may cast it from your graveyard by paying [cost] rather than paying its mana cost.\" Casting a spell using its mayhem ability follows the rules for paying alternative costs in rules 601.2b and 601.2f–h."
- New `MayhemEffect{ Cost }`. The damage line already parses. The discard/graveyard-cast is reminder text only. Cost-printed form only (`Mayhem {cost}`).

**3. Increment** — exemplar **Cuboid Colony** (`{G}{U}` 1/1, Flash / Flying,trample / Increment). Failure: `StaticAbilityParser.Parse`.
- **CR 702.191a:** "Increment is a triggered ability. \"Increment\" means \"Whenever you cast a spell, if this permanent is a creature and the amount of mana spent to cast that spell is greater than this creature's power or this creature's toughness, put a +1/+1 counter on this creature.\""
- New `IncrementEffect{}` — **bare keyword, NO cost**. Note in doc-comment it is a triggered ability per CR but printed as a bare structural keyword; MAST records the keyword's presence (describe, not execute). Flash/Flying/trample parse via existing keywords.

**4. Recover** — exemplar **Grim Harvest** (`{1}{B}` Instant, "Return target creature card from your graveyard to your hand." + Recover). Failure: `StaticAbilityParser.Parse`.
- **CR 702.59a:** "Recover is a triggered ability that functions only while the card with recover is in a player's graveyard. \"Recover [cost]\" means \"When a creature is put into your graveyard from the battlefield, you may pay [cost]. If you do, return this card from your graveyard to your hand. Otherwise, exile this card.\""
- New `RecoverEffect{ Cost }`. The return-from-graveyard spell line parses already. Record cost only; the trigger/return/exile is reminder.

**5. Web-slinging** — exemplar **Spider-Man, Web-Slinger** (`{2}{W}` 3/3 Legendary, single line: "Web-slinging {W}"). Failure: `StaticAbilityParser.Parse`.
- **CR 702.188a:** "Web-slinging is a static ability that functions while the spell with web-slinging is on the stack. \"Web-slinging [cost]\" means \"You may cast this spell by paying [cost] and returning a tapped creature you control to its owner's hand rather than paying its mana cost.\" Casting a spell using its web-slinging ability follows the rules for paying alternative costs in rules 601.2b and 601.2f–h."
- New `WebSlingingEffect{ Cost }`. Record the mana cost only; the "return a tapped creature" is an additional non-mana component of the alt cost expressed in reminder — if your alt-cost node can carry it, do so; otherwise cost-only is acceptable (it's reminder). Glossary confirms term (2026 Marvel set).

---

## (B) Spell rules — new `[SpellRule]` under `Spell/Rules/`; compose EXISTING effects. Model: Sonnet.

**6. Damage to any target + gain life** — exemplar **Sacred Fire** ("Sacred Fire deals 2 damage to any target and you gain 2 life." + Flashback, which parses). Failure: `SpellAbilityParser.YouLoseLifeSpellRule`.
- **CR 120.1:** "Objects can deal damage to battles, creatures, planeswalkers, and players… An object that deals damage is the source of that damage." **CR 119.3:** "If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly."
- One spell ability, two effects: `DealDamageEffect` (target = any target) + `GainLifeEffect`. Multi-effect-per-clause (one sentence, "X and you gain N"). Reuse existing nodes — do NOT create new damage/life nodes.

**7. Pump + gain keyword until end of turn** — exemplar **Dive Down** ("Target creature you control gets +0/+3 and gains hexproof until end of turn."). Failure: `SpellAbilityParser.YouLoseLifeSpellRule`.
- **CR 702.11b:** "\"Hexproof\" on a permanent means \"This permanent can't be the target of spells or abilities your opponents control.\"" **CR 613.1:** continuous effects apply in layers (P/T and ability-granting are continuous).
- `ModifyPTEffect` (+0/+3) + `GainAbilityEffect` (hexproof) on the target, both `UntilEndOfTurnDuration`. Reminder text ("It can't be the target…") is ignored. Reuse `HexproofEffect`/keyword via `GainAbilityEffect`.

**8. Destroy target land + its controller loses life** — exemplar **Spreading Rot** ("Destroy target land. Its controller loses 2 life."). Failure: `SpellAbilityParser.YouLoseLifeSpellRule`.
- **CR 701.8a:** "To destroy a permanent, move it from the battlefield to its owner's graveyard." **CR 119.3** (life loss, above).
- `DestroyEffect` (target land) + `LoseLifeEffect` whose subject is **the destroyed land's controller** (not "target player", not "you"). Two sentences = two effects in ONE spell ability (multi-effect-per-clause).

**9. Target player loses N and you gain N (drain)** — exemplar **Absorb Vis** ("Target player loses 4 life and you gain 4 life." + Basic landcycling, which parses). Failure: `SpellAbilityParser.YouLoseLifeSpellRule`.
- **CR 119.3** (above).
- `LoseLifeEffect` (subject = target player) + `GainLifeEffect` (subject = you). Distinct from family 8 by the loss subject (target player vs land's controller) — use a different `[SpellRule]` file. One sentence, two effects.

**10. Destroy N target lands** — exemplar **Rain of Salt** ("Destroy two target lands."). Failure: `SpellAbilityParser.YouLoseLifeSpellRule`.
- **CR 701.8a** (above). **CR 115.1:** "Some spells and abilities require their controller to choose one or more targets… These targets are declared as part of casting…"
- `DestroyEffect` over **two** target lands. Consult GLOSSARY for how an existing rule expresses "N target" / multiple targets (`Quantity` on the target filter, e.g. `LiteralQuantity 2`). Mirror an existing multi-target destroy if present; do NOT invent a new targeting model.

---

## (C) Triggered rules — new `[TriggeredRule]`/`[TriggerConditionRule]`; reuse effects unless noted.

**11. ETB: target creature can't block this turn** — exemplar **Goblin Shortcutter** (single line "When this creature enters, target creature can't block this turn."). Failure: `TriggeredAbilityParser.Parse`. **Model: Sonnet.**
- **CR 603.6a:** "Enters-the-battlefield abilities trigger when a permanent enters the battlefield. These are written, \"When [this object] enters, …\"" **CR 509.1** (declare-blockers restrictions).
- ETB trigger condition already parses; add a `[TriggeredRule]` effect rule emitting existing `CantBlockEffect` on a target creature with a this-turn duration. Reuse — `CantBlockEffect` exists.

**12. ETB: support N** — exemplar **Expedition Raptor** (Flying / "When this creature enters, support 2."). Failure: `TriggeredAbilityParser.Parse`. **Model: Opus (new node).**
- **CR 701.41a:** "\"Support N\" on a permanent means \"Put a +1/+1 counter on each of up to N other target creatures.\" \"Support N\" on an instant or sorcery spell means \"Put a +1/+1 counter on each of up to N target creatures.\""
- First check if `PutCountersEffect` can express "+1/+1 counter on each of up to N **other** target creatures" (an `UpToQuantity` over an other-creatures filter). If it can, reuse it inside a `[TriggeredRule]`. If not, create a tight `SupportEffect{ Quantity N }`. Flying parses. Duplicate-work guard: if a `SupportEffect` already exists in GLOSSARY when you read it, reuse it.

**13. Whenever you draw your second card each turn** — exemplar **Lat-Nam Adept** (single line "Whenever you draw your second card each turn, put a +1/+1 counter on this creature."). Failure: `TriggeredAbilityParser.Parse`. **Model: Opus (new trigger condition).**
- **CR 603.2:** "Whenever a game event or game state matches a triggered ability's trigger event, that ability automatically triggers."
- New `[TriggerConditionRule]` for the "you draw your second card each turn" event (check the `TriggerEvent` enum first — only `Dies` is enumerated today, so this is a new condition shape; describe it, don't model turn-state/counting machinery). Effect: existing `PutCountersEffect` (a +1/+1 counter on this creature). Describe the ordinal/per-turn qualifier as data on the condition; do not encode runtime counting.

**14. Subtype you control enters → return self to hand** — exemplar **Trial of Zeal** (`{2}{R}` Enchantment: "When this enchantment enters, it deals 3 damage to any target." [parses] + "When a Cartouche you control enters, return this enchantment to its owner's hand."). Failure: `TriggeredAbilityParser.Parse`. **Model: Opus.**
- **CR 603.6a** (enters triggers, above); **CR 603.2** (trigger events).
- The ETB-damage ability parses (other=0). The gap is a `[TriggerConditionRule]` for "a [subtype] you control enters" (a *whenever-another-permanent-enters* condition, distinct from this-object ETB) + existing `ReturnToHandEffect` returning **this** permanent. Model both abilities.

**15. ETB: investigate** — exemplar **Thraben Inspector** (single line "When this creature enters, investigate."). Failure: `TriggeredAbilityParser.Parse`. **Model: Sonnet.**
- **CR 701.16a:** "\"Investigate\" means \"Create a Clue token.\" See rule 111.10f."
- `InvestigateEffect` **already exists** — add a `[TriggeredRule]` binding it to the ETB trigger. Reminder text ("Create a Clue token. It's an artifact with…") is ignored. Reuse only.

**16. Combat damage to a creature → tap it + it doesn't untap** — exemplar **Kashi-Tribe Warriors** (single line "Whenever this creature deals combat damage to a creature, tap that creature and it doesn't untap during its controller's next untap step."). Failure: `TriggeredAbilityParser.Parse`. **Model: Opus.**
- **CR 510.1** (combat damage assignment); **CR 502.3:** "…effects can keep one or more of a player's permanents from untapping." **CR 603.2** (trigger events).
- New `[TriggerConditionRule]` "deals combat damage to a creature". Two effects: existing `TapEffect` (subject = that creature) + a "doesn't untap during next untap step" effect — check GLOSSARY for `DoesntUntapEffect`/`SkipUntapEffect`/`DoesntUntap*Duration` and reuse. Multi-effect-per-clause ("tap … and it doesn't untap").

**17. Whenever a player casts a [color] spell → you may gain life** — exemplar **Dragon's Claw** (`{2}` Artifact: "Whenever a player casts a red spell, you may gain 1 life."). Failure: `TriggeredAbilityParser.Parse`. **Model: Sonnet.**
- **CR 603.2** (trigger events, above).
- New `[TriggerConditionRule]` "a player casts a [color] spell" (color as a filter attribute) + existing `GainLifeEffect` wrapped as optional (`IOptionalEffect` / "you may"). Reuse the optional-effect trait; do not invent a new "may" node.

---

## (D) Static rules — new `[StaticRule]` under `Static/Rules/`; reuse effects.

**18. Chosen-type anthem** — exemplar **Etchings of the Chosen** (`{1}{W}{B}` Enchantment: "As this enchantment enters, choose a creature type." [parses] + "Creatures you control of the chosen type get +1/+1." + an activated indestructible ability [parses]). Failure: `StaticAbilityParser.Parse`. **Model: Opus.**
- **CR 613.1** (continuous-effect layers; P/T modification is layer 7c).
- `ChooseCreatureTypeOnEntryEffect` (landed batch 2 — read it) handles line 1; the activated line parses (other=0). The gap is line 2: a `[StaticRule]` anthem = `ModifyPTEffect (+1/+1)` over an `ObjectFilter` = "creatures you control **of the chosen type**". The filter must reference the chosen type (a back-reference to the entry choice) — describe it as a filter attribute, not a runtime lookup.

**19. Other creatures you control with [KW] get +X/+X** — exemplar **Windstorm Drake** (`{4}{U}` 3/3 Drake: Flying + "Other creatures you control with flying get +1/+0."). Failure: `StaticAbilityParser.Parse`. **Model: Sonnet.**
- **CR 613.1** (continuous P/T, above).
- `[StaticRule]` anthem = `ModifyPTEffect` over an `ObjectFilter` = "other creatures you control **with [keyword]**" (a has-keyword predicate on the filter). Flying parses. Distinct file from family 18 (filter is has-keyword, not chosen-type). Reuse `ModifyPTEffect`/`ObjectFilter`; extend the filter to carry a required-keyword predicate if it doesn't already.

**20. Threshold (ability word) — conditional buff** — exemplar **Mystic Zealot** (`{3}{W}` 2/4: "Threshold — As long as there are seven or more cards in your graveyard, this creature gets +1/+1 and has flying."). Failure: `StaticAbilityParser.Parse`. **Model: Opus.**
- **CR 207.2c:** "An ability word appears in italics at the beginning of some abilities. Ability words are similar to keywords… but they have no special rules meaning and no individual entries in the Comprehensive Rules. The ability words are … threshold …"
- `[StaticRule]`: an `AsLongAsDuration`/conditional (graveyard ≥ 7 cards) gating `ModifyPTEffect (+1/+1)` + `GainAbilityEffect (flying)` on this creature. The "Threshold —" prefix is an **ability word** (record it as an ability-word label if the AST has a slot; otherwise it is descriptive flavor with no rules meaning — do NOT treat it as a keyword or a separate ability). Check GLOSSARY for the existing as-long-as/condition shape.
