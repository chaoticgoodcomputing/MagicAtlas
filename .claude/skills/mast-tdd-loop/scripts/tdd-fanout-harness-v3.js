// =============================================================================
// tdd-fanout-harness  —  v3  (PARALLEL FAN-OUT + ORCHESTRATOR SERIAL MERGE)
// =============================================================================
//
// Companion design: tdd-fanout-protocol.md (same dir) — read it first.
//
// WHAT v3 ADDS OVER gold-burndown-execute-v2.js
// ---------------------------------------------
// v2 ran ONE worker per slice, serially (slice owns MAIN, one at a time).
// v3 runs 10–20 worktree-isolated WORKERS in ONE WAVE, then the ORCHESTRATOR
// merges their judge-passed branches back SERIALLY (rebuild + gate between each).
// The fan-out is made safe by:
//   (1) REFLECTION-FIRST framing  — new-file rule/keyword tasks collide with nothing.
//   (2) SOFT non-colliding assignment — tasks are packed into waves whose file
//       touch-sets are pairwise DISJOINT (graph-coloring the conflict graph).
//   (3) ORCHESTRATOR-MANAGED SERIAL MERGE — workers never merge; the orchestrator
//       merges one branch at a time, rebuilds, runs the gates, resolves the
//       unavoidable shared-file conflicts at the merge boundary, and rolls back
//       (defer-and-continue) any merge that goes red.
//
// MODEL ASSIGNMENT (owner decision, baked in):
//   orchestrator = opus (this harness's planning + the merge agents)
//   judges       = opus (mast-judge / interaction-judge)
//   workers      = sonnet (clear-cut, pre-AST'd plan; opus only for novel-shape)
//
// CARRIED FROM v2 (proven): delta-judge (per-gold, not whole-gold), stateless-
// whitelist partial-commit, revert-on-failure leaving a clean tree, defer-and-continue.
//
// RUNNABILITY: this is an ORCHESTRATION SKELETON. The shape (wave packing, model
// overrides, delta-judge fan-out, serial-merge loop) is concrete. The TASKS array
// and a few helpers are marked  // PLACEHOLDER  — fill them from the live triage /
// the burndown plan before running. Harness API mirrors the proven v2 / afk-recall
// scripts: agent(prompt,{label,phase,schema,agentType,model,isolation}),
// parallel([()=>p,...]), phase(title), log(...).
// =============================================================================

export const meta = {
  name: 'tdd-fanout-harness-v3',
  description:
    'Parallel fan-out MAST TDD: wave-packed worktree-isolated Sonnet workers → Opus delta-judge fan-out → orchestrator serial merge (rebuild+gate between each), revert+defer on red.',
  phases: [
    { title: 'Plan — pack waves by touch-set' },
    { title: 'Wave — fan out + judge + serial-merge' },
  ],
}

// -----------------------------------------------------------------------------
// CONSTANTS
// -----------------------------------------------------------------------------
const BASE = 'HEAD'                 // worktree.baseRef:"head" — branches from current integration HEAD
const TODAY = '2026-06-16'          // PLACEHOLDER — set to dispatch date; prefixes branch names for reaping
const WAVE_WIDTH = 18               // 10–20; cap on workers per wave
const SUITE = 'dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --nologo'

// Hot shared files — any task whose touch-set names one of these is collision-prone.
// (From the seam map §1.2. New-file rule/keyword tasks name NONE of these.)
const HOT_FILES = [
  'libs/magic-ast/Parsing/Parsers/Spell/SpellRuleHelpers.cs',
  'libs/magic-ast/Parsing/Parsers/Static/StaticRuleHelpers.cs',
  'libs/magic-ast/Parsing/Parsers/Triggered/TriggeredRuleHelpers.cs',
  'libs/magic-ast/Parsing/Parsers/Activated/ActivatedRuleHelpers.cs',
  'libs/magic-ast/AST/References/ObjectFilter.cs',
  'libs/magic-ast/AST/References/Characteristic.cs',
  'libs/magic-ast/Parsing/AbilityClassifier.cs',
  'libs/magic-ast/Parsing/Tokens/Keywords/Conditionals/ConditionParser.cs',
  'libs/mast-interaction/PortGraph.cs',
  'libs/mast-interaction/PortGraphEngine.cs',
  'libs/mast-interaction/PortWalkProjection.cs',
]

