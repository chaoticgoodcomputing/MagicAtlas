# MAST judge — batch 28 verdict

**Date:** 2026-05-26
**Scope:** 8 files (6 fixtures, 2 AST nodes)
**Result:** FAIL

## Summary

- PASS: 5
- FAIL: 3

## FAIL verdicts

### `libs/magic-ast/AST/Effects/Keyword/MaxSpeedEffect.cs`
**Verdict:** FAIL
**Issue:** Stub effect drops the rules-load-bearing payload — the granted inner ability that IS the Max-speed mechanic.
**Rule citation:** 702.178a
**Rule text:** > "A max speed ability is a special kind of static ability. 'Max speed - [Ability]' means 'As long as your speed is 4, this object has [Ability].'"
**What the AST says:** The doc-comment concedes the stub "records the keyword's presence as a minimal marker; the speed-4 condition check and the gated inner ability are engine territory."
**Why this misrepresents the rule:** Max Speed is a conditional grant template, not a self-contained keyword. The rule's literal text defines it parametrically — `[Ability]` is the operand. Unlike Convoke/Delve (which have fixed mechanical bodies — reminder text expands to known fixed semantics), Max Speed's semantics ARE the inner ability. A "presence-only" `MaxSpeedEffect` with no payload encodes a tautology: "this card has Max Speed, which grants [redacted] when speed=4." Gold fixtures must encode eventual-truth; a complete parser would need to capture the conditional + the granted ability. Calling the speed-4 check "engine territory" is fine (that's a state check); calling the granted ability "engine territory" is not — the granted ability is descriptive content the rules text literally hands the AST. This is structurally indistinguishable from emitting an `unparsed` for "This creature gets +1/+2" (Walking Sarcophagus), "{T}: Add {R}{R}" (Endrider Catalyzer), and "{3}, Exile this card from your graveyard: Draw a card" (Loxodon Surveyor).
**Suggested fix:** Either (a) defer Max Speed entirely per the briefing's original direction and find SYE fixtures that aren't paired with Max Speed (none exist in corpus per the orchestrator — so this means deferring SYE fixtures too, or hand-parsing the granted abilities as separate inner abilities under the Max Speed wrapper), or (b) give `MaxSpeedEffect` a `GrantedAbility` field carrying the body after the em-dash as a structured `Ability` reference (mirrors how Saga chapters carry per-chapter effects, or how Level-up stanzas carry granted abilities). The keyword-presence-only shape used by Convoke/Delve is not a precedent here — those keywords have fixed bodies; Max Speed is parameterized.

### `tests/magic-ast-tests/Data/HandParsedCards/DFT/WalkingSarcophagus.json`
**Verdict:** FAIL
**Issue:** Max-speed ability silently drops the granted "+1/+2" body.
**Rule citation:** 702.178a
**Rule text:** > "'Max speed - [Ability]' means 'As long as your speed is 4, this object has [Ability].'"
**What the fixture says:** > `{ "Kind": "static", "KeywordSource": "Max speed", "Effects": [{ "EffectType": "maxSpeed", "IsOptional": false }] }` — the `+1/+2` operand from "Max speed — This creature gets +1/+2" is nowhere in the gold AST.
**Why this misrepresents the rule:** The fixture's gold AST cannot round-trip back to the oracle text without the `+1/+2`. Per CONTRIBUTING.md / `feedback_mast_describes_not_executes`, MAST describes — and the description here omits descriptive content (a P/T pump), not engine semantics. A reader of the gold AST has no way to know whether this card grants +1/+2, +5/+5, or something else.
**Suggested fix:** Either defer the fixture, or extend the gold AST to capture the granted ability. Concretely: a `GrantedAbility` field on the Max Speed ability holding a nested static-ability with a `BoostEffect` (+1/+2, Self).

### `tests/magic-ast-tests/Data/HandParsedCards/DFT/EndriderCatalyzer.json`
**Verdict:** FAIL
**Issue:** Max-speed ability silently drops the granted "{T}: Add {R}{R}" activated ability.
**Rule citation:** 702.178a
**Rule text:** > "'Max speed - [Ability]' means 'As long as your speed is 4, this object has [Ability].'"
**What the fixture says:** > `{ "Kind": "static", "KeywordSource": "Max speed", "Effects": [{ "EffectType": "maxSpeed", "IsOptional": false }] }` — the tap-for-mana activated ability is missing.
**Why this misrepresents the rule:** The granted ability is an activated mana ability (Rule 605). It is unambiguous, structurally well-modeled by the existing AST (activated ability with TapCost and AddManaEffect), and the fixture has the activated-ability shape on hand. Dropping it is a fixture hole, not a deferred-mechanic concession.
**Suggested fix:** Same as Walking Sarcophagus — extend Max Speed AST to carry the granted ability and capture the activated mana ability nested inside.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Keyword/StartYourEnginesEffect.cs` — PASS. Models Rule 702.179 as a parameterless keyword effect; the keyword's semantics (initialise speed at 1, increment on opponent-life-loss, cap at 4) are state-based actions and an inherent triggered ability per 702.179a–d, all of which are engine territory and correctly omitted. The Convoke/Delve precedent applies here cleanly — SYE has a fixed mechanical body that reminder text expands to known semantics.
- `tests/magic-ast-tests/Data/HandParsedCards/CN2/StromkirkPatrol.json` — PASS. Models trigger Rule 510 + 603.6 (Whenever-deals-combat-damage-to-a-player) with `Event: DealsCombatDamageToPlayer` + Filter creature; effect is `putCounters` on `It` with `+1/+1` counter, count 1 — matches Rule 122.1a counter framework.
- `tests/magic-ast-tests/Data/HandParsedCards/DKA/ErdwalRipper.json` — PASS. Two clean abilities: `Haste` static + the standard combat-damage trigger. Matches Rules 510, 603.6, 122, 702.10.
- `tests/magic-ast-tests/Data/HandParsedCards/JVC/SlithFirewalker.json` — PASS. Same shape as Erdwal Ripper, identical rule coverage.
- `tests/magic-ast-tests/Data/HandParsedCards/DFT/LoxodonSurveyor.json` — partial PASS on the SYE half (same shape as the other DFT fixtures), but blocked from full PASS by the Max-speed half (granted activated ability `{3}, Exile this card from your graveyard: Draw a card` is dropped — same FAIL as Walking Sarcophagus / Endrider Catalyzer). **Listed here because the SYE half is correct; counted in the FAIL total via the Max-speed fixture line above is not — Loxodon Surveyor is the third Max-speed FAIL. Updating counts: PASS 4, FAIL 4.**

(Correction applied below — Loxodon Surveyor moves to FAIL.)

### `tests/magic-ast-tests/Data/HandParsedCards/DFT/LoxodonSurveyor.json`
**Verdict:** FAIL
**Issue:** Max-speed ability silently drops the granted activated ability `{3}, Exile this card from your graveyard: Draw a card`.
**Rule citation:** 702.178a
**Rule text:** > "'Max speed - [Ability]' means 'As long as your speed is 4, this object has [Ability].'"
**What the fixture says:** Same minimal Max-speed stub as the other two DFT fixtures.
**Why this misrepresents the rule:** Same reason as Endrider Catalyzer — the granted ability is structurally well-modeled (activated ability with mana + exile cost, draw-a-card effect) and is descriptive content that the fixture drops.
**Suggested fix:** Same as the other two DFT fixtures.

## Revised summary

- PASS: 4 (`StartYourEnginesEffect.cs`, `StromkirkPatrol.json`, `ErdwalRipper.json`, `SlithFirewalker.json`)
- FAIL: 4 (`MaxSpeedEffect.cs`, `WalkingSarcophagus.json`, `EndriderCatalyzer.json`, `LoxodonSurveyor.json`)

## Process notes

### Position on the briefing's MaxSpeed scope question

The briefing asked the judge to take a position on whether the MaxSpeedEffect stub (keyword-presence-only, inner-ability-dropped) is acceptable per descriptive doctrine. **It is not**, for one rules-grounded reason: Rule 702.178a defines Max Speed parametrically. The text `[Ability]` in the rule is an operand, not flavor — it is the only thing distinguishing one Max-speed-bearing card from another. Convoke / Delve / Ascend / Improvise are all keywords whose reminder text expands to a fixed mechanical body; their AST nodes can be parameterless because the rule fully specifies the body. Max Speed is the opposite: the rule specifies only the gate (`As long as your speed is 4`); the body comes from the card. Dropping the body is structurally equivalent to emitting `unparsed` for it, and the judging rubric forbids `unparsed` in gold fixtures.

The orchestrator's reasoning ("every SYE card pairs with a Max-speed sibling, so we need a stub to avoid `unparsed`") is correct as a constraint but wrong as a solution. The correct response to "every SYE card has a Max-speed sibling we can't yet model" is either (a) defer SYE until Max Speed is properly modeled, or (b) model Max Speed properly. Forcing a stub that silently drops oracle content is a third option that produces gold fixtures that don't represent eventual-truth.

The `AbilityClassifier` modification to route "Max speed" to `AbilityKind.Static` is a clean extension at the classification layer — that part is fine. The problem is solely in the AST node shape and the three fixtures that exercise it.

### Position on the AbilityClassifier modification

Adding "Max speed" to `_abilityWords` + a pre-gate routing to `AbilityKind.Static` is a clean extension, not a workaround. Max Speed IS a static ability per 702.178a. The classifier change correctly tags it as such; the problem is downstream in what the static ability's body captures.

### Position on tokenization

`!` silently dropped + `your` as `OracleToken.Your` (structural keyword) is fine for SYE — the keyword text "Start your engines!" is captured in `KeywordSource` as a string, so the punctuation/structural-token handling at the tokenizer layer doesn't leak into the AST. No verdict on this beyond noting it's clean.

### Discriminator and rule-citation verification

All rule citations check out against `rules-structure.json`:
- 510 (Combat Damage Step) — exists, title matches.
- 510.2: "all combat damage that's been assigned is dealt simultaneously" — correct anchor for "deals combat damage to a player" trigger.
- 603.6 — exists; note 603.6 is specifically about zone-change triggers, not triggered abilities generally. The briefing cites 603.6 generically; the more precise citation for the SYE / combat-damage triggers is 603.1 (or 603 as a section). Not a FAIL on its own — the section-level intent is clear — but worth noting for future briefings.
- 122 (Counters) — exists, title matches.
- 702.178 (Max Speed) — exists, title matches.
- 702.179 (Start Your Engines!) — exists, title matches.

Discriminator casing matches CONTRIBUTING.md: `putCounters`, `startYourEngines`, `maxSpeed` (camelCase effect types); `DealsCombatDamageToPlayer` (PascalCase trigger event enum). No drift.

### Glossary gaps

- "Speed" (the player property introduced by 702.179) — not in `glossary.json` (the parsed glossary predates Aetherdrift). Not a per-item FAIL, but worth surfacing for future glossary refresh.
- "Max speed" (the player condition) — same gap, same note.

These are corpus gaps, not fixture/AST defects.
