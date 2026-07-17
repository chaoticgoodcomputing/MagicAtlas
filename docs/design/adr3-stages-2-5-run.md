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

### Acceptance — RESULTS (verified live at :55173, 2026-07-16)

- **NEG ✅** Chatterfang and Aang are now **fully disconnected** (token has no flow arm to a cast
  trigger). Chatterfang: 0 mentions on Aang's page.
- **NEG ✅** The false **`damage → damage`** edge between Barrage Ogre and Ancient Copper Dragon is
  **gone** (Barrage's `noncombat` damage can't feed Copper's `combat` self-trigger — pruned by both
  the manner guard and the self-source guard). Barrage still appears on Copper's page, but **only via
  the legitimate `token ↝ sacrifice` link** (Copper makes Treasure → Barrage sacrifices an artifact) —
  the correct interaction, not the false damage one. This is the right outcome: the *specific* false
  edge the user reported is removed while the real relationship is kept.
- **POS ✅** Deadeye Navigator still feeds **392** ETB-trigger cards via `blink → etb` (including
  cross-card self-ETBs like Aang's — valid, since a *different* card's blink makes it re-enter).

Note: the engine has **no `token → etb` arm** — creating a token is not modelled as feeding an ETB
trigger (only blink/reanimation re-entry is). So the frontend's ETB feeders are `blink`/`recur`
emitters, matching both the engine and the *prior* ring behaviour (the ring also lacked `token→etb`).
The over-sensitivity was entirely the spurious combo-ring hops (`token→cast`, `mana→cast`, …) and the
missing facet checks, both now removed.

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
