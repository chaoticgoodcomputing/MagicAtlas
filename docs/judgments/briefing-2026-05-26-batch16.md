# Batch 16 briefing — 2026-05-26

Two families this batch, both novel-shape:
- **Family 1: ETB-explore** — cluster #5 (15 yield). New `ExploreEffect` AST + new triggered rule.
- **Family 2: Enchanted-creature can't attack or block** — cluster #4 (15 yield). New `CantAttackEffect` AST + Aura-body parser surface.

Skipping cluster #1 (Affinity — known cost-modifier tarpit), cluster #2 (`<verb> target <type>.` — too coarse, needs per-verb investigation), cluster #3 (attack-trigger +N/+N — `ModifyPTTriggeredRule` exists; investigation deferred). The two chosen families touch disjoint parser files: Triggered/Rules (Family 1) and StaticAbilityParser (Family 2).

---

## Family 1: ETB-explore (cluster #5)

**Failure signal:** `When this creature enters, it explores. (reminder text)` — trigger detection succeeds (ETB-self branch), but no `[TriggeredRule]` knows the verb "explores." `ExploreEffect` does not exist in the AST.

### Cards in this family
1. **River Herald Scout** — `When this creature enters, it explores. (reminder)`
2. **Merfolk Branchwalker** — `When this creature enters, it explores. (reminder)`
3. **Pathfinding Axejaw** — `When this creature enters, it explores. (reminder)`
4. **Ixalli's Diviner** — `When this creature enters, it explores. (reminder)`
5. **Queen's Agent** — `When this creature enters, it explores. (reminder)`

All 15 cluster cards share the exact same oracle line (with the canonical 701.44a reminder). Variation lives only in P/T, mana cost, and other sibling abilities — picked the cleanest 5 single-line printings.

### Relevant rules
- **701.44a Explore** — "Certain spells and abilities instruct a permanent to explore. To do so, that permanent's controller reveals the top card of their library. If a land card is revealed this way, that player puts that card into their hand. Otherwise, that player puts a +1/+1 counter on the exploring permanent and may put the revealed card into their graveyard." Per MAST doctrine (`feedback_mast_describes_not_executes`): we record the keyword action's invocation and the subject; the reveal / counter / graveyard sequence is engine territory.
- **603.6a Enters-the-battlefield trigger** — already covered by `TriggeredAbilityParser.Parse` for `When this creature enters, …` (Family 1 of batch 15 leveraged the same path).
- **109.1 "it"** — In a triggered ability, "it" refers to the object that triggered the ability. In this construction, `it` is the entering creature, so the explore subject is `Self`.

### AST types you'll write
- **`ExploreEffect`** — `[OracleEffect("explore")]`. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect` (mirror trait set on every other effect type). Field: `Target: ObjectReference` (the exploring permanent). Source: `libs/magic-ast/AST/Effects/Counter/ExploreEffect.cs` OR `libs/magic-ast/AST/Effects/CardFlow/ExploreEffect.cs` — choose based on which directory's existing inhabitants are closest in spirit (Explore is hybrid card-flow + counter-placement, but the AST records it as one keyword-action effect so the directory split is cosmetic).

### Parser surface you'll write
- A new `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExploreTriggeredRule.cs`. Pattern of `TryMatch`: receive post-trigger effect text, look for `^it\s+explores\.?$` (case-insensitive, period optional), emit `ExploreEffect { Target = ObjectReference.It() }` (or `Self()` if the call-site convention is to map "it" to `It`; check what other triggered rules emit for the same subject pronoun — `ModifyPTTriggeredRule` uses `It()` for "it gets …").

### Expected generalization
All 5 fixtures use ONE triggered rule that matches the verb "explores" on subject "it." The reminder text strips off as usual (existing infrastructure from batch 12 — `_optionalReminder` / trailing-paren extraction).

### Anti-patterns
- Do NOT model the reveal/counter/graveyard flow in the AST. The keyword effect carries no fields beyond the subject — the 701.44a procedure is engine territory.
- Do NOT special-case the reminder text contents. The keyword's reminder is descriptive flavor; existing reminder-extraction handles it generically.
- Do NOT add a new ability-kind parser or modify `TriggeredAbilityParser.cs` — the new rule is reflection-discovered via `[TriggeredRule]`.

### Glossary gaps
- None. Explore is in `glossary.json` and 701.44 has full subrule coverage.

---

## Family 2: Enchanted-creature can't attack or block (cluster #4)

**Failure signal:** Aura body `Enchanted creature can't attack or block.` (subject = enchanted/equipped creature, two restrictions joined by "or"). Existing parser `TryParseCantBlock` in `StaticAbilityParser.cs:268` handles only `This creature can't block.` (subject = self, single restriction). No `CantAttackEffect` exists in the AST.

### Cards in this family
1. **Compulsory Rest** — `Enchantment — Aura ... Enchanted creature can't attack or block.`
2. **Cage of Hands** — same body line; also has a bounce-on-cost activated sibling.
3. **Luminous Bonds** — same body line; single-line printing.
4. **Cooped Up** — same body line; rummaging activated sibling.
5. **Choking Restraints** — same body line; tap-on-upkeep activated sibling.

