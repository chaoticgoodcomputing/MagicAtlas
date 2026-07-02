# Cycle-enumeration acceleration & raising the reconstruction reach

*Feasibility memo — MagicAtlas MAST engine (2026-06-17)*
*Re: "Can GPU-accelerating the cycle enumeration let MAST raise its length bound past 6 to find 4–5 card interaction cycles?"*

> Provenance: written to answer a design question during the flow-arm fan-out session, after the
> reconstruction reach was raised 5→6. Grounded in measurements of the actual engine + bench (a
> temporary profiler, since reverted). The reach is centralized as
> `PortGraphEngine.DefaultReconstructionReach`.

---

## 1. TL;DR verdict

**GPU is not the lever. It is premature by a wide margin, and the codebase's own measurements say so.** Raising the length bound past 6 is *already essentially free* — it is not gated by compute.

The premise has two embedded assumptions, and both are wrong:

- **"The engine uses Johnson's algorithm."** It does not. It is a rooted, canonically-ordered bounded DFS — no blocking set, no `unblock` cascade, no SCC decomposition. (See §2.)
- **"Cycle enumeration is the bottleneck, so accelerating it raises reach."** It is not the bottleneck, and raising the length bound finds nothing new today. The recall bench runs **per-combo over 2–3 card graphs** (avg 9 ports / 10 edges, max 15/21), where cycle counts **saturate by length 7–8** and total wall time is **flat (~3.5–4 ms) across all length bounds from 4 to unbounded**. There is no compute wall for a GPU to break through. (See §3.)

**The real lever is DATA COVERAGE, not compute.** MAST cannot find 4–5 card cycles because no 4–5 card graph is ever assembled: of 91,795 CSB combos, only **33 are eligible** (all their cards hand-parsed in the 946-card gold corpus), and all 33 are **2–3 cards** (max = 3). To find longer cycles you need a 4–5 card combo whose cards are *all* in the gold corpus — which does not exist today. Growing parse coverage, not GPU kernels, is what unlocks reach.

**Disposition:** GPU = *later, and probably never at this architecture.* Raise the bound for free if you want; invest engineering in corpus coverage and the 6 missing flow arms.

---

## 2. What the engine actually does today

The assumption that MAST runs Johnson (1975) is incorrect. The relevant code is `EnumerateInstanceCycles` (`PortGraphEngine.cs`), reached via `FindCycles`.

**It is a rooted, canonically-ordered, bounded DFS for elementary directed cycles.** Concretely:

- **No Johnson machinery.** There is no `blocked[]` set, no `B[]` auxiliary unblock-lists, no recursive `unblock()` cascade, and no strongly-connected-component decomposition.
- **Dedup via the "root = lowest-ordinal node" trick.** Each DFS rooted at `start` only steps to nodes whose Identity is ordinally greater than `start` (`string.CompareOrdinal(toId, startId) > 0`). This guarantees every elementary cycle is enumerated exactly once — rooted at its smallest member — *without* Johnson's blocking apparatus.
- **The "simple" constraint** is enforced by an `onPath` HashSet (no vertex revisited); the DFS recurses into a neighbour only when all three guards hold: `path.Count < maxLength - 1`, the ordinal-rooting check, and not-already-`onPath`.
- **§8 structural prunes and tiering** (`IsOneShotSelfRemoval` / `BridgeFedByIncompatibleToken` / `CounterGateUnsatisfiable`, then the `CoCostsSatisfied/Balanced/Productive/TapRenewed` tier flags) run when a cycle closes, computed against the *full* edge set.

**Cost honesty:** this variant is *cheaper to write* but *asymptotically worse than Johnson in the pathological case* — without the blocking set, its true worst case degrades toward exponential DFS (it can walk simple paths that never close). The decisive cost lever is **V** (node count), not the length bound per se.

