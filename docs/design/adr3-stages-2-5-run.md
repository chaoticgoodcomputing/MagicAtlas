# ADR-0003 Stages 2–5 — AFK run plan & acceptance test

Started 2026-07-16 (session e4b8540a). Goal: carry the taxonomy migration through Stages 2→5,
gate-green and committed at each step, ending in **frontend confirmation** that two known
over-sensitivity false edges are gone.

## The two motivating false edges (the acceptance test)

Both are the *same* defect: the frontend Card Explorer's neighbour columns match at **family**
granularity through the resource-edge graph (`useCardNeighbours` → `PORT_CANDIDATES_QUERY`,
family+side only) and never consult the port-level facets the labels already carry. The **ADR-2
engine already rejects both** — it is the oracle for the migration.

| # | Shown (wrong) | Why it's false | Engine verdict |
|---|---------------|----------------|----------------|
| 1 | Chatterfang, Squirrel General → Aang, the Last Airbender | Chatterfang `emit:token:creature:squirrel`; Aang `etb:creature:**self**`. Token-creation coarsely matches "a creature enters", but Aang's ETB is self-scoped — only Aang entering fires it. | `FlowFeasible` has **no `(token,etb)` arm** → never drawn. |
| 2 | Barrage Ogre → Ancient Copper Dragon | Barrage `emit:damage:**noncombat**:any`; Copper Dragon `trigger:damage:**combat**:player` on "**this creature** deals…". Fails on manner (noncombat ✗→ combat) *and* self-source. | `DamageSatisfiesTrigger` rejects on both `CombatFacetFeeds` and the self-source same-card guard. |

### Acceptance (verify at Stage 5, live)

- **NEG** Chatterfang does **not** list Aang as a neighbour.
- **NEG** Barrage Ogre does **not** list Ancient Copper Dragon as a neighbour.
- **POS** Deadeye Navigator still shows `blink → etb` neighbours (self-blink of *another* card's ETB).
- **POS** an open-ETB payoff (Soul Warden / Essence Warden family, `etb:creature` with no `:self`)
  is still fed by token emitters like Chatterfang — the family edge is real, only the self-scoped
  application was wrong.
- **POS** a genuine combat-damage feeder for Copper Dragon (if any exists in-corpus) is still shown.

## The stages

- **Stage 2** — structure the flow-participating families (dual-emit `PortStructure` alongside the
  legacy label; byte-for-byte round-trip gate). Remaining flow families: `damage` (emit+trigger),
  `cast` (emit+trigger+consume), `dice` (emit+trigger), `recur`/`returntobattlefield`/`returntohand`,
  `copy`, `additionalcombat`/`attacks`, `life` trigger-side. (9 done: Blink, Death, Etb, Life,
  ManaEmit, PayMana, Sac, Token, Untap.) Inert projections (evasion, counter, keyword, …) get a
  generic inert annotation — they never participate in flow.
- **Stage 3** — `captures(Q,E)` lattice matcher over the structures (subject subsumption + manner +
  self same-card guard + recipient), run in **shadow mode** parallel to `FlowFeasible`, diffed over
  the whole corpus. The engine is the oracle: the matcher must reproduce its accept/reject + tier,
  including the two rejections above. Residual registry holds the guards/polarity the lattice can't
  express.
- **Stage 4** — cutover: retire the subsumption-expressible arms, regenerate resource-graph +
  card-ports + combo instances **carrying structured attributes** (`subject.isSelf`, `manner`), add
  those fields to `CardPortRow` + GraphQL, reseed the API.
- **Stage 5** — frontend neighbour matching consumes the structured attributes (self same-card guard,
  manner compatibility, subject subsumption) instead of raw family+side. Run the acceptance test.

Tracking: tasks #22–#25.
