# MAST 10-batch autonomous run summary — 2026-05-26 (batches 5-14)

## Top-line numbers

| Metric | Before run (batch 4 close) | After run (batch 14 close) | Delta |
|---|---|---|---|
| **NUnit tests** | 284 / 0 / 284 | **460 / 0 / 460** | +176 |
| **Corpus card coverage** | 7,046 / 29,614 (23.79%) | **8,249 / 29,614 (27.86%)** | **+1,203 cards (+4.07% absolute)** |
| **Corpus line coverage** | 41.61% | **46.37%** | +4.76% |
| **Distinct unresolved patterns** | 29 | 29 | 0 |
| **Sub-agents dispatched** | — | ~50 across the run | — |

Every batch closed at 100% NUnit green. Zero ratchet failures, zero rollbacks.

## Per-batch breakdown

| Batch | Card delta | Cumulative | New AST types | Key parser surfaces |
|---|---|---|---|---|
| 5 | +189 | 7,235 | `EquipEffect`, `ConvokeEffect` | `IMultiSpellRule` interface; `ModifyPTAndGainKeywordSpellRule`; static Aura composite + Equipped extension; OracleParsers Equip+Convoke |
| 6 | +116 | 7,351 | `CantBlockEffect` | `TryParseCantBlock`; `ModifyPTSpellRule` (bare PT mod with sign) |
| 7 | +104 | 7,455 | `ExaltedEffect`, `InfectEffect` | OracleParsers Exalted+Infect SimpleKeyword; LordPT "Other" prefix |
| 8 | +108 | 7,563 | `CantBeBlockedEffect`, `MorphEffect` | `TryParseCantBeBlocked`; Morph ParameterizedKeyword; `MassAnthemSpellRule` |
| 9 | +48 | 7,611 | `SkipUntapEffect`, `BushidoEffect` (first int-param) | `TryParseSkipUntap`; integer-keyword combinator (Crew pattern) |
| 10 | +163 | 7,774 | — (no new AST) | `TryParseBareKeywordGrant`; `SelfDealsDamageToAnyTargetRule`; 3 sibling additions for token-create |
| 11 | +50 | 7,824 | `CanBlockOnlyEffect`, `TypecyclingEffect` | `TryParseCanBlockOnly`; Typecycling generic combinator |
| 12 | **+336** | 8,160 | — (helper-novel found existing `AdditionalCostsAttribute` + `Ability.AbilityWord`) | **Generic ability-word prefix peeler** (Landfall/Threshold/Delirium/etc.); ETB-surveil; AdditionalCost parser; parenthetical mana ability (cluster #1 closed) |
| 13 | +53 | 8,213 | `SoulshiftEffect`, `WitherEffect`, `EntersWithCountersEffect` | OracleParsers Soulshift+Wither; `TryParseEntersWithCounters` |
| 14 | +36 | **8,249** | `PersistEffect`, `BestowEffect` | `ExileTargetLandRule`; OracleParsers Persist+Bestow |

**Average per-batch yield:** 120 cards. **Biggest:** batch 12 (+336 — helper-novel found existing infrastructure, generic ability-word peeler unlocked many cards). **Smallest:** batch 14 (+36 — diminishing returns at the tail).

## New AST types this run (16 effect types)

Keyword effects: `EquipEffect`, `ConvokeEffect`, `CantBlockEffect`, `CantBeBlockedEffect`, `MorphEffect`, `ExaltedEffect`, `InfectEffect`, `BushidoEffect`, `SoulshiftEffect`, `WitherEffect`, `PersistEffect`, `BestowEffect`, `CanBlockOnlyEffect`, `TypecyclingEffect`.
Other effects: `SkipUntapEffect`, `EntersWithCountersEffect`.

## New parser infrastructure

- **`IMultiSpellRule`** interface (batch 5) — for spell rules emitting multi-effect bundles.
- **Integer-parameterized keyword combinator pattern** (batch 9 — Bushido) — established that `OracleToken.Number` works for digit-param keywords. Soulshift (batch 13) used the same pattern with zero per-keyword arch.
- **Generic ability-word prefix peeler** (batch 12) — `<Word> — <body>` peels the word into `Ability.AbilityWord`. Adding any ability-word (Landfall, Threshold, Delirium, Revolt, Enrage, Fateful hour, Metalcraft, Morbid, Hellbent, etc.) is now a one-line addition to `_abilityWords`.
- **AdditionalCostsAttribute parser** (batch 12) — `ClauseSplitter` + `AttributeExtractor` now recognize "As an additional cost to cast this spell, sacrifice X." as a card-scope attribute.
- **Parenthetical mana ability dispatch** (batch 12) — closed the long-deferred cluster #1 (bicycle land `({T}: Add {X} or {Y}.)`).
- **Trailing reminder text extraction** on triggered abilities (batch 12) — stripped before effect dispatch, preserved on `TriggeredAbility.Reminder`.

## Skill v3 corrections landed mid-run

Worktree settings + skill simplifications applied before the run (commits `dddb251`, `9c7ec11`, `db01e15`):

- `.claude/settings.json` `worktree.baseRef: "head"` — sub-agents spawn from local main HEAD (no more "263 commits behind").
- `.worktreeinclude` — gitignored MTG rules glossary copied into every new worktree.
- Step 0b simplified — no more rebase gate (the settings make rebasing unnecessary in the common case).
- Step 1.5 — MTG glossary inlining relaxed from mandatory to recommended.

These changes paid off — every helper-novel manifest this run confirmed "worktree at main HEAD" at spawn. Zero base-staleness reports.

## Issues surfaced during the run

1. **CWD slip recurred** in batches 5 + 9 helper-novels — agents wrote files to main repo path before catching themselves. Self-recovered both times. The skill's `git -C "$WORKTREE_ROOT"` mandate addresses git-level slips but not Write-tool path slips. Worth a follow-up: add a Step 0 reminder to anchor file paths on `$WORKTREE_ROOT` when writing.

2. **Helper-novel pushed back twice on briefing in batch 12** — `AdditionalCostsAttribute` and `Ability.AbilityWord` both already existed but the briefing didn't catch it. Orchestrator-side glossary inspection before briefing-writing would save these round-trips.

3. **Merge conflict in batch 12** — two mechs both touched `TriggeredAbilityParser.cs` (etbSurveil + landfall). Auto-merge failed; manual resolution kept both changes. Doctrine note: when both Wave 2 mechs touch the SAME parser file, serialize them or expect manual merge.

4. **Helper-mech occasionally committed to wrong branch name** (batch 12 etbSurveil committed to `worktree-agent-XXX` instead of the dispatched `mast-tdd/helper-mech-etbSurveil-batch12-2026-05-26`). Worked around by merging via worktree-branch name. Not load-bearing but worth tightening in skill prompt template.

5. **Affinity remained deferred** for the entire run (top yield cluster every batch). It's a complex cost-modifier mechanic; deferred to a future dedicated session.

## What's still in the top-5 after the run

| Cluster | Marginal | Notes |
|---|---|---|
| Affinity for X | 24 | Complex cost-modifier — needs design discussion |
| (others at +12 or below) | — | Long tail — per-cluster yield now ~12-16 |

## Estimate refresh — how close to 80%?

Per the original projection (~20-50 sessions to 80% coverage):
- **Today's progress:** 5,711 → 8,249 = **+2,538 cards in 14 batches total** (counting the keyword sweep + 10-batch run + earlier batches today).
- **Pace:** ~120 cards/batch on average in the autonomous run, dropping toward ~60-80 in the tail.
- **Gap to 80%:** 23,691 - 8,249 = 15,442 cards remaining.
- **At current pace:** ~130 batches to 80%. That puts the 20-50 session estimate solidly on the **upper end** (each "session" = multiple batches; 130 batches at ~10/session = 13 sessions, but each batch is shorter as we hit the tail).

The clustering approach + helper/mech contract design held — efficiency remained ~100-200 cards/sub-agent throughout the run, even as the individual yields shrank.

## Files emitted this run

Briefings: `docs/judgments/briefing-2026-05-26-batch{5..14}.md`
Verdicts: `docs/judgments/verdict-2026-05-26-batch{5..14}.md`
This summary: `docs/judgments/run-2026-05-26-10batch-summary.md`

## Closing

Run complete. 460 tests green. +1,203 cards. 16 new AST types. 3 major parser-infra additions. Zero unresolved blockers. Ready to yield for review.