**The two-layer engine (ADR-0002) already reframed the scale problem.** `FindCyclesByLabelGraph` moves the expensive enumeration off the ~100k-node per-instance ("molecule") graph and onto the **grammar-bounded distinct-label ("atom") graph — measured at ~545 cycle-relevant nodes (a 54× dedup over 29,615 cards)**. Layer 1 (`LabelCycleHops`) enumerates label cycles *unbounded*; Layer 2 re-instantiates and tiers only the admissible instance subset. The result is **provably byte-identical** to the reference `FindCycles` (CI equivalence gate over 62 sentinels + 33 bench combos). Crucially, **in the two-layer path the length bound is GONE from enumeration** — it is demoted to `displayMaxLengthInCards`, a post-enumeration filter on the count of distinct cards a cycle spans. It prunes the *result set*, not the *search*.

> Key files: `PortGraphEngine.cs`, `docs/adr/0002-two-layer-cycle-engine.md`, `libs/mast-interaction/docs/two-layer-cycle-engine.md`.

---

## 3. The true scale regime & bottleneck

**This section is load-bearing. The recall bench runs PER-COMBO over tiny graphs.**

`ComboRecallRunner.Evaluate` (`tools/bench/MagicAtlas.Bench/ComboRecallRunner.cs`) walks *exactly* the combo's distinct card names — it projects over `cardNames` only, then runs `FindCycles(Materialize(graphs), LengthBound)` over precisely those 2–3 graphs. **There is no corpus-wide subsidy.** The 946-card gold corpus is used *only* as an eligibility filter (`cardNames.All(_corpus.Contains)`), never materialized into the graph. So each of the 33 reconstructions is over a tiny 2–3 card graph, run independently.

**Measured graph size** (temporary profiler, since reverted):

| Metric | Value |
|---|---|
| Avg materialized graph | 9.0 ports / 10.1 edges |
| Max (3-card Chatterfang × Ruthless Knave) | 15 ports / 21 edges |
| Rule of thumb | ~3–5 ports/card + O(emits × consumes) derived edges |
| Projected 4–5 card set | ~18–25 ports, ~30–60 edges — *dozens*, not hundreds |

**Cycle count vs. LengthBound — saturating, not exponential** (summed across all 33 combos):

| Bound | 4 | 5 | 6 | 7 | 8 | 10 | 12 | unbounded |
|---|---|---|---|---|---|---|---|---|
| Total cycles | 43 | 44 | 53 | 55 | 55 | 55 | 55 | **55** |

Beyond bound 7 the search finds **nothing new** — the graphs are too small to contain longer elementary cycles. Wall time is **flat** (3.5 ms at bound 4 vs 4.0 ms unbounded across all 33 combos), confirming the LengthBound is **effectively non-binding at this scale**. The current longest *real* cycle is a 6-hop cast-blink loop (5-hop before the Displacer arm).

**Where time actually goes:** in per-edge **operator tiering inside `Materialize`**, not in cycle enumeration. Warmed 200-rep attribution: `Materialize` = 3.0 ms/pass vs `FindCycles` (enumeration + all §8 floors) @ bound 6 = 1.9 ms/pass. `Materialize`'s cost is the `O(emits × consumes)` double loop calling `FlowFeasible` → `ObjectFilterRelations.Intersects`/`Subsumes`, whose `TypeAxis`/`TypeSubsumes` do a cartesian `EnumerateAssignments` over the ontology. §8 cycle tiering is cheap because there are only ~1–2 cycles per combo to tier. (The "1 s for 73 tests" reported by `dotnet test` is entirely build + test-host startup, not reconstruction.)

**The honest bottleneck is data coverage:**

- 91,795 total CSB combos → **33 eligible** (every card hand-parsed in the 946-card gold corpus).
- All 33 are 2–3 cards (15 two-card, 18 three-card; **max = 3**).
- To find a 4–5 card cycle you need both (a) a 4–5 card combo in the snapshot and (b) *all* its cards in the gold corpus. **Neither exists today.**
- Even if you fed a 4–5 card set in, the graph stays small (~18–25 ports), cycle count stays small, and the engine finishes in well under a millisecond. The bound would need raising only marginally (cycles saturate ~7–8).

The limiter is **eligible-combo / parse coverage** — not graph size, cycle enumeration, or tiering throughput.

---

## 4. GPU / parallel feasibility — honestly

### Why Johnson resists the GPU (even if we did use it)

