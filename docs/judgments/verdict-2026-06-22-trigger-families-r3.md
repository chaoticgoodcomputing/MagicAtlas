# MAST judge — batch verdict

**Date:** 2026-06-22
**Scope:** 12 targets (7 fixtures, 5 AST/parsing rules) across a 5-family long-tail trigger-parsing batch (round 3) on `feat/mast-improvements`
**Result:** FAIL

## Summary

- PASS: 13
- FAIL: 1

All 5 new parsing rules PASS (sound modeling + every cited CR rule cross-referenced against `rules-structure.json`). Six of seven fixtures PASS. One fixture (`ChampionsOfThePerfect.json`) FAILs for a silently-dropped oracle line.

## FAIL verdicts

### tests/magic-ast-tests/Fixtures/HandParsedCards/ChampionsOfThePerfect.json
**Verdict:** FAIL
**Issue:** The "As an additional cost to cast this spell, behold an Elf and exile it" line — the CR 607 linked-ability **producer** — is silently dropped from the gold. It appears in `Output.Oracle.RawText` but is encoded nowhere in `Abilities` or `Attributes` (no `additionalCosts` attribute).
**Rule citation:** CR 607.1 (Linked Abilities)
**Rule text:** > "An object may have two abilities printed on it such that one of them causes actions to be taken or objects or players to be affected and the other one directly refers to those actions, objects, or players. If so, these two abilities are linked: the second refers only to actions that were taken or objects or players that were affected by the first."
**What the fixture says:** The `returnToHand` consumer references `Target.Filter { Zone: Exile, ExiledWith: { Kind: Self } }` — a linked reference to a card exiled by this object — but the fixture contains no producer that performs that exile. `Attributes` is only `[manaCost, colors, colorIdentity, creatureStats]`.
**Why this misrepresents the rule:** The exile-as-additional-cost is the producer half of the CR 607 link; the LTB return depends on it to define "the exiled card." Dropping it leaves the consumer dangling and omits a full line of rules-meaningful structure. The codebase already models this exact shape: the documented sibling exemplar Petravark carries BOTH halves (an `exile` producer ability + a `returnToBattlefield` consumer with `ExiledWith: Self`), and three additional-cost golds — `WWK/BoneSplinters.json`, `EMN/InfernalPlunge.json`, `USG/Raze.json` — model "As an additional cost to cast this spell, <X>" as an `additionalCosts` attribute carrying a structured `Cost` node. "Could a structured node express this?" — yes, demonstrably.
**Suggested fix:** Add an `additionalCosts` attribute to the Champions gold encoding "behold an Elf and exile it" as a structured exile cost (filter: an Elf you control OR an Elf card from your hand — a disjunction over zone/control), with the exile parametrized so it is the producer the `ExiledWith: Self` consumer references. If the `behold` / exile-as-cost producer node or its parser slice does not yet exist, route to design rather than leaving the line unrepresented — do not land a gold that asserts an incomplete parse as truth.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MiasmicMummy.json` — PASS. ETB(Enters, IsSelf) -> `discardCards` Count literal 1, Player `EachPlayer`; faithful to CR 701.9 Discard.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/RottingRats.json` — PASS. Same each-player discard + Unearth keyword effect with its mana cost; both lines parsed.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/EachOpponentDiscardsRule.cs` — PASS. EachPlayer/EachOpponent scope + literal count; cites Discard **701.9** (correctly NOT Destroy 701.8).
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AngelOfFinality.json` — PASS. Flying evasion + ETB `exile` `Each`/`card`/`Owner:Target`/`Zone:Graveyard` (whole-zone exile, CR 701.13a / 404.1); all lines parsed.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/BojukaBog.json` — PASS. enters-tapped static + ETB exile (Owner:Target — oracle says "target player's") + {T}:Add{B} mana ability; all three lines parsed.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExilePlayerGraveyardZoneRule.cs` — PASS. `ExileEffect` `Kind:Each` over `Filter{card, Owner, Graveyard}`, Owner=Target/Opponent by scope; cites Exile 701.13a + owner 108.3 + zone 406 — all verified.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/BreedingPit.json` — PASS. upkeep `preventable{ sacrifice Self, Unless pay {B}{B} }` + end-step Thrull token (0/1 black creature Thrull); both lines parsed, cost correct.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/Thirst.json` — PASS. Enchant creature + ETB tap enchanted + doesn't-untap static + upkeep `preventable{ sacrifice Self, Unless pay {U} }`; all four lines parsed.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SacrificeSelfUnlessPayRule.cs` — PASS. `Preventable{ sacrifice Self }` + `UnlessClause{ You, mana cost }`; cites Sacrifice 701.21, not-automatic 118.5, and Echo 702.30a (whose text is the verbatim "sacrifice it unless you pay [cost]" templating) — all verified.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/WorldspineWurm.json` — PASS. Trample + dies->3x 5/5 green Wurm w/ trample + `PutIntoGraveyard`(IsSelf, from-anywhere) -> `shuffleIntoLibrary` Self; this is the literal CR 701.24 Guile example, correctly using PutIntoGraveyard (broader than Dies) for the second trigger.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SelfPutIntoGraveyardFromAnywhereConditionRule.cs` — PASS. `TriggerEvent.PutIntoGraveyard` (IsSelf), correctly distinguished from Dies (CR 700.4 battlefield->graveyard); cites 603.6 / 700.4 / 404.1 / 201.5 — all resolve.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ShuffleSelfIntoLibraryTriggeredRule.cs` — PASS. `ShuffleIntoLibraryEffect` Target Self; reuses existing discriminator; CR 701.24's own CR example IS this family.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReturnExiledCardToHandOnLeaveTriggeredRule.cs` — PASS. LTB(IsSelf) -> `returnToHand` `Designated{ Zone:Exile, ExiledWith:Self }` (linked reference, not free text); correctly carries NO `UnderControl` (hand has an owner, not a controller); cites CR 607 + CR 406 — verified. (Rule is sound; the FAIL is the Champions *fixture*, not this rule.)

## Glossary gaps

None. Discard, Sacrifice, Exile, Shuffle, Linked Abilities, and Unearth are all in `glossary.json`, and the glossary's own citations (Discard->701.9, Sacrifice->701.21, Shuffle->701.24) corroborate the rules.

## Process notes

- **Citation cross-reference (the agents' claim):** verified. All cited CR numbers across the 5 new rules resolve in `rules-structure.json` with matching text: 701.9 Discard (not 701.8 Destroy), 701.13a Exile, 108.3 owner, 406 exile zone, 404.1 graveyard, 701.21 Sacrifice, 118.5 cost-not-automatic, 702.30a Echo (verbatim templating), 603.6 zone-change triggers, 700.4 dies, 603.2 trigger resolution, 701.24 Shuffle (whose CR example is verbatim the Guile/Worldspine family text), 607.1 Linked Abilities, 201.5 self-by-name. The round-3 cleanup commit's three pre-existing-miscite fixes also check out: 701.7=Create / 701.13=Exile / 701.19=Regenerate / 701.20=Reveal / 701.24=Shuffle / 404=Graveyard — the corrected citations now land on the right rules.
- **Minor citation imprecision (NOT a FAIL):** `SacrificeSelfUnlessPayRule` cites CR 109.2 for "'this [noun]' is a self-reference"; 109.2 (object-description-means-battlefield-permanent) is adjacent rather than the strict self-by-name rule (201.5), but it exists and does not contradict the modeling, so per doctrine it is not a FAIL. Same for the 201.3 (interchangeable names) reference in the regex comment of `SelfPutIntoGraveyardFromAnywhereConditionRule`.
- **Initiative 03 (projection decision):** N/A for this batch. All 5 rules reuse pre-existing discriminators (`discardCards`, `exile`, `sacrifice`/`preventable`/`UnlessClause`, `shuffleIntoLibrary`, `returnToHand`) and the pre-existing `TriggerEvent.PutIntoGraveyard`; no new effect/cost type, trigger event, or restriction is introduced, so the exhaustiveness ratchet has nothing new to enforce and there is no projection sensibility to judge.
- **Dropped Shoal gold:** confirmed `ChampionsOfTheShoal.json` was removed (lossy compound-trigger parse) per the dispatch; not searched for. `WorldspineWurm` is the sole shuffle-family gold, which is acceptable — the rule is sound and the other family members have unrelated unparsed sibling lines.
- **WorldspineWurm second trigger:** the gold correctly uses `Dies` for the token trigger ("When this creature dies") and `PutIntoGraveyard` for the shuffle trigger ("put into a graveyard from anywhere") — the two distinct events on one card, faithful to CR 700.4 vs the broader from-anywhere event.

HALT: ChampionsOfThePerfect.json (tests/magic-ast-tests/Fixtures/HandParsedCards/ChampionsOfThePerfect.json)
