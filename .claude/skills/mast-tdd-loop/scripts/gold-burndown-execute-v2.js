// =============================================================================
// gold-burndown-execute  —  v2  (DELTA-JUDGE + PARTIAL-COMMIT)
// =============================================================================
//
// WHY THIS REVISION EXISTS
// ------------------------
// Run 1 (wf_a4a109bd-ace) committed Slices 0/1/4 but reverted-and-deferred 2/3/5.
// The cause was a MECHANISM flaw, not bad slice work:
//
//   * The judge verdict was WHOLE-GOLD purity — "is the regenerated gold fully
//     residual-free?" — and a single FAIL reverted the ENTIRE slice.
//   * Many golds carry free-text residuals that span MULTIPLE axis-slices. The
//     canonical case is the Mentor cards (HammerDropper, BargingSergeant,
//     BladeInstructor): each carries BOTH "lesser power" (Slice 3 PowerComparison)
//     AND "attacking" (Slice 5 combat-state). No single axis-slice can make such a
//     gold whole-clean — structuring "attacking" leaves "lesser power" behind and
//     vice-versa — so the whole-gold judge FAILed both slices even though it
//     EXPLICITLY confirmed each slice's own change was a correct improvement.
//
// THE TWO MECHANISM CHANGES (see plan §"Process refinement for the next run"):
//
//   1. DELTA-JUDGE. The judge no longer asks "is the whole gold clean?" It asks:
//      "did this slice structure ITS TARGET residual(s) correctly, and introduce
//      no NEW residual / no regression / no dropped/added/inverted ability?" A gold
//      that STILL carries a DIFFERENT slice's residual is a PASS — that residual is
//      out of this slice's scope and is some other slice's job. The judge's primary,
//      explicit criterion becomes the old secondary clause "no free-text leaked
//      BEYOND this slice's scope"; the whole-gold-clean implication is dropped.
//
//   2. PARTIAL-COMMIT via the existing STATELESS whitelist semantics. The free-text
//      whitelist (tests/.../whitelist-freetext.json) is keyed per (card, sink) and the
//      gate (GoldFreeTextWhitelistTests) tests SET-MEMBERSHIP of sinks, not counts:
//        - Test (1) fails a sink present-but-unlisted (no NEW debt may appear).
//        - Test (2) fails a listed (card,sink) that the gold no longer carries AT ALL.
//      Therefore a (card,sink) entry is removed IFF the card stops carrying that sink
//      ENTIRELY. A multi-instance-same-sink card (e.g. a Mentor gold with TWO
//      OtherCharacteristic instances) KEEPS its entry until its LAST instance of that
//      sink is structured. So a slice that clears ONE instance of a shared sink:
//        - commits its correct delta (suite stays green — sink still set-present, still
//          listed ⇒ neither test fires), and
//        - legitimately LEAVES the entry in place (its other instance/other-axis
//          residual remains). That is EXPECTED and FINE, not a failure.
//
// WHAT IS DELIBERATELY UNCHANGED (the parts that worked in Run 1):
//   * The serial slice loop (one slice owns MAIN at a time).
//   * Per-slice revert-on-failure — every exit path leaves a CLEAN tree.
//   * Defer-and-continue — a deferred slice never blocks the rest of the run.
//   * The halt-before-Slice-6 boundary — this harness drives Slices 0–5 only.
//
// NET EFFECT: slices land their CORRECT, JUDGE-VERIFIED per-axis deltas even on
// multi-residual golds, instead of being reverted because some OTHER axis's residual
// is still present. A fully-clean gold now emerges incrementally across the slices
// that each own one of its residuals, rather than requiring a single slice to clean
// the whole card.
// =============================================================================

