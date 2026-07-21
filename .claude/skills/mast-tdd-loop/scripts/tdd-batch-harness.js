// =============================================================================
// tdd-batch-harness  —  ONE MAST TDD batch, args-driven (v3 fan-out + PLAN phase)
// =============================================================================
//
// Companion design: FANOUT.md + SKILL.md (same skill dir). This is the runnable
// batch driver invoked once per batch by the orchestrator (main loop), which:
//   - picks N families from the fresh triage,
//   - seeds each card's authoritative gold Input from the corpus (seed-gold-input.py),
//   - passes them here as `args.families`.
//
// The batch runs three phases:
//   1. PLAN  (Opus, non-isolated, main checkout): one planner per family. Pulls the
//      verbatim CR rule text from libs/mtg-rules/.../rules-structure.json, reads
//      GLOSSARY.md to find the reusable AST node + the right reflection rule interface
//      + a fixture to mirror, declares cls/model/touch-set, and writes a self-contained
//      worker BRIEF (mechanic + CR + AST recommendation + card-scope notes). It does NOT
//      transcribe Input — the harness injects the pre-seeded Input byte-exact.
//   2. WAVE  (workers Sonnet in worktrees → Opus delta-judge → Opus serial-merge):
//      tasks are wave-packed so touch-sets are pairwise disjoint; workers fan out;
//      each green branch is delta-judged per gold; PASSED branches merge serially
//      (rebuild + gate between each), revert+defer on red.
//   3. CONSOLIDATE (Opus, main checkout): full CORE ring once, regen GLOSSARY, re-triage,
//      bench:recall gate, reap worktrees. Returns the numbers the orchestrator reports.
//
// MODEL: orchestrator/planner/judge/merge = opus (inherited from the Opus main loop);
//        workers = sonnet (overridden per-task, opus only for novel-shape).
//
// args = {
//   families: [{ slug, rank, cardName, mechanic, targetLine, template,
//                dominantPattern, dominantRule, model, input:<seeded Input obj>,
//                scopeNote }],
//   today: 'YYYY-MM-DD', batch: <n>, waveWidth: <n>,
// }
// =============================================================================

export const meta = {
  name: 'mast-tdd-batch',
  description:
    'One MAST TDD batch: Opus plan (CR+AST+touch-set) → wave-packed Sonnet workers (worktree) → Opus delta-judge → Opus serial-merge (rebuild+gate) → Opus consolidate (glossary+re-triage+recall+reap).',
  phases: [{ title: 'Plan' }, { title: 'Wave' }, { title: 'Consolidate' }],
}

// The per-batch payload. The orchestrator's builder script replaces this line
// (byte-exact json.dumps of {today,batch,waveWidth,families:[…]}) so unicode
// oracle text never round-trips through an LLM. Falls back to runtime `args`.
const __BATCH_ARGS = null

// ── CONSTANTS ────────────────────────────────────────────────────────────────
const __IN = (typeof args !== 'undefined' && args && args.families) ? args : __BATCH_ARGS
// The FIXED integration-HEAD sha captured at launch (worktrees branch from it via
// baseRef:"head"). MUST be a fixed sha, NOT 'HEAD' — the immutability gate + judge
// diffs run during serial merge while HEAD advances, so a literal 'HEAD' phantom-flags
// every later branch's fork-point as deletions/mods (the batch-1 false-positive defers).
const BASE = (__IN && __IN.baseSha) || 'HEAD'
const families = (__IN && __IN.families) || []
const TODAY = (__IN && __IN.today) || 'undated'
const BATCH = (__IN && __IN.batch) || 0
const WAVE_WIDTH = (__IN && __IN.waveWidth) || 10
const SUITE = 'dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --nologo'

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

