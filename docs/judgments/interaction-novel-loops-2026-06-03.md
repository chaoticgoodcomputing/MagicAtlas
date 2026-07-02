# interaction-judge — edge verdict (NOVEL loops)

**Scope:** 5 reconstructed sac→death→token-doubler cycles (5 sac→death hops judged as the load-bearing edge; the two token hops per loop are the already-judged Chatterfang doubler edges)
**Result:** FAIL (2 false-GREEN loops)

## Summary
- PASS: 2   FAIL: 2   CONCERN: 1

These are **novel** loops (engine-reconstructed over the union graph, not catalogued by Commander Spellbook). The qualitative call is exactly what they need: four of the five close GREEN under the engine's current single-port death model, but the death-payoff port does not distinguish **self-death** ("when THIS creature dies") from **other-death** ("whenever another / a creature you control dies"). Two of the four "confirmed" loops are real (other-death payoffs); two are **false-positive infinite combos** (self-death payoffs that never fire on the fodder's death).

---

## The structural defect (shared root of both FAILs)

The CR test for whether a death-payoff fires on the sac-outlet's fodder is **CR 603.2**: "Whenever a game event … matches a triggered ability's trigger event, that ability automatically triggers." A self-death trigger is a *leaves-the-battlefield* ability scoped to the source object — **CR 603.6**: such abilities are "written … 'When **[this object]** leaves the battlefield, …'" as distinct from "'Whenever a [type] is put into a graveyard from the battlefield, …'". The trigger condition of "When **this creature** dies" matches **only the source permanent's own** put-into-graveyard event (CR 700.4: "dies means 'is put into a graveyard from the battlefield'"). When Ashnod's Altar sacrifices a *different* creature (CR 701.21: "its controller moves it from the battlefield directly to its owner's graveyard"), the *fodder* dies — not the payoff permanent — so a self-death trigger **does not match** (603.2) and **does not fire**. No token is created; the cycle never closes.

The engine cannot see this because of a **two-layer model collapse**:

1. **Parser layer.** "When this creature dies" is parsed into `Trigger:{Event:"Dies", Filter:{CardTypes:["creature"]}}` — *identical* in shape to Pitiless Plunderer's "a creature you control dies" minus the `Controller:You` axis. Confirmed against landed golds: `Fixtures/HandParsedCards/MRD/SolemnSimulacrum.json` (lines 65–70) and `Fixtures/HandParsedCards/M21/DeathbloomThallid.json` (lines 32–40) are both printed "When **this creature** dies" cards, yet both render the trigger filter as a bare `{CardTypes:["creature"]}`. The self-referent — "this creature," the source object — is dropped. There is no `ExcludeSelf:false`/self-binding marker and no `ObjectReference{Kind:Self}` on the dying-object referent.

2. **Projector layer.** `PortProjector.DeathTrigger` (`libs/mast-interaction/PortProjector.cs:26–42`) captures only `Trigger.Filter` into `deathFilter` and emits one generic `death-payoff` consume port `Resource(ResourceKind.Death, deathFilter)`. With the self-referent already gone at parse time, a self-death payoff projects as "consumes ANY creature death," which `creature ⊆ creature` (You ⊆ any-controller) makes a clean **GREEN** against Ashnod's `{creature}` death emission — exactly the (correct) Blood-Artist GREEN shape, but here it is a lie.

