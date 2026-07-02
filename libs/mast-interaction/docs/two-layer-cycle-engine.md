# Two-layer cycle engine (label-cycle → instantiate) — DESIGN / banked

**Status:** 📐 **DESIGN — banked (2026-06-16), not implemented.** Pursue as an initiative-07 item
(the label-cycle set is a versioned, incrementally-recomputed derived index — see
`docs/adr/0001-versioning-and-incremental-recompute.md`). This doc records the architecture, the
measurement that motivates it, and the operating assumption.

## The framing: an analytical-chemistry engine

Ports are the **atoms of gameplay** — a finite, bounded vocabulary of canonical `PortLabel` leaves
(ADR-0002 §1–3). Cards are **molecules**: many cards, built from the same atoms (1,000 cards all
project the one atom `sac:creature:controlled`). **Interactions** are how atoms bond — either by the
card's own text (**card-defined edges**, §5) or by the rules of the game (**rules-defined edges / flow
arms**, §6/§7). The periodic table is small and bounded; the molecules are combinatorial.

The consequence: cycle-finding — the expensive, super-linear step — should run over the **atoms** (the
distinct-label graph), not the **molecules** (the per-card port instances). `cards → ports →
interactions`, with the middle term deduplicated.

## The measurement that rules out the bad case

The `PortLabelCensus` Flowthru flow (`nx`/`dotnet run -- --flow PortLabelCensus` →
`_08_Reporting/port-label-census.json`; re-run as coverage grows). **Full parsed corpus, 2026-06-16:**

| metric | full corpus (29,615 cards) | gold sample (951 cards) |
|---|---|---|
| distinct port labels | 723 (**41× dedup**) | 268 (3.5×) |
| **cycle-relevant labels** (edge-forming roles) | **545 (54× dedup)** | 115 (8.3×) |
| inert/coarse labels | 178 | 153 |

Single labels absorb thousands of cards (`pay:mana` 4,825, `tap:self` 3,883, `etb:creature:self`, …).
The dedup ratio is **far higher at full scale** (54× vs the golds' 8.3×) — the gold sample was skewed
low because the TDD loop triaged the largest card families first; the full corpus saturates harder, as
predicted. The cycle graph is **~545 nodes**, bounded by the grammar — never corpus-scale. Bad case
(label space ≈ card count) decisively ruled out.

**Operating assumption (per the project owner, 2026-06-16), now confirmed at full scale:** the gold
sample's 8.3× was skewed *low* — the TDD loop triaged the **largest card families first**, the high
card:port regime. The owner's call that the ratio holds (unique-port-count stays below card-count — the
chemistry: finite atoms, combinatorial molecules) is borne out: the full 29,615-card corpus yields
**54× dedup, 545 cycle-relevant labels**. As the parser extends to rarer cards the *absolute* label
count creeps up (more `subject:scope` filters) but stays grammar-bounded and the *ratio* climbs with
scale. Also note **cycle-relevant N grows as flow arms land** — many of the 178 inert labels are 03
coarse fallbacks (incl. `emit:unparsed` on 3,672 cards — the parser coverage gap); each new arm
promotes some into the cycle-relevant set, toward the vocabulary ceiling, never toward corpus size. We
proceed assuming the two-layer approach keeps complexity sub-corpus — the measurement backs it.

## The architecture

**Layer 1 — label graph (candidate shapes; cheap).** Build adjacency over **distinct cycle-relevant
labels** (group by label, not per-instance `Identity`). An edge `A → B` is the `FlowFeasible`
*kind/role* relation (the label-level "could bond"). Enumerate elementary cycles here — small `N`,
small SCCs, so a **generous or no length bound** is tractable. Output: candidate interaction *shapes*.
This MUST be a sound **over-approximation** — every real interaction's labels connect (the coarse-label
/ prefix-preimage design, ADR-0002 §2, guarantees nothing real is dropped).

**Layer 2 — instantiate + tier (precise; per candidate).** For each candidate label-cycle, find the
cards providing each label, materialize their specific ports, and run the operator (`Subsumes`/
`Intersects` on the real `Subject` filters) + the §8 balance/firability to assign the tier (Green/
Amber/Red) and prune false candidates. This is where instance-dependent truth lives (the
Squirrel⊄creature straddle: same label, operator decides). Bounded work over a small card-set per
candidate — never a global enumeration.

The expensive (potentially exponential) enumeration thus runs on the tiny atom graph; the precise
tiering runs only on the handful of card-sets that produced a candidate.