// ── SCHEMAS ──────────────────────────────────────────────────────────────────
const PLAN = {
  type: 'object',
  additionalProperties: false,
  properties: {
    slug: { type: 'string' },
    dispatchable: { type: 'boolean', description: 'false iff the mechanic is not in glossary.json, the card has a non-target line that will not parse, or it needs HITL/entangled design' },
    skipReason: { type: 'string' },
    cardName: { type: 'string' },
    fixturePath: { type: 'string', description: 'repo-rel gold path, e.g. tests/magic-ast-tests/Fixtures/HandParsedCards/<SET>/<CardNoSpaces>.json' },
    cls: { type: 'string', enum: ['new-file', 'shared-edit', 'interaction', 'entangled'] },
    model: { type: 'string', enum: ['sonnet', 'opus'] },
    touch: { type: 'array', items: { type: 'string' }, description: 'hot shared files this task is predicted to write ([] for new-file)' },
    crCitations: { type: 'string', description: 'CR rule number(s) + VERBATIM text pulled from rules-structure.json — the ground truth the worker cites and the judge cross-references' },
    astPlan: { type: 'string', description: 'recommended AST node(s) + exact reflection rule interface ([StaticRule]/[TriggeredRule]/[SpellRule]/[Keyword]/…) + which existing rule/fixture to mirror' },
    workerBrief: { type: 'string', description: 'the self-contained brief handed to the Sonnet worker (mechanic + CR + AST recommendation + card-scope notes for EVERY ability on the card). Do NOT include the Input JSON — the harness injects it.' },
  },
  required: ['slug', 'dispatchable', 'cardName', 'cls', 'model', 'touch', 'workerBrief'],
}
const BUILD = {
  type: 'object',
  additionalProperties: false,
  properties: {
    green: { type: 'boolean' },
    blocked: { type: 'boolean' },
    blockReason: { type: 'string' },
    branch: { type: 'string' },
    regeneratedGolds: { type: 'array', items: { type: 'string' } },
    filesChanged: { type: 'array', items: { type: 'string' } },
    newDiscriminators: { type: 'array', items: { type: 'string' } },
    sharedFileEdits: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
  required: ['green', 'blocked', 'branch', 'notes'],
}
const VERDICT = {
  type: 'object',
  additionalProperties: false,
  properties: {
    gold: { type: 'string' },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    rationale: { type: 'string' },
  },
  required: ['gold', 'verdict', 'rationale'],
}
const MERGE = {
  type: 'object',
  additionalProperties: false,
  properties: {
    status: { type: 'string', enum: ['merged', 'rolled-back', 'green', 'red'] },
    redReason: { type: 'string' },
    summary: { type: 'string' },
    recall: { type: 'string' },
  },
  required: ['status', 'summary'],
}

// ── PROMPT FRAGMENTS ───────────────────────────────────────────────────────────
const PLANNER = (f) =>
  `You are an Opus PLANNER for the MAST TDD loop, running NON-isolated in the main checkout. READ-ONLY: produce a brief, do NOT edit code.\n` +
  `Family '${f.slug}' (triage cluster #${f.rank}) — mechanic: ${f.mechanic}.\n` +
  `Target card: ${f.cardName}. Target unparsed line: ${JSON.stringify(f.targetLine)}. Normalized template: ${JSON.stringify(f.template)}. ` +
  `Fails in: ${f.dominantPattern} / ${f.dominantRule}. Orchestrator scope note: ${f.scopeNote || '(none)'}.\n` +
  `FULL oracle text of the card (authoritative, corpus-seeded — you reason about ALL its abilities, not just the target line):\n${JSON.stringify(f.input.OracleText)}\n` +
  `TypeLine: ${JSON.stringify(f.input.TypeLine)} ManaCost: ${JSON.stringify(f.input.ManaCost || '')}.\n\n` +
  `DO:\n` +
  `1. Identify the MTG mechanic(s). Pull the EXACT CR rule number(s) + VERBATIM text from libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json (jq it — do NOT paraphrase a number from memory). Also glossary.json for keyword-action definitions. If the mechanic is genuinely absent from that data, set dispatchable:false with skipReason.\n` +
  `2. Read libs/magic-ast/GLOSSARY.md + a similar existing rule file/fixture. Decide the reusable AST node(s) + the exact reflection rule interface to add ([SpellRule]/[StaticRule]/[TriggeredRule]/[TriggerConditionRule]/[Activated*Rule]/[Keyword]) — prefer a NEW reflection-discovered file (collision-free). Name the existing rule/fixture the worker should mirror.\n` +
  `3. Classify: cls='new-file' if closing it is only new [attr] file(s)+gold (touch:[]); 'shared-edit' if it must write a HOT shared file (list them in touch: ${JSON.stringify(HOT_FILES)}); 'interaction' if it touches PortGraph/PortGraphEngine/PortWalkProjection; 'entangled' (→ NOT dispatched) if it couples parser+engine-firability or needs a new Effect trait / HITL design.\n` +
  `4. model: 'sonnet' for a clear-cut established-pattern slice; 'opus' for novel-shape (new node/keyword-action/replacement subtlety/transform). Orchestrator suggested: ${f.model}.\n` +
  `5. CARD-SCOPE CHECK: confirm EVERY other ability on the card already parses (mirror how existing fixtures encode them). If a NON-target line will NOT parse with the current parser + your planned change, set dispatchable:false with skipReason (do NOT hand the worker a card it cannot make fully green).\n` +
  `6. fixturePath: tests/magic-ast-tests/Fixtures/HandParsedCards/<SET>/<CardNameNoSpaces>.json — look up the card's set code in tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json.\n` +
  `7. Write workerBrief: a self-contained brief for a Sonnet worker — the mechanic, the VERBATIM CR citation(s), the recommended AST shape + exact rule interface + the fixture/rule to mirror, and explicit notes on how to encode EVERY other ability on the card. Do NOT include the Input JSON (the harness injects it byte-exact). Do NOT dictate — establish rules facts + a concrete shape the worker implements.\n` +
  `Return the PLAN object.`

const WORKER_PRELUDE = (branch, base) =>
  `You run in an isolated git worktree (isolation:'worktree').\n` +
  `Step 0 (FIRST, before any edit): run \`bash tools/gate-isolation.sh ${base}\`. If nonzero → STOP, make NO changes, return green:false blocked:true blockReason="isolation failed: <verbatim>". ` +
  `Else: WORKTREE_ROOT="$(pwd)"; \`git -C "$WORKTREE_ROOT" checkout -b ${branch}\`.\n` +
  `Rules: never cd; RELATIVE paths for Read/Write/Edit; git ONLY via git -C "$WORKTREE_ROOT"; nx unavailable → use dotnet directly. ` +
  `Copy the GOLD INPUT below BYTE-FOR-BYTE into the fixture's "Input" field (keep curly quotes/apostrophes/em-dashes exactly — a single substituted char silently fails the orchestrator's fidelity gate). Author only the "Output" AST + parser. ` +
  `Gold = what a FULLY-implemented parser SHOULD emit: NEVER "Kind":"unparsed"/"EffectType":"unparsed", no Diagnostics[], no Pattern/RawText fallback. MAST DESCRIBES not executes. Timing and effect are SEPARATE composable nodes. No free-text that carries rules structure. Model EVERY ability on the card. ` +
  `Prefer a NEW reflection-discovered rule file over editing a shared *RuleHelpers/AbilityClassifier. Anchor matchers; if you add a non-anchored matcher or touch a shared file, grep the corpus for siblings sharing the surface phrase and confirm none is newly mislabeled (report the sweep). ` +
  `New discriminator → make a PortWalk projection decision (semantic case in PortGraph.cs + PortWalkProjection, or a justified entry in known-coarse-projections.json) or PortWalkExhaustivenessTests fails. ` +
  `Cite ONLY the CR text in the brief. Never touch GLOSSARY.md. ` +
  `Test ONLY your card: \`dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --filter "FullyQualifiedName~<CardNoSpaces>" --nologo\` (if you touched a shared file, run the whole project once to check siblings). ` +
  `If green: \`git -C "$WORKTREE_ROOT" add -A && git -C "$WORKTREE_ROOT" commit --no-verify -m "<msg>"\`, return green:true with regeneratedGolds + filesChanged + newDiscriminators + sharedFileEdits. Do NOT merge. ` +
  `If you CANNOT make the WHOLE card green: fully revert (git checkout -- . && git clean -fd tests libs), return green:false blocked:true with a precise blockReason. Never leave a half-changed tree.`

const DELTA_JUDGE = (task, gold, base) =>
  `DELTA-JUDGE the regenerated gold '${gold}' on UNMERGED branch '${task.branch}' — task ${task.slug} ("${task.mechanic}").\n` +
  `Inspect via \`git diff ${base}..${task.branch}\` and \`git show ${task.branch}:${gold}\`. Confirm the real oracle text (oracle-cards.json) + cross-check the brief's CR citation against libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json.\n` +
  `PASS iff ALL hold: (a) the target line is structured CORRECTLY (right node/discriminator, faithful to the card, describe-not-execute, no baked-in timing); (b) NO new free-text/unparsed residual introduced; (c) NO regression — no dropped/added/inverted ability, siblings preserved, out-of-axis nodes unchanged; (d) the cited CR rule genuinely exists in the data and matches the modeling. ` +
  `A residual on a DIFFERENT axis that another task owns is NOT a fail. Strict PASS/FAIL + one-line rationale.`

const MERGE_AGENT = (task, gateCmd, judgeNote, base) =>
  `Integration agent — NON-isolated, main checkout on the integration branch. Merge the judge-PASSED branch \`${task.branch}\` and gate it. ${judgeNote}\n` +
  `Steps (any red → ROLL BACK this merge only, leave the branch intact, return status:rolled-back + redReason):\n` +
  `1. \`bash tools/gate-fixture-immutability.sh ${base} ${task.branch}\` — additions-only; nonzero → rolled-back (worker illicitly edited a gold).\n` +
  `2. \`git merge --no-verify ${task.branch}\`.\n` +
  `3. ON CONFLICT (only ever on a hot shared file): resolve HERE — keep-BOTH for additive routing/registry/field lines; for a genuine semantic overlap, rebase ${task.branch} onto the merge result and re-run its targeted test. Do NOT push resolution back to the worker.\n` +
  `4. REBUILD clean: \`dotnet build tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj -v q -clp:ErrorsOnly\`.\n` +
  `5. GATE (no ratchet tolerance): ${gateCmd}\n` +
  (task.cls === 'interaction'
    ? `   PLUS bench recall must NOT decrease: \`cd tools/bench/MagicAtlas.Bench && dotnet run -- --write\` (report recall numbers).\n`
    : '') +
  `6. GREEN → keep the merge; commit any advanced baselines (git add -A && git commit --no-verify). Return status:merged.\n` +
  `7. RED → \`git reset --hard HEAD@{1}\` (undo THIS merge only), leave ${task.branch} intact, return status:rolled-back + redReason.\n` +
  `Do NOT push.`

function gateCmdFor(task) {
  if (task.cls === 'new-file') {
    const filt = (task.cardName || '').replace(/[^A-Za-z0-9]/g, '')
    return `\`${SUITE}${filt ? ` --filter "FullyQualifiedName~${filt}"` : ''}\` (targeted; full CORE ring runs at consolidate).`
  }
  return `\`${SUITE}\` — full CORE ring (gold fidelity, no-unparsed, round-trip, DiscriminatorUniqueness, PortWalkExhaustiveness) PLUS \`nx run magic-ast:lint-discriminators\` + advance baseline.`
}

// ── WAVE PACKING (graph-color the conflict graph into disjoint-touch-set waves) ──
function packWaves(tasks) {
  const remaining = [...tasks]
  const waves = []
  while (remaining.length) {
    const wave = []
    const claimed = new Set()
    let interactionInWave = false
    for (let i = 0; i < remaining.length && wave.length < WAVE_WIDTH; i++) {
      const t = remaining[i]
      if (t.cls === 'interaction' && interactionInWave) continue
      if ((t.touch || []).some((f) => claimed.has(f))) continue
      wave.push(t)
      ;(t.touch || []).forEach((f) => claimed.add(f))
      if (t.cls === 'interaction') interactionInWave = true
    }
    if (!wave.length) break
    waves.push(wave)
    wave.forEach((t) => remaining.splice(remaining.indexOf(t), 1))
  }
  return waves
}

// ── ONE WAVE: fan out workers → delta-judge → serial merge ──────────────────────
async function runWave(wave, waveIdx, waveBase) {
  log(`Wave ${waveIdx}: ${wave.length} workers (${wave.filter((t) => t.cls === 'new-file').length} new-file, ${wave.filter((t) => t.cls !== 'new-file').length} shared/interaction)`)

  // (1) FAN OUT — workers in parallel, worktree-isolated, per-task model.
  const builds = await parallel(
    wave.map((t) => () => {
      const workerPrompt =
        `${WORKER_PRELUDE(t.branch, waveBase)}\n\nTASK ${t.slug} — ${t.mechanic}\n\nBRIEF:\n${t.workerBrief}\n\n` +
        `FIXTURE PATH: ${t.fixturePath}\n\nGOLD INPUT (copy VERBATIM into the fixture "Input"):\n\`\`\`json\n${JSON.stringify(t.input, null, 2)}\n\`\`\``
      return agent(workerPrompt, {
        label: `build:${t.slug}`,
        phase: `Wave`,
        isolation: 'worktree',
        model: t.model || 'sonnet',
        agentType: 'mast-worker',
        schema: BUILD,
      }).then((b) => ({ task: t, build: b }))
    })
  )

  // (2) DELTA-JUDGE — per gold, only green branches (Opus).
  const passed = []
  const deferred = []
  for (const { task, build } of builds) {
    if (!build || !build.green || build.blocked) {
      log(`DEFER ${task.slug} (build): ${build ? build.blockReason || build.notes : 'worker died'}`)
      deferred.push({ slug: task.slug, status: 'deferred-build', detail: build && (build.blockReason || build.notes) })
      continue
    }
    const stray = (build.filesChanged || []).filter((f) => HOT_FILES.includes(f) && !(task.touch || []).includes(f))
    if (stray.length) log(`NOTE ${task.slug} touched undeclared hot file(s): ${stray.join(', ')}`)
    const golds = (build.regeneratedGolds && build.regeneratedGolds.length ? build.regeneratedGolds : [task.fixturePath]).filter(Boolean)
    const judgeType = task.cls === 'interaction' ? 'interaction-judge' : 'mast-judge'
    const verdicts = (
      await parallel(
        golds.map((g) => () =>
          agent(DELTA_JUDGE(task, g, waveBase), { label: `judge:${task.slug}`, phase: `Wave`, model: 'opus', agentType: judgeType, schema: VERDICT })
        )
      )
    ).filter(Boolean)
    const fails = verdicts.filter((v) => v.verdict !== 'PASS')
    if (fails.length) {
      log(`DEFER ${task.slug} (judge FAIL): ${fails.map((f) => f.rationale).join(' | ')}`)
      deferred.push({ slug: task.slug, status: 'deferred-judge', fails })
      continue
    }
    passed.push({ task, build, verdicts })
  }

  // (3) SERIAL MERGE — one at a time, file-affinity order, rebuild+gate between each.
  const order = { 'new-file': 0, 'shared-edit': 1, interaction: 2, entangled: 3 }
  passed.sort((a, b) => (order[a.task.cls] ?? 1) - (order[b.task.cls] ?? 1))
  const committed = []
  for (const { task, build, verdicts } of passed) {
    const note = `${task.cls === 'interaction' ? 'interaction-judge' : 'mast-judge'} PASSED ${verdicts.length} gold(s).`
    const merge = await agent(MERGE_AGENT(task, gateCmdFor(task), note, waveBase), { label: `merge:${task.slug}`, phase: `Wave`, model: 'opus', agentType: 'general-purpose', schema: MERGE })
    if (merge && merge.status === 'merged') {
      log(`MERGED ${task.slug}`)
      committed.push({ slug: task.slug, build, merge })
    } else {
      log(`DEFER ${task.slug} (merge rolled back): ${merge && (merge.redReason || merge.summary)}`)
      deferred.push({ slug: task.slug, status: 'deferred-merge', detail: merge && (merge.redReason || merge.summary) })
    }
  }
  return { committed, deferred }
}

// =============================================================================
// MAIN
// =============================================================================
if (!families.length) {
  log('args.families is empty — nothing to do.')
  return { error: 'no families in args' }
}

// ── PHASE 1: PLAN ──
phase('Plan')
log(`Batch ${BATCH}: planning ${families.length} families (Opus).`)
const plans = (
  await parallel(
    families.map((f) => () => agent(PLANNER(f), { label: `plan:${f.slug}`, phase: 'Plan', agentType: 'general-purpose', schema: PLAN }))
  )
).filter(Boolean)

// Build dispatchable tasks; carry through the seeded Input + branch name.
const famBySlug = {}
families.forEach((f) => (famBySlug[f.slug] = f))
const tasks = []
const notDispatched = []
for (const p of plans) {
  const f = famBySlug[p.slug]
  if (!p.dispatchable) {
    notDispatched.push({ slug: p.slug, reason: p.skipReason || 'planner marked non-dispatchable' })
    continue
  }
  tasks.push({
    slug: p.slug,
    rank: f ? f.rank : 0,
    cardName: p.cardName || (f && f.cardName),
    mechanic: (f && f.mechanic) || p.slug,
    cls: p.cls,
    model: p.model,
    touch: p.touch || [],
    fixturePath: p.fixturePath,
    workerBrief: `${p.workerBrief}\n\nCR CITATION(S): ${p.crCitations || '(see brief)'}\n\nAST PLAN: ${p.astPlan || '(see brief)'}`,
    input: f ? f.input : null,
    branch: `mast-tdd/${TODAY}-${p.slug}`,
  })
}
log(`Planned: ${tasks.length} dispatchable, ${notDispatched.length} skipped${notDispatched.length ? ' (' + notDispatched.map((n) => n.slug).join(', ') + ')' : ''}.`)

// ── PHASE 2: WAVE(S) ──
phase('Wave')
const waves = packWaves(tasks)
log(`Packed ${tasks.length} tasks into ${waves.length} wave(s) by disjoint touch-set.`)
const allCommitted = []
const allDeferred = []
const SHA = { type: 'object', additionalProperties: false, properties: { sha: { type: 'string' } }, required: ['sha'] }
let wi = 0
let waveBase = BASE
for (const wave of waves) {
  wi++
  // Wave 1 forks from the batch base. Wave N>1's worktrees fork from the CURRENT
  // integration HEAD (advanced by wave N-1's merges), so its isolation + immutability
  // gates must use THAT sha, not the fixed batch base — else every wave-2 worker STOPs
  // with WRONG BASE (the halam-djinn batch-4 defer). Recapture HEAD between waves.
  if (wi > 1) {
    const cap = await agent(
      `Run \`git rev-parse HEAD\` in the main checkout (integration branch) and return ONLY that 40-char sha as {sha}.`,
      { label: `capture-head:wave${wi}`, phase: 'Wave', agentType: 'general-purpose', schema: SHA }
    )
    if (cap && cap.sha) { waveBase = cap.sha.trim(); log(`Wave ${wi} base = ${waveBase.slice(0, 8)} (recaptured after wave ${wi - 1} merges).`) }
  }
  const { committed, deferred } = await runWave(wave, wi, waveBase)
  allCommitted.push(...committed.map((c) => c.slug))
  allDeferred.push(...deferred)
}

// ── PHASE 3: CONSOLIDATE ──
phase('Consolidate')
const consolidate = await agent(
  `End-of-batch ${BATCH} consolidation — NON-isolated, main checkout on the integration branch.\n` +
    `1. FULL CORE ring once: \`${SUITE}\` — 0 failed REQUIRED (catches any joint regression the targeted between-merge gates skipped). ` +
    `If RED: identify the offending merged branch (the PortWalkSentinelSnapshot diff usually localizes it), \`git revert\` it, re-run until green, report which task was backed out.\n` +
    `2. \`nx run magic-ast:glossary\` && \`git add libs/magic-ast/GLOSSARY.md && git commit --no-verify -m "chore(mast): regenerate GLOSSARY after batch ${BATCH} (${TODAY})"\` (also commit any advanced schema/discriminator-baseline).\n` +
    `3. Refresh corpus-wide parser triage — FORCE a re-parse (Flowthru's ParseCorpus cache is CODE-BLIND: it will no-op against the new parser and report STALE coverage unless the cache is cleared first): \`rm -f tests/magic-ast-tests/Data/_07_ModelOutput/Datasets/parse-records.json && nx run mast:run\`.\n` +
    `4. \`nx run bench:recall\` — HALT-report if any combo tier regressed from its pin in combo-axis-expectations.json; note the Green/Amber/missed summary.\n` +
    `5. \`nx run mast:worktree-clean\` (reap this batch's worktrees + merged mast-tdd/* branches).\n` +
    `Report: CORE ring pass/fail + count, any backed-out task, the NEW triage card-coverage % (from Data/_08_Reporting/triage-report.json GlobalMetrics.CardCoverage.Pct), and the recall Green/Amber/missed numbers.`,
  { label: `consolidate:batch${BATCH}`, phase: 'Consolidate', model: 'opus', agentType: 'general-purpose', schema: MERGE }
)

log(`Batch ${BATCH} complete: ${allCommitted.length} merged, ${allDeferred.length} deferred, ${notDispatched.length} skipped-at-plan.`)
return {
  batch: BATCH,
  merged: allCommitted,
  deferred: allDeferred,
  skippedAtPlan: notDispatched,
  consolidate,
}
