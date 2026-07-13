# MAST judge — batch verdict (batch5-ward)

**Date:** 2026-07-07
**Branch:** `mast-tdd/2026-07-07-ward-nonmana-cost` (base `535fc7f`)
**Scope:** 3 surfaces (2 AST/parser rules, 1 fixture) + 1 projection check
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/WardKeywordRule.cs` — PASS. New `DiscardCostPattern`
  branch models a Ward non-mana cost as a `DiscardCost` per **CR 702.21a** ("Ward is a triggered
  ability. Ward [cost] means 'Whenever this permanent becomes the target of a spell or ability an
  opponent controls, counter that spell or ability unless that player pays [cost].'"). The doc-comment
  quotes 702.21a verbatim; the rule exists in `rules-structure.json` and glossary `Ward` → 702.21.
  **Sibling-mislabel check:** the regex is anchored `^\s*Ward—(?<disccost>Discard\s+.+?)[.\s]*...$`
  and requires the literal `Ward—Discard` after the em-dash. It is evaluated *after* the mana
  (`Ward {N}`), life (`Ward—Pay N life`), and sacrifice (`Ward—Sacrifice a …`) branches, so no
  mana-cost or other em-dash cost sibling can reach it, and it cannot substring-match a more-specific
  cost line. No mislabel.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/PutCountersTriggeredRule.cs` — PASS. New
  `"target creature an opponent controls"` branch emits `Controller = Opponent` on the counter target
  (**CR 109.5** controller filter). Ordering is correct: it precedes the bare `"target creature"`
  fallthrough (more-specific-first) and is lexically disjoint from the `"target creature you control"`
  sibling (does not contain "you control"), so neither is mislabeled. This is the line-3 lossy fix —
  the opponent-controller restriction that was previously silently dropped is now captured.

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DSK/SunsetSaboteur.json` — PASS. All three abilities
  faithful:
  - **Menace** (`static`, `KeywordSource: Menace`) → `evasion` effect with `CanBeBlockedBy.CardTypes:
    ["creature"]` and `MinimumBlockers: 2` — **CR 702.111a/b** ("Menace is an evasion ability"; "can't
    be blocked except by two or more creatures").
  - **Ward—Discard a card** (`triggered`, `KeywordSource: Ward`) → `Trigger{Whenever, BecomesTarget,
    Filter.Controller: Opponent}` + `PreventableEffect{Inner: counterSpell(It), Unless{Player:
    ThatPlayer, Cost: discard 1 card}}` — a structurally exact reading of **CR 702.21a**.
  - **Attack trigger** → `Trigger{Whenever, Attacks, IsSelf}` + `putCounters +1/+1 count 1` onto
    `Target{creature, Controller: Opponent}` — the opponent-controller restriction is present (line-3
    faithfulness confirmed).
  No `unparsed` / `UnparsedEffect` / `Diagnostics` nodes; the only `Raw`/`RawText` fields are
  verbatim-by-design (type line, oracle text, mana cost, P/T) and carry no parsed semantics.

- `mast-tdd/2026-07-07-ward-nonmana-cost#projection` — PASS. No new discriminator is introduced. The
  Ward branch reuses the pre-existing `discard` CostType (`[OracleCost("discard")]` in `Cost.cs`,
  `ParseDiscardPattern` in `ActivatedRuleHelpers.cs`, both present at base) and the counter branch
  reuses `ControllerFilter.Opponent`. The exhaustiveness ratchet fires only on new discriminators, so
  no `PortGraph`/`PortWalkProjection` entry or `known-coarse-projections.json` justification is owed.

## Glossary gaps

None. `Ward` (→ 702.21) and `Menace` (→ 702.111) are both in `glossary.json`.

## Process notes

- `describe-vs-execute`: `CounterSpellEffect` (discriminator `counterSpell`) targeting `It` is used to
  model "counter that spell or ability". CR 702.21a counters a spell *or ability*; the node name reads
  spell-centric but this is the established Ward shape (pre-existing, shared with the mana/life/sacrifice
  cost forms) and is descriptive, not executive — not introduced or altered by this branch. Out of scope
  to relitigate here.
- Both edits are shared-parser-body additions; parser-output correctness (that `ParseDiscardPattern`
  yields `{CardTypes:["card"], Quantity: literal 1}`) is NUnit's gate, not the judge's.