export const meta = {
  name: 'gold-burndown-execute-v2',
  description:
    'Execute gold burndown Slices 0-5 serially (implement → regen → DELTA-judge → partial-commit), halt at Slice 6',
  phases: [
    { title: 'PB-4 — Bucket A counter-gate' },
    { title: 'PB-1 — aura IsEnchanted + BearUmbra' },
    { title: 'PB-5 — CandyTrail conjunction' },
    { title: 'PB-6 — DisplacerKitten' },
    { title: 'PB-3 — structured-characteristic megaslice' },
  ],
}

const POINTERS =
  'MAIN working tree, branch feat/mast-improvements — you have it to yourself this step (slices are serial). ' +
  'Parser: libs/magic-ast/ (Parsing/Parsers/{Static,Triggered,Spell,Activated}/, AST/, AST/Effects/ObjectFilter.cs, AST/Residual.cs). ' +
  'Golds: tests/magic-ast-tests/Fixtures/HandParsedCards/**.json (Output subtree = AST). ' +
  'THREE stateless whitelists (the gates): tests/magic-ast-tests/Fixtures/whitelist-freetext.json (entries {card,sink,tag,reason}), whitelist-unparsed.json (entries {card,tag,reason}), oracle-text-quarantine.json (drift) + synthetic-card-golds.json (golds naming no printed card). ' +
  'WHITELIST SEMANTICS (read carefully — this is the partial-commit contract): the free-text gate scans each gold for the SET of free-text sinks it carries (presence, not count). ' +
  'Test (1) fails a sink that is PRESENT but NOT listed (no new debt). Test (2) fails a (card,sink) entry whose gold no longer carries that sink AT ALL. ' +
  'So you remove a (card,sink) entry IFF the card stops carrying that sink ENTIRELY. If a card has TWO instances of the SAME sink (e.g. two OtherCharacteristic) and your slice structures ONLY ONE, the sink is STILL set-present ⇒ KEEP the entry (removing it would make Test (2) fail). ' +
  'Likewise a still-present residual on a DIFFERENT axis (another slice owns it) is EXPECTED and FINE — do not touch its entry. ' +
  'Regen tool: write affected gold rel-paths (under HandParsedCards, no .json) to /tmp/golds-to-regen.txt, then ' +
  'MAST_REGEN_LIST=/tmp/golds-to-regen.txt dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --filter "FullyQualifiedName~GoldRegenerationUtility" (it re-points Input from the corpus, falling back to the full oracle-cards.json bulk, and re-derives Output). ' +
  'Full plan + this slice section: libs/magic-ast/docs/gold-burndown-plan.md (see "Run 1 results" for why this is a delta-judge run). ' +
  'Build: dotnet build tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj -v q -clp:ErrorsOnly. Suite: dotnet test tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj --no-build.'

const RULES =
  'VERIFY every card fact against oracle-cards.json / live Scryfall (curl https://api.scryfall.com/cards/named?exact=...), NEVER from memory. ' +
  'For corrupt-Input golds, verify the REAL oracle text first and hand-correct Input BEFORE regen. ' +
  'Scope discipline: structure ONLY this slice\'s target axis/residual. Leaving a DIFFERENT axis\'s residual (and its whitelist entry) intact is CORRECT — another slice owns it. Do NOT chase out-of-scope residuals. ' +
  'Do NOT git commit. If you achieve a clean build AND green full suite, STOP (leave the green changes uncommitted). ' +
  'A still-present other-axis (or other-instance-same-sink) residual does NOT prevent green: the gate is the stateless whitelist, and a listed-and-still-present sink passes both checks. ' +
  'If you CANNOT get there, FULLY revert: `git checkout -- . && git clean -fd tests libs` and return green=false, blocked=true with a precise diagnosis. Never leave the tree half-changed or broken. ' +
  'Match surrounding code idiom; keep the change minimal and faithful to the slice spec.'

