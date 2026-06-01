# Battle / Siege

## Status

Accepted (2026-06-01) — implemented in the same batch.

## Context

39 Battle cards (all "Battle — Siege", the March-of-the-Machine Invasions). Each is a transform DFC: front = `Battle — Siege`, back = the permanent/spell cast when the Siege is defeated. The parser already iterates DFC faces (`Output.Faces[]`), and "Battle" is already a known card type (`TypeLineParser`). Reminder-text capture already exists (`Ability.Reminder`, CR 207.2).

Every one of the 39 fronts has the **same two lines**:
1. `(As a Siege enters, choose an opponent to protect it. You and others can attack it. When it's defeated, exile it, then cast it transformed.)` — pure type-reminder, identical on all Sieges.
2. `When this Siege enters, [effect].` — a normal ETB trigger; the effects vary (deal damage, discard, search, gain life, …) and are **already supported** by existing effect rules.

Two gaps block all 39 fronts:
- **Gap B (the yield):** the self-reference vocabulary in `TriggeredRuleHelpers.ParseObjectFilter` lists `creature/land/artifact/enchantment/planeswalker/permanent/battle/aura/equipment/vehicle/spacecraft` — but **not `siege`**. Oracle text says "this **Siege**" (the subtype word), so the ETB trigger fails to resolve a subject → the whole ETB is `UnparsedEffect`.
- **Gap A:** line 1 is a standalone parenthetical with no host ability → `UnparsedAbility`.

The **defense value** (Battles' loyalty-like number, CR 310.6) is **absent from the MAST `Input` model** (`defense` is null in the corpus DTO) — so it is out of scope; MAST cannot model an input it does not receive.

Relevant rules (verbatim):
- **CR 310.1:** "A player who has priority may cast a battle card from their hand during a main phase of their turn when the stack is empty…"
- **CR 310.7:** "If a battle's defense is 0 and it isn't the source of an ability which has triggered but not yet left the stack, it's put into its owner's graveyard. (This is a state-based action…)"
- **CR 207.2:** "The text box may also contain italicized text that has no game function."

## Decision

Two changes, no infrastructure/trait edits.

### 1. Self-reference: add `"siege"` to the subtype self-noun list
In `TriggeredRuleHelpers.ParseObjectFilter`, append `"siege"` alongside `aura/equipment/vehicle/spacecraft`. "this Siege" → `ObjectFilter{ CardTypes: ["siege"] }` — **descriptive**, recording the word the text used (the same doctrine the existing comment states for vehicle/spacecraft subtype self-references; MAST does not resolve "Siege" up to "Battle"). This single entry unblocks all 39 ETB triggers; their effects already parse.

### 2. Siege reminder: a marker, not a drop
Recognize the standalone Siege reminder line and emit a `StaticAbility{ Effects: [SiegeEffect], Reminder: <verbatim parenthetical> }`, where **`SiegeEffect`** is a new field-less marker `Effect` (discriminator `siege`). This:
- **honors no-silent-drop** — the reminder is captured via the existing `Reminder` mechanism rather than discarded;
- **structurally marks** the Siege mechanic on the card's rules text (a query anchor), mirroring the keyword-marker doctrine (Crew/Bushido/Saddle are marker effects);
- leaves the **defeat → exile → cast-transformed** behavior (CR 310.6/310.7) as **engine territory** — described in the captured reminder, not modeled (MAST describes, does not execute).

**Alternative considered — strip the reminder:** rejected. The Siege-ness is in the type line, but the `Reminder` mechanism exists precisely to preserve no-game-function text; deliberately dropping a recognized reminder contradicts that, and a marker gives the captured text a structured host.

**Defense:** out of scope (absent from `Input`). If the `Input` model later carries `defense`, a `Defense` characteristic is a clean follow-up.

## Worked AST (the gold spec) — Invasion of Karsus // Refraction Elemental

Front face (`Invasion of Karsus`, `Battle — Siege`):
```
Abilities: [
  StaticAbility{ Effects: [ SiegeEffect ], Reminder: "(As a Siege enters, choose an opponent to protect it. You and others can attack it. When it's defeated, exile it, then cast it transformed.)" },
  TriggeredAbility{ Timing: When, Event: Enters, Filter: { CardTypes: ["siege"] },
                    Effects: [ DealDamageEffect{ Amount: 3, Target: <each creature and each planeswalker> } ] }   // "this Siege enters" → Self(siege)
]
```
Back face (`Refraction Elemental`, `Creature — Elemental`): its own abilities — `Ward—Pay 2 life` (static, the PayLifeCost Ward landed this session) + a cast-trigger `deals 2 damage to each opponent`. Parsed independently as a normal creature face.

## Consequences

- Unblocks **all 39 Siege fronts** corpus-wide (the self-noun entry is the lever; the effects already parse).
- A Battle **flips** (whole-card green) only when its **back face** also fully parses — back faces are ordinary permanents/spells, handled by the rest of the parser, so flips accrue as those families land; this ADR closes the *front/Siege* gap, not the back-face tail.
- One new field-less marker node (`SiegeEffect`) + one self-noun entry. No base-type/trait/infrastructure change. Defense deferred to an `Input`-model change.