// -----------------------------------------------------------------------------
// TASKS — PLACEHOLDER.
// One entry per TDD task. The Opus PLANNING phase (or a pre-run spec workflow,
// like gold-burndown-spec) produces these from the live triage topYieldClusters +
// the burndown plan. Shape:
//   {
//     id:    'pb4-bucketA',
//     title: 'PB-4 — Persist/Undying counter-gate',
//     cls:   'new-file' | 'shared-edit' | 'interaction' | 'entangled',
//     model: 'sonnet' | 'opus',         // per §4; default sonnet, opus for novel/shared
//     touch: [<absolute-ish repo-rel shared files this task is PREDICTED to write>],
//                                        // [] for new-file tasks (their new rule+gold
//                                        // files are unique by construction)
//     deps:  ['pb1-...'],               // task ids whose MERGE must precede this one
//     golds: ['CSP/PutridGoblin', ...], // affected golds, for the delta-judge fan-out
//     spec:  '<imperative, Sonnet-executable: card DTO + CR rule + exact rule iface ' +
//             'or exact shared-file surgical change. The orchestrator authored the AST.>',
//   }
// 'entangled' tasks are NEVER auto-dispatched here (open risk #3) — they are
// surfaced for HITL design sign-off. 'interaction' tasks are capped at one/wave (§2.4).
// -----------------------------------------------------------------------------
const TASKS = [
  // PLACEHOLDER — paste the planned task array here. Example (new-file, collision-free):
  // {
  //   id: 'hylderblade', title: 'unparsed-triggered (A) Hylderblade',
  //   cls: 'new-file', model: 'sonnet', touch: [], deps: [], golds: ['LCC/Hylderblade'],
  //   spec: 'New [TriggeredRule] AttachTriggeredRule.cs accepting "attach this <X> to target...". ' +
  //         'Card DTO: {...verbatim from triage...}. CR 701.3 (attach): "<verbatim>". ' +
  //         'Drop the rule file + a new gold under HandParsedCards/LCC/Hylderblade.json; make ' +
  //         'dotnet test --filter FullyQualifiedName~Hylderblade green. Do NOT touch any shared file.',
  // },
]

// -----------------------------------------------------------------------------
// SCHEMAS
// -----------------------------------------------------------------------------
const BUILD = {
  type: 'object', additionalProperties: false,
  properties: {
    green: { type: 'boolean', description: 'clean build AND targeted/affected tests pass AND committed on branch' },
    blocked: { type: 'boolean' },
    blockReason: { type: 'string' },
    branch: { type: 'string' },
    regeneratedGolds: { type: 'array', items: { type: 'string' } },
    whitelistEntriesRemoved: { type: 'array', items: { type: 'string' }, description: 'only (card,sink) entries whose sink the gold NO LONGER carries AT ALL' },
    whitelistEntriesKept: { type: 'array', items: { type: 'string' }, description: 'in-scope golds kept because another instance/other-axis residual of that sink remains' },
    filesChanged: { type: 'array', items: { type: 'string' }, description: 'ACTUAL files written — compared to the declared touch-set to refine next wave packing' },
    notes: { type: 'string' },
  },
  required: ['green', 'blocked', 'branch', 'regeneratedGolds', 'notes'],
}
const VERDICT = {
  type: 'object', additionalProperties: false,
  properties: {
    gold: { type: 'string' },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    rationale: { type: 'string' },
  },
  required: ['gold', 'verdict', 'rationale'],
}
const MERGE = {
  type: 'object', additionalProperties: false,
  properties: {
    status: { type: 'string', enum: ['merged', 'rolled-back'] },
    redReason: { type: 'string' },
    summary: { type: 'string' },
    recall: { type: 'string', description: 'recall@Green / @(Green+Amber) after, for interaction merges' },
  },
  required: ['status', 'summary'],
}

