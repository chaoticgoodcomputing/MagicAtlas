# MAST 10-batch autonomous run — closing summary

**Window:** batches 4 → 13 (post-batch-3 doctrine adoption through final batch).
**Date:** 2026-05-25.
**Directive:** "10 batches of 5 before yielding back for review, unless something becomes severely misaligned."

## Top-line numbers

| Metric                           | Run start (batch 3 close) | Run end (batch 13 close) | Delta    |
|----------------------------------|---------------------------|---------------------------|----------|
| NUnit tests                      | 28 / 0 / 28               | 156 / 0 / 156             | +128     |
| Corpus card coverage (passing)   | ~5,434                    | 5,711                     | +277     |
| Corpus line coverage (passing)   | n/a (not snapshot)        | 20,515 / 56,908 (36.05%)  | —        |
| Distinct unresolved patterns     | n/a                       | 13                        | —        |
| Commits on `main`                | —                         | 135 over the run          | —        |
| Fixtures under `HandParsedCards/`| ~25                       | 69 across 42 set dirs     | +44      |

NUnit stayed at 100% green at every batch close (no ratchet tolerance was used after batch 4).

## New AST types added during the run

5 new top-level effect types, all attribute-registered, zero edits to base classes:
- `MustBlockEffect` — Rule 509.1c (combat assignment constraint), batch 4.
- `LegendRuleSuppressionEffect` — Rule 704.5j (state-based action exemption), batch 5.
- `DrawCardEvent` — Rule 614 (replacement effect targeting draw), batch 8.
- `CantBeCastEffect` — Rule 601.5 (cast restriction), batch 10.
- `SuspendEffect` / `CantBeCounteredEffect` — keyword/timing additions during late batches.

Plus the one quietly load-bearing field change:
- `ObjectReference.Quantity` (batch 12) — cardinality on the reference itself, distinct from `Filter`. Unlocked "up to N target", "N target", "any number of target" without rebuilding the Target plumbing. Reused by Amazing Acrobatics' "one or two target creatures" in batch 13.

`ObjectFilter` also picked up `IsMulticolored` / `IsMonocolored` (Rule 105.x color predicates) plus the `Characteristics` free-text escape hatch and `History` predicate union.

## Parser surface growth

Diff vs `df4a86e` (batch-3 closure):
- `libs/magic-ast/Parsing/` — **8 files changed, +4,611 / −121 lines.**
- `libs/magic-ast/AST/` — 8 files changed, +286 / −24 lines.

Heaviest contributors: `SpellAbilityParser.cs` (most-touched file in the run; near-every batch added at least one new spell shape), `StaticAbilityParser.cs` (granted-ability subject forms — "Enchanted/Equipped", "All [Subtype]s", "[CardType]s you control", "[Color] [CardType]s you control", "[Subtype]s you control"), `TriggeredAbilityParser.cs` (self-by-type lists + new trigger conditions), `ActivatedAbilityParser.cs` (multi-sentence dispatch + UnparsedEffect fallback when cost parses but effect doesn't).

No edits to infrastructure (`OracleParser.cs`, `AbilityParserRegistry.cs`, `PolymorphicReflectionConverter.cs`) — the restructure goal of "agents never touch shared machinery" held across all 10 batches.

## Doctrinal changes captured during the run

Three durable doctrines emerged that are now memory-backed:

1. **Multi-effect-per-clause** (`feedback_mast_multi_effect_per_clause`). When an oracle line contains multiple sentences (`". [A-Z]"` boundaries within a single `\n`-bounded clause), bundle those effects into one ability with multiple `Effects[]`. The corpus has ~10,691 such lines — the per-clause-one-ability convention is per *line*, not per *sentence*. First implemented in parser at batch 12 (Heritage Reclamation's third modal option).
2. **Colorless = empty arrays, always emitted** (per session-level user instruction). `AttributeExtractor` always emits `Colors: []` / `ColorIdentity: []` for colorless cards; fixtures mirror present-with-empty. Caused a brief regression at batch 6 when "always emit" wasn't yet uniform.
3. **Color ordering**: parser emits ColorIdentity in WUBRG; Colors in input order (alphabetical from Scryfall). Mixing these caused the Niambi regression in batch 6.

## Notable incidents

- **Mental Modulation gold-AST decision** (batch 4) — helper bundled tap+draw into one SpellAbility; orchestrator split to per-clause-one-ability before user surfaced the multi-sentence-single-line corpus pattern that retroactively validated the helper's instinct. Triggered doctrine (1) above.
- **Mech sub-agent killed mid-investigation** (batch 4, Mental Modulation) — no commits, no work; recovered in-process, ~10 min lost.
- **Worktree base-staleness recurred every batch** despite Step 0 rebase instructions. Sibling stash bleed-through observed multiple times; sub-agents self-recovered.
- **CWD slips to main repo** — batch 5 (Telekinetic Sliver) and batch 6 (Gravkill) sub-agents committed work to `main` directly instead of their feature branches. Both recoverable as no-op merges, but the friction is real and worth a separate skill follow-up.
- **Adjacent-method merge conflicts on `SpellAbilityParser.cs`** — recurring; manual resolution each time by keeping both methods sequentially. Mechanical fix, but a smell worth tracking.

## Triage state at run end

Top 5 remaining gaps by frequency:

| Rank | Pattern              | Lines  | Cards  |
|------|----------------------|--------|--------|
| 1    | AmbiguousStructure   | 12,201 | 10,121 |
| 2    | UnparsedTriggered    |  8,165 |  7,476 |
| 3    | UnparsedSpell        |  4,087 |  3,693 |
| 4    | ComplexTargeting     |  3,636 |  3,517 |
| 5    | ConditionalEffect    |  3,084 |  2,969 |

Note: `AmbiguousStructure` is a generic fallback bucket — it's the next obvious candidate for the "refine the pattern bucket" step in the TDD-loop skill. Sub-pattern splitting is now likely worth more than another mechanical batch.

## Recommendations for the next run

1. **Refine `AmbiguousStructure`** in `FallbackParser.InferFailurePattern` before any more mechanical batches. 10k+ cards in one coarse bucket means the cleanliness-score ranking inside it is mostly noise.
2. **Look at `UnparsedTriggered`** next. The corpus is heavy on triggered abilities; the existing parser handles core patterns but the remaining 7.4k cards likely cluster around a small number of structural variants (replacement triggers, intervening-if clauses, multi-event triggers).
3. **Solve the CWD-slip + stale-base recurrence** at the sub-agent prompt template level. Both happened despite explicit Step 0 instructions; the prompt isn't enough. Either a worktree-isolation wrapper or a hard pre-flight gate that blocks first commit if `pwd` doesn't include `.claude/worktrees/`.
4. **Adjacent-method merges** could be mitigated by either splitting `SpellAbilityParser.cs` into rule-per-file (the original restructure plan but for parser bodies, not just AST types) or by making batches more carefully not co-modify the same parser file. The first is structural; the second is brittle.

## Closing

Run lands clean. NUnit 156/0/156, +277 cards, 5 new attribute-registered effect types, three new doctrines captured to memory, zero infrastructure file edits. Yielding back for review per the original directive.
