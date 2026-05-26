# MAST TDD briefing — batch 7 (autonomous run 3/10)

**Entering coverage:** 7,351 / 29,614 (24.82%). NUnit 334/0/334.

## Family A — LordPT "Other X you control" variant (cluster 1, +31 marginal)

**Parser file:** `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` — extend the existing LordPT recognizer.

### Cards (3 mechanical, helper-mech)
1. **King of the Pride** — `{2}{W}` Creature — `Other Cats you control get +2/+1.`
2. **Imperious Perfect** — `{2}{G}` Creature — `Other Elves you control get +1/+1.` (+ a token-creation activated ability — check if sibling parses)
3. **Elvish Clancaller** — `{G}` Creature — `Other Elves you control get +1/+1.` (+ activated tutor)

If 2 and 3's siblings don't parse, swap for simpler cards with ONLY the lord line. Pre-curate from corpus.

### Relevant rules
- **Rule 113.3** — granted-ability semantics for "[Filter] you control get X".

### AST types in scope
- `ModifyPTEffect` (existing). `Target: ObjectReference { Kind: Each, Filter: {...} }`.
- `ObjectFilter.Characteristics: ["other"]` — escape hatch for the "Other" qualifier (per existing convention in earlier batches).

### Expected generalization
The existing LordPT parser (added batch 2) handles `<filter> get +N/+M` with Color/Subtype/CardType filters. Extend the recognizer to match an optional leading `Other ` prefix, populate `Characteristics: ["other"]` when present.

### Anti-patterns
- Don't conflate with `creature gets` (singular target) — Lord is multi-target.

---

## Family B — Exalted + Infect keyword effects (clusters 3 + 4, +52 marginal combined)

**Parser file:** `libs/magic-ast/Parsing/Combinators/OracleParsers.cs` — extend `SimpleKeyword` chain.

### Cards (helper-novel writes both AST types + 5 fixtures)

Exalted (3 fixtures):
1. **Aven Squire** — `{1}{W}` Creature — `Flying\nExalted (Whenever a creature you control attacks alone, that creature gets +1/+1 until end of turn.)` (has Flying sibling — already parses)
2. **Qasali Pridemage** — `{G/W}` Creature — `Exalted (...)\n{1}, Sacrifice this creature: Destroy target artifact or enchantment.` (sibling has sacrifice cost — verify parses)
3. **Knight of Glory** — `{1}{W}` Creature — `Exalted (...)\nProtection from black` (sibling Protection — already exists)

Pre-curate single-line Exalted creatures from corpus if siblings cause bails.

Infect (2 fixtures):
1. **Phyrexian Crusader** — `{1}{B/P}{B/P}` Creature — `First strike, protection from red and from white\nInfect (...)`
2. **Glistener Elf** — `{G}` Creature — `Infect (...)` (single-line, perfect candidate)

Pre-curate more single-line Infect creatures if needed.

### Relevant rules
- **Rule 702.83 (Exalted)** — "Whenever a creature you control attacks alone, that creature gets +1/+1 until end of turn." Triggered ability mechanically; MAST models the keyword presence.
- **Rule 702.91 (Infect)** — "This creature deals damage to creatures in the form of -1/-1 counters and to players in the form of poison counters."
- **Glossary entries:** both present in `glossary.json`.

### AST types to add
- **`ExaltedEffect`** — `[OracleEffect("exalted")]`. NO parameters. Inherits `Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect`. Source: `libs/magic-ast/AST/Effects/Keyword/ExaltedEffect.cs`. Docstring: cite Rule 702.83. Doctrine note: although Exalted is mechanically a triggered ability, MAST models it as a keyword marker (same approach as Prowess from batch 4).
- **`InfectEffect`** — `[OracleEffect("infect")]`. NO parameters. Same inheritance. Source: `libs/magic-ast/AST/Effects/Keyword/InfectEffect.cs`. Docstring: cite Rule 702.91. Doctrine note: rules-engine handles the damage-redirection semantics.

### Expected generalization
Same pattern as last batch's Convoke / Defender additions. ONE entry each in `OracleParsers.SimpleKeyword` chain. Emit `StaticAbility { KeywordSource: "Exalted"|"Infect", Effect: ExaltedEffect{}|InfectEffect{} }`.

### Anti-patterns
- Don't expand Exalted into a triggered-ability structure. Descriptive keyword convention per `feedback_mast_describes_not_executes`.
- Same for Infect — don't model the damage-as-counters semantic.

---

## Dispatch plan

**Wave 1 (2 parallel):**
- `[sub:helper-novel]` (Opus) — Exalted + Infect AST types + 5 fixtures.
- `[sub:helper-mech]` (Sonnet) — 3 LordPT-Other fixtures.

**Wave 2 (2 parallel):**
- `[sub:mech]` (Sonnet) — Family A: extend LordPT parser in StaticAbilityParser.
- `[sub:mech]` (Sonnet) — Family B: extend OracleParsers SimpleKeyword for Exalted + Infect.

**Yield ceiling:** ~83 cards.