All 15 cluster cards share the exact same body line. Variation lives only in other abilities on the card. **Helper-novel should prefer cards with single-line bodies** (Luminous Bonds, Compulsory Rest) to avoid sibling-shape complications; cards with activated siblings (Cage of Hands, Cooped Up, Choking Restraints) require the helper to verify those siblings already parse before committing the fixture.

### Relevant rules
- **509.1d Combat restriction** — "A creature 'attacks each combat if able'" is the canonical attack-side requirement; the inverse "can't attack" is a restriction in the same family. Rule 508 (declared attackers) is where these restrictions apply.
- **509.1c Block restriction** — `CantBlockEffect` already encodes this; see `libs/magic-ast/AST/Effects/Combat/CantBlockEffect.cs`.
- **702.5 / 303 Aura** — Aura permanents are attached to objects; abilities of the form "Enchanted [type] [restriction/effect]" describe the attached object using `ObjectReference.EnchantedOrEquipped` (existing convention from batch 5 Aura composite work).
- **Multi-effect bundling** (`feedback_mast_multi_effect_per_clause`) — One sentence, two restrictions joined by "or", is a multi-effect clause: emit two effects (`CantAttackEffect` + `CantBlockEffect`) under one static ability. Do NOT collapse them into a synthetic combined effect type.

### AST types you'll write
- **`CantAttackEffect`** — `[OracleEffect("cantAttack")]`. Mirror `CantBlockEffect` exactly (same file folder, same trait set, same `Target: ObjectReference` field). Source: `libs/magic-ast/AST/Effects/Combat/CantAttackEffect.cs`.

### Parser surface you'll write
- Either extend `TryParseCantBlock` in `StaticAbilityParser.cs` to also handle `cant attack`, OR add a new `TryParseCantAttackOrBlock` rule that emits multi-effect (two effects in one `StaticAbility.Effects[]` list). The latter is cleaner because the multi-effect bundling is a distinct shape from the single-restriction `This creature can't block.`
- The subject in the Aura body is `Enchanted creature` → `ObjectReference.EnchantedOrEquipped()` (cf. batch 5 Aura composite rule).
- Verify that `StaticAbility.Effects` is a list (multi-effect-per-ability) — if it's a single `Effect`, this is a trait-boundary call: STOP and report.

### Expected generalization
ONE parser surface in `StaticAbilityParser.cs` that handles "Enchanted creature can't attack or block." emitting two effects. Subject extraction (`Enchanted creature` → `EnchantedOrEquipped`) and dual-restriction parsing happen in the same method. All 5 fixtures use the same surface.

### Anti-patterns
- Do NOT invent a `CantAttackOrBlockEffect` combined type. The "or" in oracle text is a multi-effect clause boundary; the canonical descriptive shape is two effects, not one.
- Do NOT extend `CantBlockEffect` to carry an "also can't attack" flag. Effects are atomic; combinatorial flags break the discriminator-per-effect invariant.
- Do NOT model the engine-side effects of restrictions (illegal-attacker check, declared-attackers replacement). Just describe what oracle says.

### Glossary gaps
- None. Both restriction types are well-established in 509.1.

---

## Cross-family notes

- Files touched by each family:
  - **Family 1**: new `AST/Effects/{CardFlow|Counter}/ExploreEffect.cs`, new `Parsing/Parsers/Triggered/Rules/ExploreTriggeredRule.cs`.
  - **Family 2**: new `AST/Effects/Combat/CantAttackEffect.cs`, modified `Parsing/Parsers/StaticAbilityParser.cs` (new private `TryParse…` method + dispatch slot).
- No file overlap. The `StaticAbilityParser.cs` edit in Family 2 is the only shared parser-file modification and Family 1 doesn't touch it.
- If `StaticAbility.Effects` is a singular `Effect` field (not a list), Family 2 surfaces a trait-boundary decision and the mech must HALT with a report — that's a HITL call per the skill's stop conditions.