// -----------------------------------------------------------------------------
// PROMPT FRAGMENTS
// -----------------------------------------------------------------------------
const WORKER_PRELUDE = (branch) =>
  `You run in an isolated git worktree (the harness set isolation:'worktree').\n` +
  `Step 0 (FIRST, before any edit): run \`bash tools/gate-isolation.sh ${BASE}\`. ` +
  `If it exits nonzero, STOP, make NO changes, return green:false blocked:true blockReason="isolation failed: <verbatim>". ` +
  `Else: WORKTREE_ROOT="$(pwd)"; \`git -C "$WORKTREE_ROOT" checkout -b ${branch}\`.\n` +
  `Rules: never cd; relative paths for Read/Write/Edit; git ONLY via git -C "$WORKTREE_ROOT"; nx unavailable — use dotnet directly. ` +
  `Never edit an existing gold to pass a test (additions-only; the immutability gate enforces it). Never disable a gate. ` +
  `VERIFY every card fact against the in-worktree oracle-cards.json / glossary.json — NEVER from memory. ` +
  `DELTA-SCOPE: structure ONLY this task's target axis/residual; leaving a DIFFERENT axis's residual (and its whitelist entry) is CORRECT — another task owns it. ` +
  `WHITELIST: remove a (card,sink) entry IFF the card stops carrying that sink ENTIRELY; if another instance/axis of that sink remains, KEEP the entry. ` +
  `If you achieve clean build + green targeted tests, COMMIT on ${branch} (git commit --no-verify) and return green:true. Do NOT merge. ` +
  `If you CANNOT: fully revert (git checkout -- . && git clean -fd tests libs), return green:false blocked:true with a precise blockReason. Never leave a half-changed tree.`

const DELTA_JUDGE = (task, gold) =>
  `DELTA-JUDGE the regenerated gold '${gold}' on UNMERGED branch '${task.branch}' for task ${task.id} — "${task.title}".\n` +
  `This is a DELTA judgment, NOT whole-gold purity. Inspect via \`git diff ${BASE}..${task.branch} -- <paths>\` and \`git show ${task.branch}:tests/magic-ast-tests/Fixtures/HandParsedCards/${gold}.json\`. ` +
  `Confirm the real oracle text (oracle-cards.json / rules-structure.json). PASS iff ALL hold:\n` +
  `  (a) this task's TARGET residual(s) on this gold were structured CORRECTLY (right node/axis, faithful to the real card);\n` +
  `  (b) NO NEW free-text/unparsed residual introduced BEYOND this task's scope (PRIMARY criterion);\n` +
  `  (c) NO regression: no dropped/added/inverted ability or effect; siblings preserved; out-of-axis nodes serialize unchanged.\n` +
  `EXPLICITLY NOT A FAIL: a residual on a DIFFERENT axis, or a second instance of this sink the task could not reach — another task owns those.\n` +
  `TASK SPEC (the target): ${task.spec}\n` +
  `Strict PASS/FAIL + one-line rationale.`

// The orchestrator's NON-isolated serial-merge agent. Merges ONE judge-passed
// branch, rebuilds, runs the gates, resolves shared-file conflicts at the boundary,
// rolls back (reset --hard HEAD@{1}) + defers on red. This is priority 3.
const MERGE_AGENT = (task, judgeNote) =>
  `Integration agent — NON-isolated, you run in the orchestrator's main checkout on the integration branch. ` +
  `Merge the judge-PASSED branch \`${task.branch}\` and gate it. ${judgeNote}\n` +
  `Steps (any red → ROLL BACK this merge only, leave the branch intact, return status:rolled-back with redReason):\n` +
  `1. \`bash tools/gate-fixture-immutability.sh ${BASE} ${task.branch}\` — additions-only; nonzero → rolled-back (worker illicitly edited a gold).\n` +
  `2. \`git merge --no-verify ${task.branch}\`.\n` +
  `3. ON CONFLICT (only ever on a hot shared file): resolve HERE at the boundary — keep-BOTH for additive routing/registry/field lines; for a genuine semantic overlap, rebase ${task.branch} onto the merge result and re-run its targeted test. Do NOT push conflict resolution back to the worker.\n` +
  `4. REBUILD clean: \`dotnet build tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj -v q -clp:ErrorsOnly\`.\n` +
  `5. GATE (no ratchet tolerance): ${task.gateCmd}\n` +
  (task.cls === 'interaction'
    ? `   PLUS bench recall must NOT decrease: \`cd tools/bench/MagicAtlas.Bench && dotnet run -- --write\` (auto-advances on a gain; report the recall numbers).\n`
    : '') +
  `6. GREEN → keep the merge; commit any advanced baselines/regenerated snapshots (git add -A && git commit --no-verify). Return status:merged.\n` +
  `7. RED → \`git reset --hard HEAD@{1}\` (undo THIS merge only), leave ${task.branch} intact, return status:rolled-back + redReason.\n` +
  `Do NOT push.`

