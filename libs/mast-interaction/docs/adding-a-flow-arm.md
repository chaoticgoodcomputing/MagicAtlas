# Adding a flow arm — the reference pattern

A **flow arm** is a rules-defined edge the interaction engine derives between two cards' ports — "an
emit of kind X satisfies a consume of role Y" (ADR-0002 §5/§6). Adding one is how the engine learns
to reconstruct a new family of combos. Most "missed combos" (see `tools/bench/MagicAtlas.Bench` recall)
are a *missing flow arm*, not a parser bug.

This doc is the canonical worked example. **Copy this pattern; do not copy the anti-patterns at the
bottom.** The worked example is the **life-drain arm** (CR 119): Vito × Exquisite Blood,
Marauding Blight-Priest combos — landed 2026-06-15, interaction-judge-verified zero-false-positive
(`docs/judgments/verdict-2026-06-15-life-arm.json`), recall@Green 0→0.061, recall@Amber 0.24→0.36.

## The split — two layers, each its own job

A missed combo is almost always **two** problems on **two** layers. Fix each in its own layer:

| Layer | Job | Where | The life example |
|---|---|---|---|
| **Parse** (magic-ast → PortWalk) | *Describe* the effect/trigger faithfully as a port **label** + a **Subject** | `PortGraph.cs`, `PortLabel.cs`, `PortWalkProjection.cs` | `gainLife`→`emit:life:gain:<scope>`; `GainsLife`→`trigger:life:gain:<scope>` |
| **Interaction** (engine) | *Connect* emit→consume (rules-implicit, cross-card) + let the **operator tier** it | `PortGraphEngine.cs` `FlowFeasible` | a `("life","trigger")` arm; player-scope overlap → Green/Amber |

The boundary is load-bearing (see ADR-0002 + the `feedback_mast_describes_not_executes` principle):
**intra-card, lexical** meaning of explicit text lives in parse; **cross-card, causal, uncertain**
connections live in the engine. A flow arm is always the latter.

## The recipe (what the life arm did)

1. **Project a faithful label + Subject** (`PortGraph.cs`). Add a case to `EmitPort`/`Trigger` that
   emits a real label and carries the discriminating filter as the port **`Subject`** — the operator
   tiers on the Subject, so this is what determines Green vs Amber. (Life: the affected/watched
   *player* via `PlayerFilter` / the trigger filter.)
2. **Add the discriminator to `PortWalkProjection`** and **remove it from
   `known-coarse-projections.json`** (run `nx run magic-ast:lint-discriminators` mentally — the 03
   ratchet *shrinks*; the blind-spot metric drops). A discriminator that stays coarse emits a label no
   arm reads → zero recall.
3. **Add the arm** (`PortGraphEngine.cs` `FlowFeasible`) — one `switch` clause matching
   `(ResourceKind(emit), Role(consume))`, plus a tiny feasibility helper if needed (life: same
   gain/loss direction). **The arm decides *feasibility* only; it does NOT decide certainty** — that
   is `AddRulesEdge`'s operator overlap on the Subjects ("the label names, the operator decides",
   ADR-0002 §7).
4. **Pin it**: add the exemplar combo to the sentinel manifest, regenerate snapshots (a *justified*
   diff — cite the arm), let `bench:recall` advance its baseline, and **dispatch the
   `interaction-judge`** on every new edge.

## Gate sequence (all must hold — this is the safety net)

- `PortWalkExhaustivenessTests` (03 ratchet) **shrinks** — the new discriminators left the allowlist.
- `PortWalkSentinelSnapshotTest` regenerated with a justified diff (tier/label changes are *expected*).
- `nx run bench:recall` — recall **did not decrease** (it rose; the ratchet advanced the baseline).
- **`interaction-judge` PROCEED** — every new GREEN is genuinely reliable (the false-positive guard),
  every AMBER soundly irreducible. A GREEN it can't justify is a FAIL: stop.
- `nx run mast:test` green.

## Anti-patterns — do NOT do these

1. **Don't encode the connection in the AST/parser.** "A sacrificed creature dies", "a life-loss
   triggers a life-loss watcher" are *rules consequences* across cards — they belong in the engine's
   flow grammar, never as a field the parser writes. The AST transcribes what the card *says*.
2. **Don't fudge a GREEN.** When the gold is imprecise, the honest tier is AMBER. The life arm's Vito
   hop is AMBER because Vito's gold models "target **opponent**" as an unqualified `target player`
   (`{CardTypes:[player]}`) — the operator can't certify the loser is an opponent. The GREEN ceiling
   is earned by a **parse-layer** sharpen of the gold ("target opponent" → opponent-scoped), **never**
   by relaxing the operator or the arm. Marauding Blight-Priest reaches GREEN honestly because its gold
   *is* opponent-scoped (`EachOpponent`).
3. **Don't let a non-scalar resource hit the scalar null-default GREEN.** `AddRulesEdge` defaults a
   **null** Subject to `Overlaps + reliability Yes` (GREEN) — correct only for a fungible scalar like
   mana. Life/cards/players are *scoped*, so a null Subject there would be a false-positive vector.
   Always carry a **non-null** Subject (life uses `PlayerFilter` + the `AnyPlayer` floor). This was the
   subtle bug caught while building this arm; the judge confirmed the fix.
4. **Don't leave the discriminator coarse.** If you add the arm but skip step 2, the emit still
   projects `emit:<x>` and no arm matches — silent zero recall, and the 03 ratchet will flag the
   unprojected discriminator.

## Files (the life arm, to copy from)

- `libs/mast-interaction/PortLabel.cs` — `LifeGainEmit`/`LifeLossEmit`/`LifeGainTrigger`/`LifeLossTrigger`
- `libs/mast-interaction/PortGraph.cs` — `EmitPort` life cases, `Trigger` life branches, `PlayerFilter` + `AnyPlayer`
- `libs/mast-interaction/PortWalkProjection.cs` — `gainLife`/`loseLife`/`GainsLife`/`LosesLife`
- `libs/mast-interaction/PortGraphEngine.cs` — `FlowFeasible` `("life","trigger")` + `LifeFlowFeasible`
- `tests/magic-ast-tests/Tests/Interaction/Snapshots/vito-x-exquisite-blood-life-flow-arm-exemplar.json`
