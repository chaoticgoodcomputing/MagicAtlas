# Interaction golds — the schema-by-accretion source (ADR-0003 §8, Stage 0b)

Each file here is one **hand-authored interaction gold** — the ONLY hand-authored artifact in the
interaction layer. The `../rollup/` artifacts are GENERATED from these by the **`InteractionRollup`
Flowthru flow** (`Flows/InteractionRollup/`; `dotnet run -- --flow InteractionRollup`), never hand-edited.
Loop: read the rollup → derive a gold, declare only new rules → regenerate the rollup.

## The witnessing unit (`unit`)

- `single-card` — one card's port derivation. Witnesses that a stem / attribute / alias exists + is
  AST-derivable and CR-correct. No cycle.
- `pairwise` — one card's emit satisfying another's consume, no cycle closed. Witnesses a subsumption edge
  or a residual rule.
- `combo` — a closed loop. Additionally exercises §10 SDF balance (the repetition-vector GREEN/AMBER
  *cycle*-tiering). Carries `loop_tier`.

All three are rules-judge-gated and climb the same promotion ladder (`observed`→`corroborated`→`confirmed`).

## Fixture shape

```jsonc
{
  "id": "chatterfang-x-pitiless-plunderer",       // stable, name-derived; the rollup keys on it
  "unit": "combo",                                 // single-card | pairwise | combo
  "cards": ["Chatterfang, Squirrel General", ...], // card names; also the token names a card creates
  "source": { "csb": true, "popularity": 61668 }, // provenance of the interaction (optional)
  "loop_tier": "GREEN",                            // combo only; the SDF-balance verdict
  "judge": { "verdict": "PASS", "ref": "..." },    // if a rules-judge reviewed it → rules may be `confirmed`

  // Ports each card/token projects. `id` is gold-local. stem = side:supergroup:card-type (is-a spine);
  // attrs = the unordered attribute SET (O14). An attribute value may be an object to carry provenance:
  //   "to": { "value": "graveyard", "provenance": "derived" }   // over-approximated → caps Reliability
  //   "color": { "value": "any", "polarity": "producer-choice" }// existential match (E5)
  "ports": {
    "Chatterfang, Squirrel General": [
      { "id": "C1", "side": "intercept", "kind": "EVENT",    "stem": "deployment",            "attrs": { "token": true, "control": "you" } },
      { "id": "C5", "side": "emit",      "kind": "EVENT",    "stem": "removal:creature",      "attrs": { "from": "battlefield", "to": { "value": "graveyard", "provenance": "derived" }, "manner": "sacrificed", "control": "you", "qty": "X" } }
    ]
  },

  // Directed port-to-port edges that make the interaction. `mechanism` names how the match is certified;
  // anything other than `subsumption`/`card-defined`/`modifier` MUST reference a declared rule id.
  "edges": [
    { "id": "E1", "from": "Chatterfang.C5", "to": "Pitiless Plunderer.P1",
      "mechanism": "subsumption", "residuals": ["guard:exclude-self"], "tier": "GREEN",
      "notes": ["to=graveyard is derived → Reliability capped; Rest-in-Peace prune"] },
    { "id": "E5", "from": "Treasure.T4", "to": "Chatterfang.C3",
      "mechanism": "polarity", "rule": "polarity:color:emit-mana", "tier": "GREEN" }
  ],

  // NEW rules this gold introduces (with itself as witness). Rollup sections: polarity | match_policy |
  // guards (impl in code) | bridges. Each `id` is stable; the rollup unions by id and FAILS on conflict.
  "declares": {
    "polarity":     [ { "id": "polarity:color:emit-mana", "attr": "color", "context": "emit:mana", "value": "producer-choice", "cr": ["105"] } ],
    "match_policy": [ { "id": "policy:argument:subject-cover", "consume_kind": "argument", "subject": "cover" } ],
    "guards":       [ { "id": "guard:exclude-self", "impl": "code", "desc": "another-object / exclude=self identity guard", "cr": ["400.7","603"] } ],
    "bridges":      [ ]
  },

  // Machine-checkable acceptance tests — the gold IS its own test. Checked structurally at Stage 0b
  // (well-formed + internally consistent); engine-executed at Stage 3 shadow mode.
  "assertions": [
    { "claim": "loop_tier == GREEN" },
    { "claim": "edge.E5.tier == GREEN", "because": "producer-choice polarity" },
    { "claim": "edge.E1.reliability_capped", "because": "to=graveyard provenance=derived" }
  ],
  "cr": ["701.21a", "700.4", "603.10a", "614.5", "107.4"]
}
```

## Asserted-absence claims — `no_arm[P]` (ADR-0004 §1)

A gold may claim a **negative**: that a port connects to nothing, deliberately. The judgment becomes Evidence
with an *executable* justification instead of prose in a whitelist (which rots silently — that is the failure
ADR-0004 was written for). The claim is the sibling of `no_loop`, and is executed by
`Tests/InteractionRollup/TopologyRollupContractTests` Part B.

```jsonc
"ports": {
  "Rat Colony": [
    { "id": "P0", "side": "emit", "kind": "BEHAVIOR", "stem": "deck-construction",
      "attrs": {}, "structured": false, "note": "…" }   // structured:false = deliberately no PortStructure
  ]
},
"edges": [],
"assertions": [
  { "claim": "no_arm[P0]", "because": "for every consume probe in the current universe, SelectArm(P0, c) is null — …" }
]
```

- **`P` is named by the gold-local port `id`** (`"Card.Id"` also accepted, and required if two cards in the
  gold share an id). The port's declared `side`/`stem`/`attrs` **are** its identity — the ADR-0003 structure
  canonical form — so the claim reads *"structured exactly as declared, nothing connects."*
- **Asserted against the matcher, never against an edge set.** `PortFlowMatcher.SelectArm(P, consume)` must be
  null for every probe (an emit-side `P`); a consume-side `P` is probed as `SelectArm(emit, P)`. "This card
  produces zero edges" would be *vacuously* true for a single-card gold and would keep passing after somebody
  armed the port.
- **The probe universe is read at evaluation time** (`Tests/InteractionRollup/FlowProbes.cs`): every
  `witnessed` stem in the regenerated rollup, plus every distinct `PortStructure` the engine projects over the
  hand-parsed card corpus (which supplies the *facets* arms key on). Nothing is hardcoded, so the assertion
  **strengthens as stems accrete**.
- **A firing is a hard build failure, judge-resolved** — either the new arm is correct (amend/delete the gold,
  judge-gated) or the arm is wrong (fix it). Never weaken the assertion; never defer it to a report.
- **Non-vacuity is itself gated** by `NoArmNonVacuityTests`: the universe must be non-empty and carry facets,
  every declared `FlowArm` must be selectable by some probe pair, and an armed control port (`emit:mana`) must
  come back armed through the same evaluation path.

## What the flow validates (Stage 0b gate)

1. Every gold parses and is well-formed (required keys, unique port ids per card, unique edge ids).
2. Every edge `from`/`to` resolves to a declared port; every non-structural `mechanism` cites a rule that
   exists in the gold's `declares` or another gold.
3. Tier/ladder coherence: a GREEN edge/loop must rest on `confirmed` rules (judge-backed) — a rule with
   only `observed` support caps at AMBER.
4. Rollup union has **no conflicts** (same rule id, different content ⇒ FAIL the pass).
5. Assertions are well-formed (executed against the live engine only at Stage 3).

Bootstrap golds (§8): `chatterfang-x-pitiless-plunderer`, `deadeye-x-peregrine-drake`,
`ruthless-knave-x-blood-artist`.
