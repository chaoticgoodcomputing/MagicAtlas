# interaction-judge — edge verdict

**Scope:** 5 edges across 2 golds
**Result:** PASS (0 FAIL; 1 CONCERN routes, does not block)

## Summary
- PASS: 4   FAIL: 0   CONCERN: 1

## FAIL verdicts
_None._

## CONCERN verdicts (sound but fixable)

### families.json :: Chatterfang:token-doubler → Chatterfang:sac-outlet  --(Token/Flow)--  [Tier: AMBER]
**Operator said:** Overlap=Overlaps, Reliability=No, Reason=Controller
**Producer emits (doubler):** `{CardTypes:[creature], IsToken:true, Subtypes:[Squirrel]}` — **no Controller axis**
**Consumer wants (sac-outlet):** `{Subtypes:[Squirrel], Controller:You}`

**Why sound:** The emitted producer filter carries no `Controller` axis. From the filters alone the operator cannot prove the created tokens are `Controller:You` — an absent axis is unconstrained, so `⊆ Controller:You` is not provable and `Reliability=No` is the correct zero-false-positive floor. The operator is behaving exactly as the `Unknown`-floor contract demands; it did not under-derive.

**…but fixable because: the controller is pinned by the CR and is present in the oracle text, yet the producer port dropped it.**
- **CR 111.2:** "The player who creates a token is its owner. The token enters the battlefield under that player's control." A token's controller is fixed by the creation event — the creator.
- **CR 110.2 (lead):** "A permanent's controller is, by default, the player under whose control it entered the battlefield."
- The Chatterfang replacement clause is explicitly *"If one or more tokens would be created **under your control**, those tokens plus that many 1/1 green Squirrel creature tokens are created instead."* The creator is "you." So both the rule (111.2) and the printed text decide the axis: a token Chatterfang creates is `Controller:You`.
- The projector **should** stamp `Controller:You` onto a created-token producer port, mirroring the sac-outlet's emitted-Death filter, which inherits `Controller:You` from **CR 701.21 (Sacrifice):** "its controller moves it from the battlefield directly to its owner's graveyard. A player can't sacrifice … a permanent they don't control." Sacrifice-your-own pins the controller on the way *out*; token-creation (111.2) pins it on the way *in*. The two are the same projection move on opposite event polarities.

**Routing: PROJECTOR (port-emission), not parser, not operator.**
- Not the **parser**: controller-of-a-created-token is not a printed characteristic *of the token* (CR 111.3 — a token's text is the listed characteristics; control is a property of the creation event, 111.2). The AST faithfully records the token's intrinsic characteristics (creature/Squirrel/IsToken); nothing was dropped at parse time.
- Not the **operator**: with the axis absent it correctly floored to `No`; inventing `Controller:You` from nothing would be a false `Overlaps`/`Subsumes`.
- The fix belongs in the projector that lowers a `createToken` effect into a producer port's emitted subject filter: derive `Controller:You` from the creation event per CR 111.2 (the dual of the sac-outlet's 701.21 derivation). With that, the doubler emits `{…, Controller:You}`, `Controller:You ⊆ Controller:You` = Yes, and **Edge C lifts AMBER → GREEN**, closing the token-doubler → sac-outlet → death-payoff cycle reliably.

## PASS verdicts

- `families.json :: Chatterfang:sac-outlet → Pitiless:death-payoff` (Death/Flow) — **PASS, Tier AMBER.** Sound type-straddle: producer emits `{Subtypes:[Squirrel], Controller:You}`, consumer wants `{CardTypes:[creature], Controller:You}`. Subjects can coincide (Overlaps), but `Squirrel ⊄ creature` is `Unknown` because creature types are shared by Creature and Kindred (**CR 205.3**: "Creatures and kindreds share their lists of subtypes … Squirrel"; **CR 308.1**: "Each kindred card has another card type"), so a Squirrel need not be a creature. Reliability=Unknown / Reason=Types is the correct floor. (Edge A.)

- `families.json :: Pitiless:death-payoff → Chatterfang:token-doubler` (Token/Modifier) — **PASS, Tier GREEN (reliability guaranteed).** Producer emits `{CardTypes:[artifact], IsToken:true, Subtypes:[Treasure]}`; consumer's only constraint is `{IsToken:true}`. The emitted filter pins `IsToken:true`, and a Treasure token is a token (**CR 111.1/111.6**: a token is a marker representing a permanent; it is not a card but is a token). *Every* object the producer emits satisfies the consumer → subsumption = Yes. The CR **guarantees**, not merely permits. No false GREEN. (Edge B.)