## Complexity

- Elementary-cycle enumeration is exponential worst-case **in the node count**, but the node count is
  the *label* graph (hundreds), not instances (~100k) or cards (38k). With small SCCs the real cost is
  far below worst-case. If it ever bites: **Johnson + SCC decomposition** (enumerate only within each
  strongly-connected component) before touching the depth cap.
- `LengthBound` demotes to a **display/query filter** in *cards* (`N < K`), applied post-enumeration —
  exactly the memoize-then-filter the owner sketched. The enumeration itself need not be card-bounded.
- **GPU-parallel cycle analysis** is a future escalation lever (noted, not committed): the instance
  tiering across candidates is embarrassingly parallel, and large-SCC cycle enumeration has known
  GPU formulations. Reach for it only if the label graph + SCCs grow past CPU tractability — the
  measurement above suggests that's distant.

## Relationship to initiative 07

The label-cycle set is the canonical **derived index**: build once per corpus version, query many
(`all cycles a card is in` = card → its labels → label-cycles → instantiate). Incremental recompute
keys on `(label, version)`: a card whose projection changes dirties only the labels it touches plus
their cycle neighborhood — far tighter than the per-card neighborhood 07 currently bounds. This doc is
the engine half; 07 owns the versioning/invalidation/atomic-swap half.

## Copy recursion — the label-graph closure is the fixpoint (decided 2026-06-16)

A copy that copies a copy is the recursion hazard. Two forms surface it:

- **Interaction layer — spell-copy** (the deferred `alignment/spell-copy-arm` work). Narset's Reversal +
  Reiterate each copy the *other's* spell (bench 11-3368): the copy-of-spell graft reproduces a
  spell-copy that re-copies, unbounded if expanded eagerly. **Decision (project owner): handle via
  Option B — two-layer label-graph closure.** The copy-of-spell arm is expressed as a label-level
  `FlowFeasible` relation (`emit:copy:spell → resolve`), so the recursion becomes a **finite elementary
  cycle in the bounded ~545-node label graph** (Layer 1's DFS already guards on-path revisits and
  terminates). Layer 2 instantiates that one finite candidate over the real 2-card set and tiers it (the
  AMBER target — Reiterate's `{3}` buyback co-cost — falls out of §8 balance). Infinite recursion is
  *structurally impossible* because Layer 1 walks labels, never instances. No depth cap, no visited-set
  bookkeeping in the graft — the engine this doc describes *is* the termination argument. (A depth
  backstop stays available as defense-in-depth but is not the mechanism.)

- **Parse layer — token-copy-of-self** (already resolved). Cards that make a token copy of *themselves*
  are common — the **myriad** keyword is self-copy by definition (~35 cards), plus explicit self-name
  copies (Jace Mirror Mage, Living Laser, Shredder), "copy of *this* creature" (Homunculus Horde, Giant
  Adephage, Conclave Evangelist), and embalm/eternalize (Adorned Pouncer, Honored Hydra). The AST does
  **not** inline-expand the copied object: `TokenDefinition.IsCopy = true` is a bounded **reference** to
  the source (via the effect target / "it" / "this creature") plus the printed deltas ("except it's a
  4/4 black Zombie"), never a clone of the source's abilities. So "where do we stop the AST parse" =
  *at the copy boundary* — `IsCopy` is the fixpoint marker. Explicit token definitions (literal "a
  Spirit with flying") are still inlined because they are bounded literal text; only **copies** are
  references. (The self-reference source — "this card"/"it" — is presently captured as the
  `OtherCharacteristic("this card")` free-text residual, a de-string-debt item, not a recursion risk.)

Both layers share one principle: **a copy is a bounded reference, and recursion closes as a finite
self-loop — never an inline expansion.**

## Next steps (when pursued)

1. Refactor `FindCycles` adjacency to group by label (not `Identity`); emit candidate label-cycles.
2. Add the instantiate-and-tier pass (re-materialize a candidate's card-set, run the operators).
3. Equivalence test: the two-layer result == the current per-instance `FindCycles` result on the
   sentinel set + the bench (label-cycle → instantiate must be byte-identical in tiers).
4. Track the census numbers over time via the `PortLabelCensus` Flowthru flow
   (`_08_Reporting/port-label-census.json`) — the card:label ratio is the health metric for the
   assumption. (Diagnostics live in Flowthru, not the NUnit suite.)