// -----------------------------------------------------------------------------
// WAVE PACKING — graph-color the conflict graph into disjoint-touch-set waves (§2.2)
// -----------------------------------------------------------------------------
function packWaves(tasks) {
  const dispatchable = tasks.filter((t) => t.cls !== 'entangled') // entangled → HITL, never auto-dispatched
  const heldEntangled = tasks.filter((t) => t.cls === 'entangled')

  const remaining = [...dispatchable]
  const merged = new Set()        // task ids whose MERGE has completed (deps gate on this)
  const waves = []

  // A task is eligible for THIS wave if all its deps are already merged.
  // Within a wave, a task may join only if its touch-set is disjoint from every
  // task already in the wave, the wave is under WAVE_WIDTH, and (interaction cap)
  // at most one interaction task per wave.
  while (remaining.length) {
    const wave = []
    const claimedFiles = new Set()
    let interactionInWave = false

    for (let i = 0; i < remaining.length && wave.length < WAVE_WIDTH; i++) {
      const t = remaining[i]
      if (!t.deps.every((d) => merged.has(d))) continue           // dep not yet merged → wait
      if (t.cls === 'interaction' && interactionInWave) continue   // §2.4 cap
      const overlaps = (t.touch || []).some((f) => claimedFiles.has(f))
      if (overlaps) continue                                       // touch-set collision → next wave
      // accept
      wave.push(t)
      ;(t.touch || []).forEach((f) => claimedFiles.add(f))
      if (t.cls === 'interaction') interactionInWave = true
    }

    if (!wave.length) {
      // No task became eligible — a dep cycle or an unmergeable predecessor. Bail safely.
      log(`PACK STALL: ${remaining.length} tasks remain but none are eligible (dep not merged?). Holding them.`)
      break
    }
    waves.push(wave)
    wave.forEach((t) => merged.add(t.id))                          // OPTIMISTIC: planning assumes merge; the real loop reconciles
    wave.forEach((t) => { remaining.splice(remaining.indexOf(t), 1) })
  }
  return { waves, heldEntangled, stalled: remaining }
}

// -----------------------------------------------------------------------------
// PER-TASK gate command. OPEN RISK #1 lever lives here:
//   FULL  = run the whole CORE ring between every merge (purest, slowest)
//   FAST  = --filter to the affected tests between merges; full ring once at wave end
// Default FULL for shared-edit/interaction (collision-prone, deserve the full ring);
// new-file branches are provably disjoint → targeted gate + one full-ring consolidation
// at wave end. RATIFIED 2026-06-16 (open risk #1): HYBRID throughput is the chosen strategy.
// -----------------------------------------------------------------------------
function gateCmdFor(task) {
  if (task.cls === 'new-file') {
    const filt = task.golds?.[0]?.split('/').pop()?.replace(/[^A-Za-z0-9]/g, '') || ''
    return `\`${SUITE}${filt ? ` --filter "FullyQualifiedName~${filt}"` : ''}\` (targeted; full CORE ring runs once at wave end).`
  }
  return `\`${SUITE}\` — full CORE ring (gold fidelity, no-unparsed, round-trip, DestringSinkRatchet, DiscriminatorUniqueness, PortWalkExhaustiveness). PLUS \`nx run magic-ast:lint-discriminators\` + advance baseline.`
}