Even setting aside that MAST doesn't run Johnson, Johnson is **fundamentally hostile to GPU SIMT** for three coupled reasons:

1. **Strict DFS recursion.** Cycle enumeration rooted at a vertex is a single depth-first walk — a deep, data-dependent recursion stack of unpredictable depth. SIMT wants thousands of lockstep threads doing the same instruction on different data; a DFS offers no wide, uniform parallel front to map onto a warp.
2. **The blocking set is shared mutable state with a sequential dependency.** Johnson's entire speedup comes from `blocked[]` + the `B[]` unblock-lists, read and mutated across the whole recursion. Two DFS branches sharing vertices cannot proceed independently without racing on or contradicting each other's pruning. *The pruning that makes Johnson efficient is exactly what serializes it.*
3. **Irregular, data-dependent control flow.** Different roots recurse to wildly different depths → massive warp divergence and load imbalance, plus large/variable per-thread path+blocking state that wrecks register/shared-memory budgets and coalescing.

The honest literature position: the state of the art (Blanusa/Brisk/Hoefler et al., *Fast Parallel Algorithms for Enumeration of Simple, Temporal, and Hop-constrained Cycles*, ACM TPC 2023 / arXiv 2301.01068) **relaxes** Johnson's strict DFS to recover parallelism — and even that targets a **256-core / 1024-thread CPU cluster, not a GPU**, precisely because the irregular recursion and large per-task state fit cores-with-stacks far better than SIMT lanes.

### GPU-amenable alternatives (only relevant IF scale ever demanded it)

| Approach | Fit | Tradeoff |
|---|---|---|
| **Adjacency-matrix powers (A^k, SpGEMM)** | Poor as a solution; OK as a pre-filter. The most GPU-mature primitive there is — but a 545×545 matrix is trivial on a *CPU*. No scale problem to throw a GPU at. | Counts **walks, not simple cycles**; recovering simple cycles is #P-hard (2^k sieves). Can only gate "does a closed walk ≤k exist". |
| **Color-coding (Alon–Yuster–Zwick)** | Good conceptual fit — parametrized by **k**, exactly the length-bound lever; per-coloring DP and the O(e^k) colorings are embarrassingly parallel. | Randomized/Monte-Carlo — *detects/counts*, doesn't cleanly enumerate; deterministic listing needs costly derandomization. 2^O(k): fine k=5–6, brutal as k grows. MAST needs the full cycle set to instantiate. |
| **BFS-frontier bounded k-walk (V-FEC / T-FEC)** | The genuinely GPU-amenable form of the *actual* problem; BFS frontiers are wide+uniform, k caps frontier depth. First GPU elementary-circuit enumerator, ~190× over single-threaded Johnson. | Memory-bound; frontier explosion O(paths × k) detonates on dense graphs/large k; warp divergence remains. The 190× is over **sequential** Johnson, not a tuned multicore baseline. |
| **SCC decomposition + per-SCC/per-source task parallelism** | **Excellent fit, almost free.** Layer-1's ~545 nodes have small SCCs; Layer-2 instantiate-and-tier is already embarrassingly parallel (one Task per candidate). | This is **thread/task parallelism, not SIMT** — it wants CPU cores, not a GPU. Per-SCC load is uneven, but for MAST's scale the coarse per-candidate split is more than enough. |

### .NET GPU options, ranked