- `blood-artist-engine.json :: Ruthless Knave:sac-outlet(creature) → Blood Artist:death-payoff` (Death/Flow) — **PASS, Tier GREEN (reliability guaranteed).** Producer emits `{CardTypes:[creature], Controller:You}` ("Sacrifice a creature"); consumer wants `{CardTypes:[creature]}` (no controller — "this creature or another creature dies", any controller). `creature ⊆ creature` = Yes (structured card type, not a straddling subtype, so no Kindred ambiguity), and the consumer imposes no controller restriction so `Controller:You` is a satisfying narrowing. The CR **guarantees** the handoff: a sacrificed creature is on the battlefield (**CR 701.21** — you can only sacrifice a permanent you control) and is moved to the graveyard, which is exactly "dies" (glossary / CR 700.4: "A creature or planeswalker 'dies' if it is put into a graveyard from the battlefield"). Every emitted death object satisfies the consumer. No false GREEN. (Edge D.)

- `blood-artist-engine.json :: Ruthless Knave:sac-outlet(Treasure) → Blood Artist:death-payoff` (Death/Flow) — **PASS, Tier AMBER.** Keeping the edge (Overlaps, not Disjoint-pruned) is **correct**: Treasure is an *artifact* type (**CR 205.3**: "The artifact types are … Stone, Treasure, and Vehicle"), and artifact is **not** disjoint from creature — artifact creatures exist (**CR 205.2b**; operator spec canonical fixture `{CardTypes:[artifact]}` vs `{CardTypes:[creature]}` → Overlaps). So an artifact-creature bearing the Treasure subtype is admissible-not-forbidden and the pair must **not** prune to Disjoint. Reliability=No is **sound**: a predefined Treasure token is a *colorless Treasure artifact token* with no creature type (**CR 111.10**), so "every Treasure is a creature" is provably false → subsumption = No, Reason=Types. Overlaps + Reliability=No = AMBER. (Edge E.)

## Process notes
- **Renumbering caveat for the dispatch prompt.** The prompt cites "CR 701.16 for sacrifice." In the vendored `rules-structure.json`, **701.16 is now *Investigate*** ("'Investigate' means 'Create a Clue token.'"); **Sacrifice is 701.21**. I judged against 701.21 (the live sacrifice rule). The dual the prompt was pointing at — sacrifice pins the sacrificer's control on exit, token-creation pins the creator's control on entry — holds; only the number moved.
- **Edge C real-world note.** In the concrete combo the doubler's Squirrels *are* in fact created under your control, so the live edge fires reliably; AMBER is purely an artifact of the dropped controller axis, which is why this is a recoverable CONCERN rather than sound-irreducible AMBER (contrast Edge A, where the straddle is genuinely open).
- **Edge E real-world note.** The concrete Treasures are 111.10 tokens and never creatures, so the live edge never fires — but the operator judges from filters, and AMBER ("can coincide in principle via an admissible artifact-creature Treasure, not reliably") is the sound representation. Not a Disjoint-prune (would drop the admissible artifact-creature case), not GREEN (Treasures are typically non-creature).
- No CORPUS GAP: every axis judged (`Subtypes` straddle, `CardTypes` permanent-partition / artifact∧creature, `Controller`, `IsToken`, the Dies/Sacrifice/token-creation event semantics) is covered by the CR dataset and the operator spec.

---

**Closing.** 5 edges, 2 golds: **PASS 4, FAIL 0, CONCERN 1.** No false GREEN — both GREENs (B: Treasure-token ⊆ IsToken; D: sacrificed-creature ⊆ creature-death via 701.21 + 700.4) are CR-*guaranteed*, not merely permitted. Both AMBERs are sound (A: Squirrel⊄creature straddle, 205.3/308.1; E: Treasure is an artifact type, artifact∧creature admissible so not pruned, but 111.10 Treasures aren't creatures so Reliability=No). The single most-impactful finding is the **CONCERN on Edge C**: the AMBER is a sound floor but recoverable — the projector should stamp `Controller:You` on a created-token producer port per **CR 111.2** (mirroring the sac-outlet's 701.21 controller derivation), lifting C to GREEN. Routing: **projector**, not parser or operator. Zero FAIL → **PROCEED**.