// -----------------------------------------------------------------------------
// THE FAN-OUT: build a whole wave in parallel, delta-judge each PASSED branch,
// queue PASSED branches for serial merge.
// -----------------------------------------------------------------------------
async function runWave(wave, waveIdx) {
  phase(`Wave ${waveIdx} — fan out + judge + serial-merge`)
  log(`Wave ${waveIdx}: ${wave.length} workers (${wave.filter((t) => t.cls === 'new-file').length} new-file, ${wave.filter((t) => t.cls !== 'new-file').length} shared/interaction)`)

  // ── (1) FAN OUT: all workers in parallel, worktree-isolated, per-task model ──
  const builds = await parallel(
    wave.map((t) => () => {
      t.branch = `mast-tdd/${TODAY}-${t.id}`
      t.gateCmd = gateCmdFor(t)
      return agent(
        `${WORKER_PRELUDE(t.branch)}\n\nTASK ${t.id} — ${t.title}\n\nSPEC: ${t.spec}`,
        {
          label: `build:${t.id}`,
          phase: `Wave ${waveIdx}`,
          isolation: 'worktree',
          model: t.model || 'sonnet',          // §4: workers default Sonnet
          agentType: 'mast-worker',
          schema: BUILD,
        }
      ).then((b) => ({ task: t, build: b }))
    })
  )

  // ── (2) DELTA-JUDGE: per-gold, only on green branches; Opus judges ──
  const passed = []      // {task, build} ready to merge
  const deferred = []
  for (const { task, build } of builds) {
    if (!build || !build.green || build.blocked) {
      log(`DEFER ${task.id} (build): ${build ? (build.blockReason || build.notes) : 'worker died'}`)
      deferred.push({ id: task.id, status: 'deferred-build', detail: build })
      continue
    }
    // touch-set reconciliation (soft assignment, §2.5): note any out-of-prediction write for next wave
    const stray = (build.filesChanged || []).filter((f) => HOT_FILES.includes(f) && !(task.touch || []).includes(f))
    if (stray.length) log(`NOTE ${task.id} touched undeclared hot file(s): ${stray.join(', ')} — refine next wave's packing`)

    const golds = (build.regeneratedGolds || task.golds || []).filter(Boolean)
    const judgeType = task.cls === 'interaction' ? 'interaction-judge' : 'mast-judge'
    const verdicts = golds.length
      ? (await parallel(
          golds.map((g) => () =>
            agent(DELTA_JUDGE(task, g), {
              label: `judge:${task.id}:${g}`,
              phase: `Wave ${waveIdx}`,
              model: 'opus',                    // §4: all judges Opus
              agentType: judgeType,
              schema: VERDICT,
            })
          )
        )).filter(Boolean)
      : []
    const fails = verdicts.filter((v) => v.verdict !== 'PASS')
    if (fails.length) {
      log(`DEFER ${task.id} (judge): DELTA-FAIL on ${fails.map((f) => f.gold).join(', ')} — branch held unmerged for review`)
      deferred.push({ id: task.id, status: 'deferred-judge', fails, verdicts })
      continue
    }
    passed.push({ task, build, verdicts })
  }

  // ── (3) ORCHESTRATOR SERIAL MERGE: one branch at a time, file-affinity order,
  //        rebuild + gate BETWEEN each, roll back + defer on red (priority 3). ──
  // File-affinity order: new-file first (trivial ff), then AbilityClassifier, then
  // interaction/orchestration last.
  const order = { 'new-file': 0, 'shared-edit': 1, 'interaction': 2, 'entangled': 3 }
  passed.sort((a, b) => (order[a.task.cls] ?? 1) - (order[b.task.cls] ?? 1))

  const committed = []
  for (const { task, build, verdicts } of passed) {
    const note = `${task.cls === 'interaction' ? 'interaction-judge' : 'mast-judge'} PASSED ${verdicts.length} gold(s).`
    const merge = await agent(MERGE_AGENT(task, note), {
      label: `merge:${task.id}`,
      phase: `Wave ${waveIdx}`,
      model: 'opus',                            // §4: orchestrator/merge = Opus
      schema: MERGE,
    })
    if (merge?.status === 'merged') {
      log(`MERGED ${task.id} (${verdicts.length} delta-PASS; removed ${(build.whitelistEntriesRemoved || []).length}, kept ${(build.whitelistEntriesKept || []).length})`)
      committed.push({ id: task.id, build, verdicts, merge })
    } else {
      log(`DEFER ${task.id} (merge rolled back): ${merge?.redReason || merge?.summary}`)
      deferred.push({ id: task.id, status: 'deferred-merge', merge })
    }
  }

  // ── (4) END OF WAVE: full CORE ring once (catches anything the fast/targeted
  //        between-merge gates skipped), then glossary + recall + reap. ──
  // PLACEHOLDER: this end-of-wave consolidation agent is where open-risk-#1 option (b)
  // is realized — the new-file branches merged under a targeted gate are re-validated
  // here by the full ring exactly once.
  const consolidate = await agent(
    `End-of-wave ${waveIdx} consolidation (NON-isolated, main checkout). ` +
      `1. Run the FULL CORE ring once: \`${SUITE}\` — 0 failed required (catches any joint regression the between-merge targeted gates skipped). ` +
      `If RED: identify the offending merged branch (sentinel snapshot diff usually localizes it), \`git revert\` it, re-run until green, and report which task was backed out. ` +
      `2. \`nx run magic-ast:glossary\` && commit GLOSSARY. 3. \`nx run mast:run\` (re-triage) && \`nx run bench:recall\` (HALT if recall dropped). ` +
      `4. Reap: \`nx run mast:worktree-clean\`. Report committed task ids, any backed-out task, and the recall numbers.`,
    { label: `consolidate:wave${waveIdx}`, phase: `Wave ${waveIdx}`, model: 'opus', schema: MERGE }
  )

  return { committed, deferred, consolidate }
}