1. **ILGPU** — the most credible (pure-C# JIT, CUDA/OpenCL/CPU backends, actively maintained). *But* it is the SIMT kernel model: no recursion, no dynamic heap in kernels, bounded buffers, punishes divergence. You'd have to rewrite the algorithm into iterative bounded-frontier BFS (V-FEC/T-FEC) *before* it helps.
2. **ComputeSharp** — C#→HLSL via DX12. Windows/DirectX-centric — a poor match for the Linux-first dev environment and a cross-platform library; HLSL compute is *more* rigidly SIMT, so recursion/irregularity is worse.
3. **Raw CUDA interop** — max control + cuSPARSE/cuGraph, but abandons .NET purity and inherits native build/deploy complexity. Only justified if committing to a published GPU algorithm.

**Maturity verdict:** ILGPU is production-real for *regular* data-parallel kernels; the .NET GPU ecosystem is far thinner than CUDA-C/Python. **None of them make recursive Johnson fast — they only help an already-reformulated regular kernel.** GPU is correctly filed as a "future escalation lever, noted not committed."

---

## 5. The soundness tradeoff of raising reach

Raising the length bound is **soundness-safe**: longer cycles **floor toward Amber**, not toward false-GREEN.

The §8 floors (`CoCostsSatisfied`/`Balanced`/`Productive`/`TapRenewed`) are computed against the full edge set when a cycle closes; a longer reconstruction has more co-costs and gates to satisfy, so the *more* hops a cycle spans, the more likely it lands in Amber rather than clearing all floors to Green. Concretely: **raising reach raises the Amber count, it does not manufacture false GREENs.** So there's no soundness reason *not* to raise the bound — the only reason not to is that it finds nothing at today's graph scale (§3). (Current bench standing: 6 Green / 21 Amber / 6 Missed.)

---

## 6. Cheaper levers, ranked

In descending order of value relative to "GPU-accelerate the enumeration":

1. **Nothing (for compute).** The two-layer reframe (ADR-0002) already solved the scale problem. The length bound is a display filter, not a compute limit — **raising k costs almost nothing.** If reach is the goal and the data existed, just raise it.
2. **Grow the gold corpus toward 4–5-card combos.** This is *the* unlock for finding longer cycles. No 4–5 card graph is assembled today because no eligible combo has >3 cards with full parse coverage. This is the binding constraint.
3. **Close the missing flow arms** (currently 6 Missed). Direct recall improvement, independent of reach.
4. **Parse-precision Amber→Green.** Move existing Amber reconstructions to Green by sharpening parses — converts the 21 Amber, addresses real recall quality.
5. **Parallelize CPU + ship two-layer dedup** *(if compute ever bites).* Per the ADR escalation ladder: Johnson + SCC decomposition on Layer 1, then coarse-grained `Task`/thread-pool parallelism — first across SCCs, then the embarrassingly-parallel Layer-2 instantiate-and-tier (one Task per candidate). Plain .NET, fits the codebase; the EPFL TPC-2023 result confirms multicore CPU is where even the state of the art lives.
6. **SCC-scoped bound raises only** — raise the length bound only within SCCs that can actually contain longer cycles, avoiding wasted frontier on graphs that saturate early.

---

## 7. Recommendation & next steps

**Do not pursue GPU acceleration.** It would be solving a problem the project measured itself out of — premature optimization against a graph that is *hundreds of nodes, not millions*, where wall time is flat across all length bounds and the longest real cycle is 6 hops. Reaching for ILGPU now is wrong-tool, wrong-scale.

**The verdict on the original question is: not now — reach is not the bottleneck (bound 6, max reconstruction 3 cards).** 4–5 card combos are ~48% of the 91,795-combo universe, so reach *is* a worthy long-term goal — but the gate is data coverage, not enumeration speed.

**Concrete next steps, in order:**

1. **If you want longer reach today, just raise it** — bump `PortGraphEngine.DefaultReconstructionReach`, or rely on the two-layer path where it's already unbounded with `displayMaxLengthInCards` as the only cap. Cost ≈ zero. (Expect no new cycles until the data changes.)
2. **Invest in corpus coverage:** prioritize hand-parsing cards that appear in 4–5 card CSB combos, so an eligible 4–5 card combo actually exists for the runner to assemble. *This is where the reach gains live.*
3. **Close the missing flow arms and push Amber→Green** via parse precision — the highest-leverage recall work that does not depend on reach at all.
4. **File GPU under the ADR escalation ladder, unchanged.** The trigger condition to revisit: *the label graph and its SCCs grow past CPU tractability* — and even then, the path is a reformulated BFS-frontier kernel (V-FEC/T-FEC) in ILGPU, **never a ported Johnson, and CPU task-parallelism comes first.** The chemistry argument (finite atom vocabulary, combinatorially many molecules) says that condition is structurally distant.
