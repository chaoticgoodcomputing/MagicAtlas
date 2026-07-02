# CardAtlas — the "shape → buildable" data layer (D1–D4)

MAST-generated datasets that bridge the port-graph *structure* to actual, filterable **cards** — the
gap the explorer/exploiter persona reviews found. Scoped to the parse-ready CSB combo-card union
(cards of combos whose every card parses cleanly), offline (committed `combos.json` + `card-inputs.json`).
Diagnostics — never gates.

## The four datasets

| # | Dataset | Output | What it is |
|---|---|---|---|
| **D1** | CardPorts | `_08_Reporting/card-ports.json` + `card-meta.json` | The card↔port index (one row per card × distinct port label; family + emit/consume side) and per-card metadata (colour identity, derived mana value, type line). The keystone. |
| **D4** | ComboInstances | `_08_Reporting/combo-instances.json` | Per-combo reconstructed loops: **named cards** + family-signature + tier + firability + CSB result. `DiceComboReport` generalised beyond dice. Anchor on a family by filtering `familySignature`. |
| **D2** | ResourceGraph | `_08_Reporting/resource-graph.json` | The family "subway map" — stations (card mass) + directed lines the reconstructed combos traverse, each annotated with realizing-combo count, best tier, and the bidirectional-engine flag. |
| **D3** | ArchetypeCatalog | `_08_Reporting/archetype-catalog.json` | The **realized** combo-shape catalog: every family-signature ≥1 reconstructed combo realizes, with combo count, best tier, green-fraction, an example piece list, and the produced results. |

The full *structural* catalog of theoretically-possible shapes (all 3,286, now untruncated) lives in the
`PortGraphAtlas` report's `familyArchetypeCatalog`; D3 is the actionable *realized* subset.

Metadata note: colour identity + mana value + type line come from `card-inputs.json`; **price and EDHREC
are not in that source** (they arrive with a fuller Scryfall fetch when this migrates to `atlas-flows`).

## Generate

```bash
# from tests/magic-ast-tests (no Python venv needed — pure C#)
dotnet run -- --flow CardAtlas
```

Fast (~20–30s compute): D1 per-card projection, D4 per-combo reconstruction (each combo's 2–5-card
materialize is tiny — the 847s whole-union blowup is avoided), D2/D3 aggregate D1+D4 with no re-materialize.

## Test

**Automated gate** — runs the three steps end-to-end on a committed fixture (real oracle text for
Chatterfang × Pitiless Plunderer + a sac outlet + a death payoff) and asserts the cross-dataset invariants
the API/UI will rely on, plus golden facts. Stateless, deterministic, no gitignored corpus:

```bash
dotnet test --filter "FullyQualifiedName~CardAtlasContractTests"
```

The gate covers: D1 mana-value derivation + colour identity + sac-outlet port detection; D4 golden combo
reconstruction with named cards + valid/sorted signature; and the joins — every D4 card ∈ D1 index, every
D3 archetype realized + canonical, every D2 station/line canonical.

**Manual smoke** — after `--flow CardAtlas`, the persona queries confirm the walls are down (jq/python over
the outputs): sac outlets with colour/CMC (D1), named sac combos with tier + result (D4, filter
`familySignature` contains `sacrifice`), realized sac archetypes tier-annotated (D3), realized lines from
`sacrifice` (D2).
