# MAST batch 5 verdict (autonomous run 1/10)

**Result:** PASS. NUnit 316/0/316. Corpus 7,046 → **7,235 cards** (+189, +0.64% absolute). Lines 41.61% → 43.13%.

## Families landed

| Family | Helper | Mech | Cards green |
|---|---|---|---|
| A — Spell composite (`Target creature gets +N/+M and gains KW until end of turn`) | 5 fixtures green (Sonnet) | 5/5 via new `ModifyPTAndGainKeywordSpellRule` + new `IMultiSpellRule` interface | 5 |
| B — Aura composite (`Enchanted creature gets +N/+M and has KW`) | 3 fixtures green (Sonnet) | 3/3 via `TryParseEnchantedPTAndKeyword` in StaticAbilityParser | 3 |
| C — Equip + Convoke keywords | 8 fixtures green (Opus helper-novel) — 2 new AST types (`EquipEffect`, `ConvokeEffect`) | 8/8 via OracleParsers SimpleKeyword + ParameterizedKeyword extensions + StaticAbilityParser Equipped extension + small PutCountersTriggeredRule sibling | 8 |

**Sub-agents:** 3 helpers (1 Opus + 2 Sonnet) parallel, 3 mechs (Sonnet) parallel. 6 total.

## New architecture

- **`IMultiSpellRule` interface** — for spell rules that emit multiple effects from a single recognizer (e.g., modifyPT + gainAbility for the spell composite shape). Co-exists with `ISpellRule`; `SpellAbilityParser.RuleEntry` now optionally carries an `IMultiSpellRule`. Borderline-architectural but bounded — single interface, single dispatch path.

## Sibling-shape additions (per skill allowance — all single-shape)

- Family A's mech: `AbilityClassifier` routing rule for `Target … gets +N/+M` clauses to `AbilityKind.Spell` (previously fell to static default).
- Family C's mech:
  - `StaticAbilityParser` extension: `Enchanted` → `(?:Enchanted|Equipped)` for "X creature gets +N/+M" recognition.
  - Sign generalization to handle `+N/-M` (BarbedBattlegear's `+4/-1`).
  - `PutCountersTriggeredRule` extension: "you may" optional + "another target" Characteristics for LivingTotem.

## Top-5 yield clusters now (post-batch)

The shape has rotated again — keyword-effect tail done, composite buffs done, now restrictions and bare PT mods surface.

| Rank | Template | Marginal | Note |
|---|---|---|---|
| 1 | `<SUBTYPE> <TYPE> can't block.` | 34 | Static restriction — was deferred from batch 5 (StaticAbilityParser conflict avoided) |
| 2 | `<SUBTYPE> <TYPE> gets +<N>/+<N> until end of turn.` | 31 | Spell-side bare PT buff |
| 3 | `<SUBTYPE> <TYPE> gets -<N>/-<N> until end of turn.` | 31 | Spell-side bare PT debuff |
| 4 | `<SUBTYPE> <SUBTYPE> you control get +<N>/+<N>.` | 31 | LordPT variant (two subtypes — likely tribal subtype + creature) |
| 5 | `(<COST>: <SUBTYPE> <COST> or <COST>.)` | 30 | Looks like alt-cost reminder text (e.g. phyrexian mana, Spree) |

Cumulative through top-5: 157 marginal cards.

## CWD-slip note (helper-novel)

Helper-novel briefly wrote files into main repo's working tree before catching itself and copying into the worktree. Self-recovered cleanly. The `worktree.baseRef:head` setting fixed base-staleness; CWD discipline is the next layer of agent hygiene. Not a batch-blocking issue; flagged for awareness.

## Closing

Batch 5 lands clean. 189 cards, 2 new AST types, 1 new parser interface, 1 new Spell rule, several sibling additions. **9 batches remaining in the autonomous run.**
