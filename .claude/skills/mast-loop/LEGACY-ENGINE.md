# Legacy-engine track — ADR-2 ports, edges, and product recall

The ADR-2 engine loop: make the **current** interaction engine reconstruct more, and more *trustworthy*, combos. Vocabulary is ADR-2 (`sac`/`ltb`/`etb` — mechanism labels) because this is the live engine that ships the product. This track keeps `bench:recall` moving while the [Accretion track](ACCRETION.md) builds the ADR-3 replacement in shadow.

> This content is the "projection-slice" and "precision-fix" currencies from the Parse track's [SKILL.md §"Three kinds of effort"](../mast-tdd-loop/SKILL.md) — promoted to a first-class track. That section is the authoritative mechanics; this doc is the track framing + entry surfaces. Read both.

Two currencies. Weigh them at Step 1 against the Parse track's coverage work — a false-GREEN or an AMBER-that-should-be-GREEN outranks raw coverage, because an untrustworthy GREEN poisons the product regardless of how many cards parse.

## Currency A — projection-slice (cheapest edge inventory)

A card that **already parses** can still project to a coarse `emit:<x>` / `emit:unparsed` label that no flow arm reads, forming **no interaction edge** — thousands of such cards exist, paid-for parse work sitting dark. Closing one means adding a **PortWalk flow arm** — usually **no parser or gold work at all** — and one arm lights up *every* card carrying that label.

- **Entry report:** `port-label-census.json` → `topProjectionGaps[]` — coarse emit labels ranked by the combo-popularity mass of the cards behind them (same value axis as `fusedScore`). Refresh: `nx run mast:run` then `dotnet run -- --flow PortLabelCensus`.
- **Gold artifact:** a new flow arm per [`libs/mast-interaction/docs/adding-a-flow-arm.md`](../../../libs/mast-interaction/docs/adding-a-flow-arm.md).
- **Gates:** `PortWalkExhaustivenessTests` (the new arm must project or be whitelisted), the Step-8 edge-diff (a new arm SHOULD change edges — the target labels are the expected footprint), `nx run bench:recall`.
- **Signal:** ports/edges ↑, `emit:unparsed` card count ↓.

## Currency B — precision-fix (multiplies the value of all coverage)

Lift a reconstructed edge **AMBER → GREEN** (usually a `Subsumes` type-proof the operator can't yet make), or kill a **false-GREEN** (an over-approximating port that fabricates an "infinite combo" — e.g. a death-payoff arm conflating self-death and other-death). Until GREENs are trustworthy the novel-combo product is worthless, so precision work retroactively raises the value of every parse and projection unit.

- **Entry report:** the `bench:recall` AMBER/missed combos (`tools/bench/MagicAtlas.Bench/bench-report.json` + `combo-expected-tiers.json`) and the `interaction-judge` findings under `docs/judgments/`.
- **Gold artifact:** an operator/port fix + a re-pinned `combo-expected-tiers.json` entry (a named pin edit, never a silent baseline rewrite).
- **Gates:** `nx run bench:recall` (per-combo expected-tier; a regression HALTs, an improvement re-pins) + the `interaction-judge` (the false-GREEN guard).
- **Signal:** RecallAtGreen ↑, judge-PASS rate ↑.

## Picking within the track

- **`combo-anchor-report.json` → `topAnchors[]`** is the shared demand surface (unparsed hub cards ranked by `popularityMass`) — also the Parse track's primary pick, because the two tracks share the same objective (reconstructed combos) from opposite ends: Parse gets the card *parsing*; Legacy-engine gets the parsed card *projecting + closing* at GREEN.
- The `bench:recall` **missed** combos are the aligned worklist: a missed combo is either a projection-slice gap (a piece projects dark) or a precision gap (it reconstructs but not at GREEN). Diagnose which, then pick the currency.

## The bridge back to Parse

A projection-slice that finds the card's clause **doesn't parse to the shape the arm needs**, or a precision-fix that needs a facet the AST lacks, spawns a [Parse track](../mast-tdd-loop/SKILL.md) sub-slice first (the umbrella's parse-inside-engine bridge), then completes the arm/fix. Do not add a flow arm over an `emit:unparsed` the parser should be structuring — fix the parse, then project.

## Migration note

Every artifact here is ADR-2 and re-derives in ADR-3 vocabulary only at Stage-4 cutover. Until then, precision-fixes and flow arms landed on the ADR-2 engine are the *product*; they are not lost at cutover (the reprojection carries their coverage forward), but net-new taxonomy work belongs in the [Accretion track](ACCRETION.md), not here. If a precision-fix keeps fighting the ADR-2 mechanism naming (e.g. the sacrifice/dies/LTB conflation the ADR calls out), that tension is a *reason to witness it in Accretion*, not to keep patching ADR-2 — surface it.
