# mast-tdd-loop — final-tweaks review (2026-06-17)

*Independent 4-dimension review against the 7-batch + flow-arm-trial failure evidence. Verdict: one NECESSARY tweak (the corpus-edge-diff gate), the rest high-value/optional.*

> **STATUS 2026-06-17 — tweaks 1, 2, 4, 5, 6 LANDED.** The NECESSARY gate (#1) + its NX-target prereq (#2)
> are built + verified (no-change PASS / synthetic-overfit HALT / dispatched-target PASS) + wired into
> SKILL Step 0 (baseline snapshot) and Step 8 (the gate): `tools/gate-corpus-edge-diff.sh` +
> `tools/corpus-edge-signatures.py` + `nx run mast:interaction-triage` (slices to ClassifyCombos +
> MaterializeCardEdges, excluding the intractable cycle tail) + the named carve-out
> `tests/magic-ast-tests/Fixtures/edge-diff-expected.json`. Defense-in-depth #4/#5/#6 landed earlier
> (worker-contract anchor+sibling-sweep+byte-for-byte, SKILL 2-strike defer, seed non-ASCII WARN). Only #3
> (merge-rule split) + #7 (optional `.gitattributes merge=union`) remain — pure ergonomics; the suite
> already fails a bad generated-artifact merge red, so neither is a safety gap.*

All claims verified. The bench recall gate is per-combo (independent of the union cycle enumeration), and card-edges.json is emitted at MaterializeCardEdges, which runs before the intractable MaterializeCycles tail — so Dimension 1's proposed gate is generable at the existing timeout-bounded stage.

I have everything needed to synthesize. Here is the recommendation.

---

# mast-tdd-loop — Final-Tweaks Recommendation

## Verdict

**Not production-ready as-is on safety — there is exactly ONE necessary tweak, and the other three reviews' "necessary" labels do not survive scrutiny against the high bar.** The loop has one real correctness gap: the OVERFIT failure class (silent sibling-mislabel) has **zero deterministic orchestrator gate** — it was caught ~4x purely by judge diligence, which is a non-deterministic safety floor. Everything else is documentation/ergonomics that makes already-safe behavior legible, or is redundant with gates that already exist. Close the one gap; ship the rest as high-value follow-ups.

I verified the load-bearing facts before ranking: no root `.gitattributes` exists; `card-edges.json` is 320 MB and is emitted at `MaterializeCardEdges` (which runs *before* the intractable `MaterializeCycles` tail — confirmed in `InteractionTriageFlow.cs`); `Committed_schema_export_is_current` is a plain `[Test]` (not `[Explicit]`) inside the gated suite; the harness `MERGE_AGENT` step 3 applies a generic "keep-BOTH additive" rule with no derived-artifact carve-out; the worker contract's shared-file list omits `*RuleHelpers.cs`; and there is no `interaction-triage` nx target (re-triage runs raw).

---

## Why three of the four "necessary" labels are downgraded

The reviews each nominated a "necessary" tweak within their own silo. Skeptically deduped against the high bar (real safety/correctness gap, not polish):

- **Dim 2's "discard-both+regen" merge rule** — labeled necessary, but Dim 2's *own* tweak 3 proves it is self-checking: a mis-merged `ast-schema.json` / `known-coarse-projections.json` *already fails* the between-merge SUITE (both staleness assertions are non-`[Explicit]` `[Test]`s in the gated csproj — verified). The hand-merge can produce a bad artifact, but it **cannot land silently** — it goes red at the next gate. That makes this a *documentation* fix (stop misdirecting the operator), not a correctness hole. **Downgrade to high-value.**
- **Dim 3's "document why the slow tail is safe to skip"** — the tail (`MaterializeCycles`/`PortNodes`/`PlotInteractionGraph`) gates nothing; the product gate is the independent per-combo `bench:recall` (verified per-combo, max-3-card graphs). The invariant is *already true and already safe*; writing it down prevents future operator error but fixes no current defect. **Downgrade to high-value.**
- **Dim 4's three "high-value" doc tweaks** stay high-value (correctly self-rated).

Only Dim 1's edge-diff gate is a *missing detection capability* for a *recurring, demonstrated* silent failure. That is the bar.

---

## Ranked tweaks

### 1. NECESSARY — Add a deterministic corpus-edge-diff gate over `card-edges.json`

**The single material safety hole.** Overfit FAILs were the #1 recurring class (Mindcrank, Rings of Brighthearth, Hapatra) and in every case the worker suite was green — only manual judge re-parsing of siblings caught them. A `parse-records.json` diff is **blind** to this class (the mislabel changes an ability's *semantic content*, e.g. LifeLost↔Mill, not its parse counts/residuals). The signal that moves is the downstream port projection in `tests/magic-ast-tests/Data/_08_Reporting/card-edges.json` (semantic `fromLabel`/`emit`/`resource`/`tier` labels over ~2,900 cards), produced at `MaterializeCardEdges` — *before* the intractable tail, so it is generable at the existing timeout-bounded stage. It covers ~2,900 cards vs the bench gate's 33 pinned combos, closing the no-fixture-sibling blind spot that defeats both the round-trip suite and the bench gate.

**Action:** Add `tools/gate-corpus-edge-diff.sh`, run at SKILL.md Step 8 (after re-triage, before recall). Snapshot `card-edges.json` at batch base; after merge regenerate it via the **timeout-bounded** `MaterializeCardEdges` run (kill after that step); diff keyed by `(fromCard, fromLabel, toCard, toLabel, resource, tier)`. Any **non-target** card (not in the batch's dispatched card set) whose edge set changed is a **HALT**, quoting the changed cards + label deltas. Back it with a named carve-out file (`tests/.../edge-diff-expected.json`, keyed by card+reason) per the project's de-ratchet philosophy — a legitimate cross-card reprojection is an explicit, judge-reviewed named entry, never a silent baseline rewrite. Document in SKILL.md Step 8 that this **mechanizes** what the judge does by hand (it complements, does not replace, the judge).

> **Scope it cheaply (fold in Dim 1's tweaks 2 & 3, which are prerequisites, not separate work):** Do **not** diff the full 1.1M-edge union every batch (320 MB → it will be skipped under time pressure, re-opening the hole). Derive the candidate set from each merged branch's touched `[*Rule]`/discriminator and diff only the affected cards' rows. Make this runnable by codifying the timeout-bounded slice as an nx target (see tweak 2 below) so the gate isn't relying on a human remembering the kill-point. If even the scoped union join is slow, fall back to the parser-only projection (`parse → PortWalkProjection` labels) over the candidate cards — strictly cheaper, still catches the label flip.

### 2. HIGH-VALUE — Codify the fast-triage slice as an nx target (and document that the slow tail gates nothing)

This is **promoted to second** because it is the runnability prerequisite for tweak 1's gate. Today there is no `interaction-triage` target (verified — `mast:run` invokes the *other* flow, `MagicAstTriage`), so re-triage runs raw and hits the intractable `MaterializeCycles` tail, forcing an ad-hoc `kill -9`. A gate that depends on a human remembering to kill a flow at the right step is not deterministic.

**Action (no flow code change — Flowthru 0.25.0 supports CLI step-slicing):** In `tests/magic-ast-tests/project.json` add `interaction-triage`: `dotnet run -- --flow InteractionTriage --exclude MaterializeCycles --exclude PortNodes --exclude PlotInteractionGraph` (stops cleanly after `MaterializeCardEdges`, emitting `interaction-triage-report.json` + `port-graph-metrics.json` + `card-edges.json`). Add `interaction-graph` (manual/on-demand) for the full viz. Change SKILL.md Step 8 / FANOUT.md §3.6 from raw `--flow InteractionTriage` to `nx run mast:interaction-triage`. Add 2-3 sentences stating that `MaterializeCycles`/`PortNodes`/`PlotInteractionGraph` produce only the diagnostic viz and gate nothing — the product gate is the independent per-combo `bench:recall` — so the slice is the canonical re-triage and the kill is safe. (Subsumes Dim 3's tweaks 1, 2, and Dim 4's tweak 4.)

### 3. HIGH-VALUE — Split the merge conflict rule: derived artifacts are "discard-both + regenerate, never hand-merge"

The harness `MERGE_AGENT` step 3 (`tdd-fanout-harness-v3.js:178`) and FANOUT.md §3.4c apply "keep-BOTH for additive lines" uniformly. That is correct for hand-written shared files but **wrong** for the three pure-derived artifacts (`libs/magic-ast/schema/ast-schema.json`, `libs/mast-interaction/known-coarse-projections.json`, `libs/magic-ast/GLOSSARY.md`) — a hand keep-both can produce non-canonical key ordering or resurrect a stale name the regen would have dropped. **Not necessary** (the SUITE catches a bad merge red — see below), but the operator should never be told to hand-merge a generated file.

**Action:** Split §3.4c and the harness step into two named branches. (A) Hand-written hot shared files (`AbilityClassifier.cs`, `*RuleHelpers.cs`, `ObjectFilter.cs`, `PortGraph*.cs`, registry-set lines) → existing keep-BOTH/rebase rule, unchanged. (B) Derived artifacts (the three named paths) → "NEVER hand-merge. `git checkout --theirs <path>`, regenerate (`nx run magic-ast:schema` / `dotnet test --filter Regenerate_coarse_projection_whitelist` / `nx run magic-ast:glossary`), stage." Then add to FANOUT.md §3.4e that the discard-both rule is **self-checking**: `SchemaExportTests.Committed_schema_export_is_current` and `PortWalkExhaustivenessTests` (both verified plain `[Test]`s in the gated suite) HALT the merge on a stale artifact; `GLOSSARY.md` is the deliberate exception (`glossary:check` excluded per SKILL.md, regenerated once at wave-end). **Do not add a separate post-merge regen gate** — it would duplicate the in-SUITE staleness tests or re-introduce the glossary false-fail. (Subsumes Dim 2's tweaks 1, 3, 4.)

### 4. HIGH-VALUE — Mandate the corpus-sibling parse sweep in the worker contract + judge scope

The overfit sweep instruction currently lives only in two ad-hoc card-specific design notes, not in the standing contract. The worker's shared-file exception (`mast-worker.md:31`) lists `ObjectFilter/AbilityClassifier/TriggerCondition` but **omits the four `*RuleHelpers.cs` files and unanchored matchers** — exactly where the overfits landed (verified). This is the *process* complement to tweak 1's *mechanical* gate; both should exist (defense in depth).

**Action:** Extend `mast-worker.md`'s shared-file list to include the `*RuleHelpers.cs` files and any unanchored matcher, and require: "If you add/edit a matcher that could match as a substring of a sibling trigger (any non-anchored regex on a shared helper), run it against corpus lines sharing that surface phrase and confirm no sibling is newly mislabeled or has filters dropped. Anchor matchers (`^...$`) by default — an unanchored surface phrase matching inside a more-specific sibling is the #1 FAIL class." Mirror in SKILL.md Step 4's judge scope as an always-judge trigger: any branch touching a `*RuleHelpers` file or adding a non-anchored matcher gets the sibling-sweep judged, never skipped.

### 5. HIGH-VALUE — Add an "ASCII-only, never substitute typography" rule to the gold-input worker contract

Deadeye Navigator drifted on a single U+201C curly→ASCII quote. `seed-gold-input.py` emits corpus text with curly quotes; the worker hand-transcribes; `GoldOracleTextFidelityTests` does no quote-folding and **skips in the worktree** (corpus absent), so the substitution is invisible to worker and judge and only fails the orchestrator's post-merge CORE gate. "Copy verbatim" doesn't register as violated by a one-character "cleanup."

**Action:** In `mast-worker.md` gold-authoring rules (mirror one sentence into SKILL.md's seeded-Input invariant / FANOUT.md §3.2): "Copy the seeded `Input.OracleText` BYTE-FOR-BYTE. Do not normalize typography — keep curly quotes (U+201C/U+201D), apostrophes (U+2019), em/en-dashes exactly as seeded; do not fold to ASCII. A single substituted character is silent in-worktree (the fidelity test skips) and only fails the post-merge CORE gate." Optionally have `seed-gold-input.py` print a WARN when emitted OracleText contains non-ASCII.

### 6. HIGH-VALUE — Codify the 2-strike-FAIL → dedicated-surface escalation as a Stop condition

(Both Dim 1 and Dim 4 raised this independently — deduped here.) Rings of Brighthearth and The One Ring each FAILed twice for *different* root causes before being pulled out, by ad-hoc human call. With no codified threshold, the orchestrator's default is to keep re-dispatching a hard card every batch, burning a worker + Opus judge cycle each time. Tied to the overfit class (Rings is both an overfit example and a 2-strike defer).

**Action:** Add a `[main]` Stop condition to SKILL.md: "A card that FAILs the judge or merge gate TWICE across batches for different root causes is no longer a batch-card — STOP re-dispatching it. Promote it to a dedicated-surface design (`libs/magic-ast/docs/dedicated-surfaces-design.md`) and remove it from the per-batch slate." Track strikes via the existing date-prefixed `verdict-{date}-{batch}.json` artifacts so the count is mechanical, not memory.

### 7. OPTIONAL — `.gitattributes merge=union` for the two JSON artifacts

A conflict-reducer for the common case (two branches each appending a disjoint entry), safe *only because* tweak 3's staleness gate re-validates afterward. Not necessary (tweak 3 already makes conflicts safe and cheap); pure throughput.

**Action:** Create root `.gitattributes` with `libs/magic-ast/schema/ast-schema.json merge=union` and `libs/mast-interaction/known-coarse-projections.json merge=union`. Document that union output is always re-validated by the post-merge staleness gate, never trusted as canonical. **Do not apply union to `GLOSSARY.md`** (Markdown prose — union interleaves garbage; leave to discard-both).

### 8. OPTIONAL — Watch item for `MaterializeCardEdges` quadratic blow-up

Forward-looking, not a current defect. `InteractionUnion.Materialize` is O(emits × consumes) over the parse-ready union; the same intractability that killed `MaterializeCycles` will eventually creep into the "fast" slice as coverage grows toward 4-5 card combos.

**Action:** No code change. Add a one-line burndown watch item: if `nx run mast:interaction-triage` wall time climbs (`port-graph-metrics.json` `TotalEdges` trending toward 10^5), apply the caps/sampling the step's own remark anticipates, or move to the two-layer label-graph path (ADR-0002).

### NOT a loop tweak — FANOUT 1.4 reflection-seam refactors

The `[CharacteristicLabel]`/`[QualifierAxis]`/`[FlowArm]`/`[ClassifierRoute]` registries reduce *shared-edit collision* surface, a different failure class from silent-sibling-overfit. They do not add or remove any detection of a semantic mislabel (Mindcrank and Rings each landed in their own rule file and still overfit). Net-positive architecture, but file them as standalone refactor tickets — **do not gate the loop on them and do not credit them as closing the overfit hole.** Tweak 1 is what closes it.

---

## Bottom line

Ship **tweak 1** before the next batch — it is the only thing standing between the loop and a silently-mislabeled sibling reaching main. Tweaks 2-6 are a single high-value documentation/ergonomics pass (mostly editing `SKILL.md`, `FANOUT.md`, `mast-worker.md`, `project.json`, and `tdd-fanout-harness-v3.js`) that hardens process and makes already-safe behavior legible; bundle them as one follow-up. Tweaks 7-8 are opportunistic.

**Key file paths:**
- Gate target / data source: `tests/magic-ast-tests/Data/_08_Reporting/card-edges.json`, `tests/magic-ast-tests/Flows/InteractionTriage/InteractionTriageFlow.cs`, `tests/magic-ast-tests/Flows/InteractionTriage/Steps/MaterializeCardEdgesStep.cs`
- nx targets: `tests/magic-ast-tests/project.json` (no `interaction-triage` target exists today)
- Merge protocol: `.claude/skills/mast-tdd-loop/scripts/tdd-fanout-harness-v3.js:178` (`MERGE_AGENT` step 3), FANOUT.md §3.4
- Staleness gates (already wired, plain `[Test]`): `tests/magic-ast-tests/Tests/Parse/SchemaExportTests.cs:18`, `tests/magic-ast-tests/Tests/Interaction/PortWalkExhaustivenessTests.cs`
- Worker contract: `.claude/agents/mast-worker.md:31` (shared-file list missing `*RuleHelpers.cs`)
- Product gate (overfit-independent): `tools/bench/MagicAtlas.Bench/ComboExpectedTierTest.cs` (per-combo whitelist)
- No root `.gitattributes` exists (only `docs/reference/misc/external/flowthru/repo/.gitattributes`)