// -----------------------------------------------------------------------------
// SLICES — PLACEHOLDER.
// A PARALLEL AGENT is producing the gold-set-grouped slice definitions (grouping by
// gold-set rather than by single axis, so a multi-residual gold can be cleaned across
// the slices that own its residuals). DO NOT invent slice specs here — paste the
// produced array in over this placeholder. Each entry must be:
//   { n: <int>, title: '<string>', spec: '<imperative slice spec, scoped to ONE axis/gold-set>' }
// The loop below is axis/spec-agnostic; it only reads n / title / spec.
// -----------------------------------------------------------------------------
// Gold-set-grouped slices from plan §"Parser batch (revised, gold-set-grouped)". Order: single-concern/
// leaf first; PB-3 last (broadest *RuleHelpers edit; Slice 6 rebases on it). PB-2 (comparative) is MERGED
// into PB-3. Each implementer must READ its PB-N section in libs/magic-ast/docs/gold-burndown-plan.md for
// the full per-gold checklist — the spec below is the scoped summary.
const SLICES = [
  {
    n: 'PB-4',
    title: 'PB-4 — Bucket A counter-gate',
    spec:
      'Gold set (7): CSP/PutridGoblin, DKA/StranglerootGeist, DKA/UndyingEvil, INR/ButcherGhoul, INR/YoungWolf, MOR/GravelgillAxeshark, SHM/SafeholdElite. ' +
      'Route the 3 hardcoded intervening-if producers through the EXISTING ConditionParser.Parse (its regex ALREADY matches both counter texts — do not add a new arm): ' +
      'Keywords/Definitions/PersistKeyword.cs (text "it had no -1/-1 counters on it"), Keywords/Definitions/UndyingKeyword.cs (text "it had no +1/+1 counters on it"), ' +
      'and Parsing/Parsers/Spell/Rules/TargetCreatureGainsKeywordRule.cs ~L194 (the "undying" arm, for UndyingEvil\'s granted ability). ' +
      'Target AST: TriggeringObjectCounterCondition{CounterType:"-1/-1"(Persist)|"+1/+1"(Undying), Present:false}. ' +
      'Everything else (returnToBattlefield/WithCounters/UnderControl/Trigger{Dies}/IsSelf) must stay BYTE-IDENTICAL — only InterveningIf changes. UndyingEvil\'s gate is on the GainedAbility, not the spell ability. ' +
      'No new AST/schema. Remove all 7 from whitelist-freetext.json (sink OtherCondition).',
  },
  {
    n: 'PB-1',
    title: 'PB-1 — aura IsEnchanted + BearUmbra',
    spec:
      'Gold set (3): ROE/LuminousWake, M14/UnhallowedPact, ROE/BearUmbra. ' +
      'Add a flat `bool? IsEnchanted` to AST/References/ObjectFilter.cs (mirror IsSelf/IsToken; CR 303.4/702.5). Route the qualifier branch in TriggeredRuleHelpers.ParseObjectFilter (and any static path emitting Other("enchanted")) to set IsEnchanted=true instead of the residual. ' +
      'BearUmbra is a CORRUPT-INPUT re-derive — RE-POINT Input FIRST to the Scryfall text (Enchant creature / "Enchanted creature gets +2/+2 and has \\"Whenever this creature attacks, untap all lands you control.\\"" / Umbra armor (…)), THEN parse so it yields: (a) modifyPT +2/+2 on EnchantedOrEquipped; (b) a gainAbility on Target{EnchantedOrEquipped} whose GainedAbility is the TRIGGERED ability "Whenever {this creature, IsSelf} attacks -> untap Each {land, Controller:You}" (self-ref is the GRANTED ability\'s own source, NOT a separate "enchanted creature" filter); (c) keyword "Umbra armor" (NOT obsolete "Totem armor"). Reuse the GorgonsHead/GuardDuty gainAbility-on-Aura precedent (triggered GainedAbility). ' +
      'MUST land before PB-3 (both write ParseObjectFilter). Remove all 3 from whitelist-freetext.json (sink OtherCharacteristic).',
  },
  {
    n: 'PB-5',
    title: 'PB-5 — CandyTrail conjunction',
    spec:
      'Gold set (1): WOE/CandyTrail. RE-POINT Input FIRST (corrupt): TypeLine="Artifact — Food Clue", OracleText="When this artifact enters, scry 2.\\n{2}, {T}, Sacrifice this artifact: You gain 3 life and draw a card." (Scryfall-exact). Then ensure the activated-ability body "You gain 3 life and draw a card" parses as TWO structured effects — gainLife{3} + drawCards{1} — via effect-conjunction, NOT one residual and NOT dropping the gain-3-life conjunct (the explicit Run-1 failure mode). Find the "... and ..." effect splitter in the activated/spell effect pipeline; add the "gain N life and draw a card" arm if not already covered. ETB scry 2 and cost {2},{T},Sacrifice this are already-covered shapes. Zero IUnparsed. Remove WOE/CandyTrail from whitelist-unparsed.json (the unparsed node is gone).',
  },
  {
    n: 'PB-6',
    title: 'PB-6 — DisplacerKitten',
    spec:
      'Gold set (1): DisplacerKitten. Two coupled defects: (1) "noncreature spell" leaves OtherCharacteristic{"noncreature"} — structure the spell-cast trigger filter as {CardTypes:["spell"], ExcludedCardTypes:["creature"], Controller:You} (reuse the existing non-type negation path). (2) AbilityWord: "Avoidance" is NOT a CR 207.2c ability word (it is the card\'s printed italic label) — the encoding must NOT assert a false CR ability word; either drop the AbilityWord field for non-CR labels or keep it as a distinct printed/flavor label. ' +
      'Verify the shared "noncreature" producer change does not regress WAR/SpellgorgerWeird, OTJ/SlickshotShowOff, ZEN/SpellPierce (not in this set; if it cleans them too, remove their whitelist entries — bonus). Light overlap with PB-3\'s mapping — fold the noncreature producer into PB-3 if it is the same method (verify at implementation). Remove DisplacerKitten from whitelist-freetext.json (sink OtherCharacteristic).',
  },
  {
    n: 'PB-3',
    title: 'PB-3 — structured-characteristic megaslice',
    spec:
      'The consolidated ATOMIC slice owning BOTH the structured-characteristic axis AND comparative-power (PB-2 merged in), so Mentor golds (attacking + lesser-power) are fully cleaned in ONE regen. READ the PB-3 section of the plan for the full gold set + per-axis mapping + per-gold checklist (it is large). ' +
      'FIRST extract ONE shared qualifier->axis helper across SpellRuleHelpers/StaticRuleHelpers/TriggeredRuleHelpers/ActivatedRuleHelpers, then route all call sites through it. ' +
      'New Characteristic variants (AST/References/Characteristic.cs): TappedStateCharacteristic{bool Tapped} (covers tapped AND untapped, CR 110.5) and CounterCharacteristic{string CounterType, Comparison?} ("with a +1/+1 counter"); extend Characteristic.FromLabel. New ObjectFilter axis ExcludedColors (nonblack/nonblue/nonwhite); reuse Colors/CardTypes/ExcludedCardTypes/ExcludedSupertypes/IsToken/SharesColorWith and the existing CombatStateCharacteristic (attacking / attacking alone). ' +
      'MERGE comparative (PB-2): extend the Comparison record (ObjectFilter.cs) so RHS can be RelativeTo:ObjectReference.Self() + RelativeCharacteristic — make Value int? nullable; AUDIT the ~12 literal-int Comparison consumers to serialize BYTE-IDENTICALLY (RelativeTo/null Value absent via WhenWritingNull); producers MentorKeyword.cs + CantBeBlockedRule.cs emit PowerComparison{LessThan, RelativeTo:Self, RelativeCharacteristic:Power}. ' +
      'SCHEMA: add discriminator kinds "tapped","counter" to the Characteristic PolymorphicReflectionConverter; update EVERY exhaustive CharacteristicKind switch in libs/mast-interaction (no silent drop — these are filter predicates, no firability change); regenerate the SchemaExportTests snapshot + ast-schema.json. ' +
      'DELTA-SCOPE: structure your axes only; LEAVE the other/another exclusion residual on the [S6-SHARED] golds AdeptWatershaper and SarythTheVipersFang (Slice 6 owns it) and KEEP their whitelist entries. Land AFTER PB-1 and PB-6. Remove fully-cleaned golds from whitelist-freetext.json; keep S6-shared golds whitelisted.',
  },
]