// =============================================================================
// MAIN
// =============================================================================
phase('Plan — pack waves by touch-set')

if (!TASKS.length) {
  log('TASKS is empty (PLACEHOLDER). Populate it from the live triage / burndown plan before running.')
  return { error: 'no tasks — fill the TASKS placeholder' }
}

// Pre-flight: a clean, isolated environment (skill Step 0). HALT on nonzero.
await agent(
  `Pre-flight (NON-isolated). Run \`bash tools/gate-preflight.sh\`. If nonzero, return blocked with its output verbatim — do NOT proceed. ` +
    `Also confirm we are on the integration branch feat/mast-improvements and the tree is clean.`,
  { label: 'preflight', phase: 'Plan — pack waves by touch-set', model: 'opus' }
)

const { waves, heldEntangled, stalled } = packWaves(TASKS)
log(`Planned ${waves.length} wave(s) from ${TASKS.length} tasks. ` +
    `${heldEntangled.length} entangled held for HITL: ${heldEntangled.map((t) => t.id).join(', ') || '(none)'}. ` +
    `${stalled.length} stalled (unmergeable dep): ${stalled.map((t) => t.id).join(', ') || '(none)'}.`)

const allCommitted = []
const allDeferred = []
let waveIdx = 0
for (const wave of waves) {
  waveIdx++
  const { committed, deferred } = await runWave(wave, waveIdx)
  allCommitted.push(...committed.map((c) => c.id))
  allDeferred.push(...deferred)
  // NOTE: packWaves assumed every wave-task merges; if a dep-bearing successor's
  // predecessor actually DEFERRED, a production harness re-packs the remaining tasks
  // against the REAL merged-set here before the next wave. (Re-pack hook — PLACEHOLDER.)
}

log(`Run complete: ${allCommitted.length} merged, ${allDeferred.length} deferred, ` +
    `${heldEntangled.length} entangled-held (HITL), ${stalled.length} stalled.`)
return {
  merged: allCommitted,
  deferred: allDeferred,
  entangledHeldForHumanDesign: heldEntangled.map((t) => ({ id: t.id, title: t.title })),
  stalled: stalled.map((t) => t.id),
}
