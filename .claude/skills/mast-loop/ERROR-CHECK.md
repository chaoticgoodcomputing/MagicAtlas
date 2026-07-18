# Error-check — the span-witness QA mechanics

The quality dimension of the loop: not "add a port" but "**a port is wrong — fix it**." A port's `SourceSpan` is a **witness** — the exact oracle-text characters the port claims. When that text contradicts the port's label (a `sac` port whose span says "Flying", a `trigger:damage` port whose span points at an equipment's enchant clause), the port is a **suspect**: either a **false-positive port** (the parser/projection over-generated a port that isn't in the text) or a **span mis-attribution** (a real port pointing at the wrong clause). Both poison downstream: a false-positive port fabricates edges (the "Chatterfang feeds Aang" class); a mis-attributed span mis-highlights and mis-routes. A wrong port outranks a missing one — the same "untrustworthy GREEN > coverage" logic the [Interaction track](INTERACTION.md) uses, one layer down at the port.

> This is the mechanics doc for the check the [Interaction track](INTERACTION.md) runs at the end of every round (Step 6). It's also directly invocable on its own for a full-corpus sweep. Either way, a suspect resolves into **either** a Parse sub-slice (fix the span mint) **or** an Interaction-track refinement (tighten the gold that witnesses the stem) — the entry report routes it; you finish in the owning track's discipline.

## Entry report — `span-witness-report.json`

Refresh: `nx run mast:run` (or `--flow CardAtlas`) to make `card-ports.json` current, then **`nx run mast:span-witness`**. Corpus-gated + gitignored; degrades to blank witness-routing if the cited topology is absent.

The check slices every parsed port's span and verifies it contains the anchor word its label asserts (`sac`→"sacrific", `emit:token`→"create", `trigger:damage`→"damage"; keyword mechanics aliased in — firebending→mana, modular→dies, embalm→create). Four buckets:

- **`checkedPorts`** — the denominator (ports with a span + a checkable anchor).
- **`derivedExcluded`** — a created token's affordance projected onto its creator (span borrowed from the creating clause, ADR-0003 §7). **Not a defect** — excluded by design.
- **`misalignedDfc`** — span offsets run *past* the stored oracle text (empty slice). A distinct systematic class: **double-faced cards** whose served `OracleText` is empty while spans index the composed CardFaces text. Its own fix (store/serve the composed text), not a per-port suspect.
- **`outliers[]`** — the **actionable suspects**: span text present, anchor absent. Ranked **unwitnessed-stem-first** (a suspect on a stem no gold covers is a QA flag *and* a taxonomy gap), then by stem, then card. Each carries `stem`, `expectedAnchor`, `claimedText`, and **`witnessGolds`** — the golds that witness its stem (`stems[stem].witnesses` from the cited topology).

## The pick → diagnose → route cycle

1. **Pick** a suspect (top of `outliers[]` — unwitnessed stems first). Read `claimedText` against `label`.
2. **Diagnose** which failure it is:
   - **Span mis-attribution** (the common case): the port IS real, but its span points at a container/preamble/grant clause instead of its own text (a Class level, a Siege mode, an equipment's granted trigger). → **Parse sub-slice**: the parser must mint the inner-clause span. See the convergence note below.
   - **False-positive port**: the `claimedText` genuinely doesn't support the port — the parser/projection invented it. → fix the parser rule or the PortWalk projection that mints it (Parse or Interaction-track discipline). This is the one that also kills fabricated edges.
3. **Route via the witness** — `witnessGolds` names the golds that vouch for this stem. Two moves:
   - If the port is a **false positive** and a gold witnesses its stem, the gold's witness is likely **too permissive** (it admits the bad port). Tighten the gold, re-run `InteractionRollup`.
   - If the stem is **uncovered** (`witnessGolds` empty — the event-verb stems `damage`/`dice` carry no per-stem witness today), the suspect is *also* a taxonomy gap: witnessing that stem (Interaction track, Currency C) both covers it and gives future checks a reference.
4. **Gate**: `nx run mast:test` (the span-provenance NUnit invariants — `Derived_token_affordance_ports_inherit_the_creating_clause`, the orphan/round-trip guards) must stay green, and **re-run `nx run mast:span-witness`** — the suspect must clear (and no new one appear). If the fix touched golds, `InteractionRollup` conflict + ladder green.

## Standing facts

- **The gate is NUnit; the report is the diagnostic.** The invariants in `PortWalkSentinelSnapshotTest` (span-provenance) fail the build on regression; the Flowthru report surfaces suspects for the loop. Never make the report a gate (diagnostics = Flowthru, gates = NUnit).
- **Extend the anchor vocabulary, never suppress a suspect.** A correct keyword-mechanic port flagged as an outlier means the vocabulary lacks that keyword→effect alias (`SpanWitnessStep.AnchorsFor`) — add the alias. But a suspect that is genuinely wrong text must never be silenced to lower the count.
- **The current cluster is one parser capability — per-inner-clause span provenance.** The container span slice (Class/Saga/Modal → each body inherits its sub-clause span; landed 2026-07-16) cleared the null-span orphans. The remaining semantic suspects are the same shape in other AST forms: **granted abilities** (equipment/aura `has "…"` — needs the inner quoted-text span), **keyword-cost** (`tap` span → the keyword label, not `{T}`), and **faction-choice modals** (Sieges: "As this enters, choose X/Y" — an ETB-triggered mode-selection, *not* `ModalAbility`). Each is a Parse sub-slice; the report's `claimedText` + `stem` scope it exactly. See `docs/design/span-witness-triage.md`.
- **`derived` and `misalignedDfc` are not suspects.** Don't burn loop cycles on them from this report — they're their own workstreams (a `derived` provenance flag; the DFC composed-text fix).