const IMPL = {
  type: 'object', additionalProperties: false,
  properties: {
    green: { type: 'boolean', description: 'clean build AND full mast-tests suite green with the slice landed (an in-scope, listed-and-still-present other-axis residual is fine — it does not break green)' },
    blocked: { type: 'boolean', description: 'true if you reverted and could not complete' },
    blockReason: { type: 'string' },
    regeneratedGolds: { type: 'array', items: { type: 'string' }, description: 'rel paths under HandParsedCards (no .json) regenerated or re-pointed' },
    whitelistEntriesRemoved: { type: 'array', items: { type: 'string' }, description: 'only (card,sink) entries whose sink the gold NO LONGER carries at all — NOT entries kept because another instance/another-axis residual of that sink remains' },
    whitelistEntriesKept: { type: 'array', items: { type: 'string' }, description: 'in-scope golds you regenerated whose (card,sink) entry you intentionally KEPT because the sink is still present (another instance or another-axis residual remains) — names the partial-commit carry-over' },
    filesChanged: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string', description: 'what changed + any caveat' },
  },
  required: ['green', 'blocked', 'regeneratedGolds', 'whitelistEntriesRemoved', 'notes'],
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

const results = []
for (const s of SLICES) {
  phase(s.title)

  const impl = await agent(
    `Implement gold-burndown ${s.title}.\n\n${POINTERS}\n\nSLICE SPEC: ${s.spec}\n\n${RULES}`,
    { label: `impl:slice${s.n}`, phase: s.title, schema: IMPL }
  )

  if (!impl || !impl.green || impl.blocked) {
    // Implementer already fully reverted per its instructions (tree clean). Defer this slice and
    // CONTINUE to the next — one bad/mis-specced slice must not block the rest of the run.
    log(`DEFER Slice ${s.n} (impl): ${impl ? (impl.blockReason || impl.notes) : 'implementer died'}`)
    results.push({ slice: s.n, title: s.title, status: 'deferred-impl', detail: impl })
    continue
  }

  // DELTA-JUDGE: verify this slice's DELTA on each regenerated gold — did it structure its TARGET
  // residual(s) correctly and introduce NO new residual / regression / dropped-or-inverted ability?
  // A gold that still carries a DIFFERENT slice's residual is a PASS (out of scope). This is the
  // pivotal change from v1's whole-gold-purity verdict.
  const golds = (impl.regeneratedGolds || []).filter(Boolean)
  const verdicts = golds.length
    ? (await parallel(
        golds.map((g) => () =>
          agent(
            `DELTA-JUDGE the regenerated gold '${g}' (uncommitted in the working tree) for SLICE ${s.n} — "${s.title}".\n\n` +
              `This is a DELTA judgment, NOT a whole-gold-purity judgment. The slice's job is to structure ONE axis/residual; OTHER residuals on the same gold belong to OTHER slices and are intentionally LEFT in place.\n\n` +
              `SLICE SPEC (the target this slice was supposed to structure): ${s.spec}\n\n` +
              `Read tests/magic-ast-tests/Fixtures/HandParsedCards/${g}.json and confirm the real oracle text (oracle-cards.json / live Scryfall). PASS iff ALL hold:\n` +
              `  (a) The slice's TARGET residual(s) on this gold were structured CORRECTLY (right structured node/axis, faithful to the real card).\n` +
              `  (b) NO NEW free-text/unparsed residual was introduced BEYOND this slice's scope (this is the PRIMARY criterion).\n` +
              `  (c) NO regression: no dropped/added/inverted ability or effect; co-occurring sibling filters/effects preserved (not lost); literal/structured nodes outside this slice's axis serialize unchanged.\n\n` +
              `EXPLICITLY NOT A FAIL: the gold STILL carrying a residual on a DIFFERENT axis (e.g. an "attacking" combat-state residual when this slice structured "lesser power", or vice-versa), or a SECOND instance of this slice's own sink that the slice could not reach. Those are some other slice's debt and are expected; they MUST NOT cause a FAIL. Judge ONLY the change this slice made.\n\n` +
              `Strict PASS/FAIL with a one-line rationale that names what the slice structured and which (if any) out-of-scope residual remains.`,
            { label: `judge:${g}`, phase: s.title, schema: VERDICT, agentType: 'mast-judge' }
          )
        )
      )).filter(Boolean)
    : []

  const fails = verdicts.filter((v) => v.verdict !== 'PASS')
  if (fails.length) {
    await agent(
      `Slice ${s.n} failed DELTA judging on: ${fails.map((f) => f.gold).join(', ')}. FULLY revert the uncommitted working-tree changes: run \`git checkout -- . && git clean -fd tests libs\`, then confirm \`git status --short\` is clean. Return a one-line confirmation.`,
      { label: `revert:slice${s.n}`, phase: s.title }
    )
    // Reverted to a clean tree. Defer this slice and CONTINUE — a delta-FAIL means the slice's OWN
    // change was wrong (e.g. a dropped sibling effect, a mis-structured axis, or genuinely-new
    // residual), which is real parser work, not a run-stopper. Note: under the delta-judge a remaining
    // OTHER-axis residual no longer produces these FAILs — only a true defect in this slice's change does.
    log(`DEFER Slice ${s.n} (judge): DELTA-FAIL on ${fails.map((f) => f.gold).join(', ')} — reverted`)
    results.push({ slice: s.n, title: s.title, status: 'deferred-judge', fails, verdicts })
    continue
  }

  // PARTIAL-COMMIT: the slice's per-axis delta is green and delta-judge-PASSED on every regenerated
  // gold. Commit it EVEN IF some golds still carry another slice's residual (their whitelist entry
  // legitimately persists). The commit body records both removed AND intentionally-kept entries so the
  // partial progress is auditable.
  const removed = (impl.whitelistEntriesRemoved || [])
  const kept = (impl.whitelistEntriesKept || [])
  const commit = await agent(
    `Slice ${s.n} (${s.title}) is green and DELTA-judge-PASSED. Stage and commit it: \`git add -A tests libs\` then \`git commit --no-verify\` with subject "fix(golds): burndown slice ${s.n} — ${s.title}" and a body that lists: ` +
      `(1) regenerated golds: ${golds.join(', ')}; ` +
      `(2) whitelist entries REMOVED (sink fully cleared): ${removed.join(', ') || '(none)'}; ` +
      `(3) whitelist entries intentionally KEPT (another instance / other-axis residual still present — partial-commit carry-over): ${kept.join(', ') || '(none)'}. ` +
      `Do NOT push. Return the resulting short commit sha and the one-line subject.`,
    { label: `commit:slice${s.n}`, phase: s.title }
  )

  log(`Slice ${s.n} committed (${golds.length} golds, ${verdicts.length} delta-PASS; ${removed.length} entries removed, ${kept.length} kept)`)
  results.push({ slice: s.n, title: s.title, status: 'committed', golds, verdicts, removed, kept, commit, impl })
}

const committed = results.filter((r) => r.status === 'committed')
const deferred = results.filter((r) => r.status !== 'committed')
log(`Run complete: ${committed.length} committed, ${deferred.length} deferred (of ${results.length}). Halting before Slice 6 per boundary.`)
return {
  slicesAttempted: results.length,
  committedSlices: committed.map((r) => r.slice),
  deferredSlices: deferred.map((r) => ({ slice: r.slice, status: r.status })),
  results,
}