So the GREEN claim on the sac→death hop of loops 1 and 2 asserts "every creature Ashnod sacrifices satisfies the payoff's death trigger." The CR does **not** guarantee that — it guarantees the *opposite* for a self-death trigger (it fires on exactly one death, the payoff's own, which the sac-outlet did not cause). False GREEN → FAIL.

---

## FAIL verdicts

### novel-loop-1 :: Ashnod's Altar:sac-outlet → Triplicate Titan:death-payoff  --(Death/Flow)--  [Tier: GREEN]
**Verdict:** FAIL
**Operator said (as reconstructed):** Overlap=Overlaps, Reliability=Yes, Reason=null → GREEN
**Producer emits:** `{CardTypes:["creature"], Controller:You}` — Ashnod's "Sacrifice a creature" (sac fodder dies; Controller:You stamped per CR 701.21 at `PortProjector.cs:128`).
**Consumer wants (as projected):** `{CardTypes:["creature"]}` — but the printed trigger is "**When this creature dies**" (SELF death only).
**CR citation:**
- CR 603.2: "Whenever a game event … matches a triggered ability's trigger event, that ability automatically triggers." (The fodder's death does not match a self-scoped trigger.)
- CR 603.6: leaves-the-battlefield abilities written "'When **[this object]** leaves the battlefield, …'" — scoped to the source object, as opposed to "'Whenever a [type] is put into a graveyard …'".
- CR 700.4: "dies means 'is put into a graveyard from the battlefield.'"
**Why the tier misrepresents the rules:** Triplicate Titan's tokens are created only when *Triplicate Titan itself* dies. Sacrificing a *different* creature to Ashnod's Altar does not trigger it (603.2). The GREEN asserts a guaranteed handoff the CR forbids. The loop does not close — it is a **false-positive infinite combo**.
**Routing:** false-GREEN soundness bug → the death-payoff port must encode **self-death vs other-death**; the sac→death edge from an arbitrary-creature sac-outlet to a self-only death-payoff must be **pruned (no edge)**, not GREEN. Upstream, the parser must stop collapsing "this creature" into `{CardTypes:["creature"]}`.

### novel-loop-2 :: Ashnod's Altar:sac-outlet → Phyrexian Triniform:death-payoff  --(Death/Flow)--  [Tier: GREEN]
**Verdict:** FAIL
**Operator said (as reconstructed):** Overlap=Overlaps, Reliability=Yes, Reason=null → GREEN
**Producer emits:** `{CardTypes:["creature"], Controller:You}` — Ashnod's sac fodder.
**Consumer wants (as projected):** `{CardTypes:["creature"]}` — printed trigger "**When this creature dies**, create three 3/3 Phyrexian Golem tokens" (SELF death only; Encore is irrelevant to the death port).
**CR citation:** Identical to loop-1 — CR 603.2 (trigger must match), CR 603.6 ("When [this object] leaves the battlefield"), CR 700.4 (dies).
**Why the tier misrepresents the rules:** Same defect. Phyrexian Triniform's three Golems are created only on *its own* death; another sacrificed creature does not trigger it (603.2). False GREEN → the loop does not close.
**Routing:** false-GREEN soundness bug → same fix as loop-1 (self-death/other-death port axis; prune the arbitrary-sac → self-death edge).

---

## CONCERN verdicts (sound but fixable)

### novel-loop-5 :: Lithatog:sac-outlet → Pitiless Plunderer:death-payoff  --(Death/Flow)--  [Tier: AMBER, Reason=Types]
**Verdict:** CONCERN (sound — does not block — but the AMBER is the *right answer for the wrong reason*, and the loop is in fact dead).
**Why sound:** Lithatog's two outlets are "Sacrifice an **artifact**" and "Sacrifice a **land**" — its fodder filters are `{CardTypes:["artifact"]}` and `{CardTypes:["land"]}`. Pitiless wants `{CardTypes:["creature"], Controller:You}` (an *other*-creature death). The AMBER is operator-sound by the same logic as the Blood-Artist × Treasure hop (judged 2026-06-02, Edge E): `artifact` is **not** Disjoint from `creature` (artifact creatures exist, CR 205.2b), and `land` is **not** Disjoint from `creature` either (CR 301.5 / 305.7 land-creatures, e.g. Dryad Arbor, animated lands), so the pair must **not** prune to Disjoint; but a sacrificed artifact/land is not *provably* a creature, so `Subsumes = No`/`Unknown` → Reliability floored, Tier AMBER, Reason=Types. The operator is holding the zero-false-positive bar correctly.
**…but fixable / why the loop is actually dead:** The *concrete* loop never fires — Lithatog sacrifices artifacts and lands, and in the real game those are typically non-creature, so **no creature ever dies**, so Pitiless ("whenever **another creature** you control dies") never triggers. The engine keeps the edge (correctly, since the admissible artifact-creature / land-creature case is not forbidden) and labels it AMBER (correctly), but **the AMBER is doing double duty**: it's masking the fact that this loop is qualitatively a non-combo, not merely an "unproven" one. This is the *expected* behavior of the type-straddle floor and **not a FAIL** — AMBER is the honest representation of "can coincide in principle via an admissible permanent that is both, not reliably." No merge-blocker.
**Routing:** **operator/projector — the fodder card-type axis is doing its job here, so the concern is informational, not a code fix.** The actionable item is the *same* self/other-death axis as the FAILs: once death-payoff ports carry it, Pitiless (other-death) would still take this AMBER edge, which is correct. The fodder-type carry on the sac-outlet port (CR 701.21 fodder is `{artifact}`/`{land}`) is already faithful. No parser/operator bug — flag for human triage that AMBER here means "dead in practice," not "promising-but-unconfirmed."

---

## PASS verdicts

- `novel-loop-3 :: Ashnod's Altar:sac-outlet → Pitiless Plunderer:death-payoff` (Death/Flow) — **PASS, Tier GREEN.** Pitiless triggers on "whenever a creature you control dies" — an **other-death** payoff (the source is excluded; the printed card is in fact "another creature you control"). Ashnod sacrifices a creature you control (CR 701.21 — you sacrifice your own; CR 700.4 — sacrifice = dies), so *every* fodder death is a "creature you control dies" event that matches the trigger (CR 603.2). `creature ⊆ creature`, `You ⊆ You`. The CR **guarantees** the handoff, not merely permits it. Real loop. (The fixture `Fixtures/Interactions/cards/PitilessPlunderer.json` line 6 drops the printed "another" — a parser-fidelity nit that is mast-judge's concern; it does not affect this tier, since either an other-creature death or any-controlled-creature death fires on the *fodder*, never on Pitiless itself.)

- `novel-loop-4 :: Ashnod's Altar:sac-outlet → Elenda, the Dusk Rose:death-payoff` (Death/Flow) — **PASS, Tier GREEN — on the relevant trigger.** Elenda has TWO death triggers: "**Whenever another creature dies**, put a +1/+1 counter on Elenda" (other-death — the relevant one, fires on Ashnod's fodder per CR 603.2) and "**When Elenda dies**, create X 1/1 Vampires" (self-death — the token emitter). The loop's token output comes from the *self*-death trigger (Elenda must herself die to make Vampires), so the loop is a different shape than 1–3: it is the +1/+1-counter other-death trigger that fires repeatedly on fodder, and Elenda's own death (sacrificed to Ashnod) that emits the X Vampires. The sac→death GREEN is **CR-correct for the "another creature dies" trigger** (`creature ⊆ creature`, CR 603.2 matches every other-creature death). PASS. **Caveat for the projector (see process notes):** if the engine projected the *self*-death "When Elenda dies" trigger as the death-payoff consumer, that hop would be a FALSE GREEN exactly like loops 1–2; it is sound only because the matching trigger is the other-death one. The single-port collapse means the engine cannot currently tell *which* of Elenda's two death triggers it matched — so this PASS is conditional on the engine having bound the other-death trigger, which it cannot presently distinguish.

---

## Process notes

- **The two token hops per loop are not re-judged here.** `death-payoff → Chatterfang:token-doubler` (Token/Modifier, GREEN: emitted token pins `IsToken:true` ⊆ doubler's `{IsToken:true}`, CR 111.1) and `Chatterfang:token-doubler → sac-outlet` (Token/Flow, GREEN since the C2 projector stamps `Controller:You` per CR 111.2) were ruled PASS on 2026-06-02 (`interaction-verdict-2026-06-02.md`, Edges B and C). They are unchanged. **One caveat:** loop 5's refuel hop is `Chatterfang token-doubler → Lithatog sac-outlet`, but Lithatog's outlets consume **artifacts/lands**, not Squirrels/creatures. The doubler emits `{creature, Squirrel, IsToken, Controller:You}`; Lithatog's intercept/consume is `{artifact}`∪`{land}`. `Squirrel-creature-token ⊆ artifact` and `⊆ land` are both **No** (a green Squirrel creature token is neither an artifact nor a land), and `creature ∩ artifact`/`creature ∩ land` are Overlaps-not-Subsumes → that refuel hop is itself AMBER/likely-No, reinforcing that loop 5 is dead. I did not have the loop-5 refuel tier in the dispatch (only the death hop was given as AMBER), so I flag it: **the refuel hop of loop 5 is also not GREEN** — Chatterfang does not refuel an artifact/land sac-outlet. This makes loop 5 doubly non-closing.

- **Why loops 1–2 FAIL but 3 PASSes under the same `creature ⊆ creature` GREEN.** The discriminator is *not* the dying object's card type (all four are creature deaths). It is **whose** death the payoff's trigger condition matches: self (1, 2) vs other (3, 4). The operator's `Subsumes` on the dying-object filter cannot see this because the distinguishing structure (the self-referent "this creature") was erased at parse time and never projected. This is precisely a case where "the printed text + the CR *can* decide it" (603.2 + 603.6 make "this creature" self-scoped, unambiguously) but the model lost it — i.e. the doctrine's **gap**, escalated to FAIL because the lost structure produces a **false GREEN** (a quantified false-positive across every self-death payoff × every creature sac-outlet in the corpus), not a merely-conservative AMBER.

- **Corpus blast radius.** Every "when this creature dies → create tokens" card (Triplicate Titan, Phyrexian Triniform, Solemn Simulacrum, Deathbloom Thallid, and the ~dozen self-death golds grepped under `Fixtures/HandParsedCards/`) currently projects an indistinguishable-from-other-death `death-payoff` port. Paired with any creature sac-outlet over the union graph, each yields a false-GREEN "confirmed" loop. This is the highest-value false-positive class in the novel-loop reconstruction.

- **No CORPUS GAP:** every axis is CR-covered (700.4 dies, 603.2 trigger-match, 603.6 self-scoped LTB triggers, 701.21 sacrifice-your-own, 205.2b/305.7 artifact-creature & land-creature admissibility, 111.1/111.2 tokens). The defect is in the model, not the rules dataset.

---

**Closing.** 5 novel loops: **PASS 2 (loops 3, 4 — other-death payoffs, GREEN CR-guaranteed via 603.2), FAIL 2 (loops 1, 2 — self-death payoffs project as GREEN but never fire on the fodder's death, 603.2/603.6/700.4), CONCERN 1 (loop 5 — AMBER is sound but the loop is dead: Lithatog sacrifices artifacts/lands, no creature dies; refuel hop also non-GREEN).** The single highest-value recognizer fix: **the `death-payoff` port must encode self-death vs other-death** — split the projector's generic `Dies` recognizer so a "when THIS creature dies" trigger projects as a self-only death port that does NOT consume an arbitrary sac-outlet's fodder, and pair it with a parser fix that stops collapsing "this creature" into a bare `{CardTypes:["creature"]}` filter (carry an `ObjectReference{Kind:Self}` / self-binding, or `ExcludeSelf` polarity). This single axis eliminates the entire false-GREEN class. Two FAILs → **HALT**.
