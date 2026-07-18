# MAST judge — batch verdict

**Date:** 2026-07-18
**Scope:** 4 files (2 fixtures, 2 rule/.cs parser files) — branch `mast-tdd/2026-07-18-stragglers`, base `79b8afe3f8c31b92e7bde8e4a11c157b5ff6480e`
**Result:** PASS

## Summary

- PASS: 5 (2 .cs rule files, 2 gold fixtures, 1 projection-decision item)
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/EnchantedPTAndGrantedAbilityRule.cs` — PASS. Applies the identical `clause.RawText.IndexOf(body, StringComparison.Ordinal)` → `clause.SourceSpan.Start + offset` rebase already used in `GrantedAbilityRule.cs` (diffed side-by-side: same variable names `bodyOffsetInClause`/`bodyAbsoluteStart`, same `>= 0 ? offset : 0` guard against a failed IndexOf). No logic errors.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/SubtypeCreaturesHaveQuotedAbilityRule.cs` — PASS. Same convention, applied correctly; matches `EquippedCreatureHasQuotedAbilityRule.cs`'s equivalent (`Math.Max(bodyOffsetInClause, 0)`) functionally.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ManaweftSliver.json` — PASS. See fixture findings below.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/ROE/BearUmbra.json` — PASS. See fixture findings below.
- `mast-tdd/2026-07-18-stragglers#projection` — PASS. No new discriminator introduced; this branch is a pure span-provenance fix on pre-existing rule files (`ObjectReferenceKind.EnchantedOrEquipped`, `gainAbility`/`modifyPT` effect types were already in the AST before this branch). No PortWalk projection decision applies.

## Fixture findings (item 3)

**ManaweftSliver.json** — Oracle text: `Sliver creatures you control have "{T}: Add one mana of any color."`
- The quoted body `{T}: Add one mana of any color.` (31 chars) genuinely starts at absolute offset 35 in the oracle string (confirmed via `str.index(body)`); the outer static ability's `clause.SourceSpan.Start` is 0 (single-line card), so `bodyAbsoluteStart = 0 + 35 = 35`.
- `tap` cost span: `Start:35, Length:3` = `{T}` — correct.
- `addMana` effect span: `Start:39, Length:27` = `" Add one mana of any color."` (with the leading space the inner cost/effect splitter has always retained — a pre-existing inner-parser quirk unrelated to this fix). Confirmed exact: the pre-fix (buggy 0-based) value was `Start:4`, and `4 + 35 = 39`. The rebase arithmetic is exact, not approximately right.

**ROE/BearUmbra.json** — Oracle text (3 lines): `Enchant creature` / `Enchanted creature gets +2/+2 and has "Whenever this creature attacks, untap all lands you control."` / `Umbra armor (...)`.
- Line 2 (the granted-ability clause) starts at absolute offset 17 (`len("Enchant creature\n")`); the quoted body `Whenever this creature attacks, untap all lands you control.` starts at offset 39 within that line, so `bodyAbsoluteStart = 17 + 39 = 56`.
- Trigger span: `Start:56, Length:30` = `"Whenever this creature attacks"` (no trailing comma) — correct; pre-fix value was `Start:0` (0-based), and `0 + 56 = 56`.
- `untap` effect span: `Start:87, Length:29` = `" untap all lands you control."` (leading-space inner-parser quirk, same pattern as ManaweftSliver, pre-existing and unrelated to this fix). Pre-fix value was `Start:31`, and `31 + 56 = 87`. Exact.

Both fixtures' corrected spans are independently re-derivable from the real oracle text and match the committed values to the character.

## Regex-dual-purpose verification (item 2)

`EnchantedPTAndGrantedAbilityRule.cs`'s pattern:
```
^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+["“](?<body>[^"”]+)["”]\.?\s*$
```
confirmed to contain a genuine `(?:Enchanted|Equipped)` alternation (not just an "Enchanted"-only pattern coincidentally producing the right answer some other way). Cross-checked against Biorganic Carapace's real oracle text (`tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json`):
```
When this Equipment enters, attach it to target creature you control.
Equipped creature gets +2/+2 and has "Whenever this creature deals combat damage to a player, draw a card for each modified creature you control." (Equipment, Auras you control, and counters are modifications.)
Equip {2}
```
The second line's `Equipped creature gets +2/+2 and has "..."` literally matches the alternation's `Equipped` branch (the trailing parenthetical reminder text is stripped by `StaticRuleHelpers.StripReminderText` before the regex runs, pre-existing and not part of this diff). Biorganic Carapace genuinely routes through this rule file.

## Effect-attribution risk check (item 4)

The rule's target construction (`new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped }`, unchanged by this diff) already uses the union discriminator rather than an Enchant-specific one, so an Equipped-creature card (Biorganic Carapace) is attributed correctly, not silently coerced into an "enchanted" semantic. This code path predates the branch and was not touched — no behavior-change risk beyond the span fix.

## CR-incorrectness notes

- `EnchantedPTAndGrantedAbilityRule.cs` and `SubtypeCreaturesHaveQuotedAbilityRule.cs`'s cited rules (CR 303.4, CR 702.5, CR 611.1, CR 113.3) all exist in `rules-structure.json` and are topically consistent with the modeling (Aura enchant/grant mechanics, continuous effects granting activated abilities). None of this doc-comment text was touched by this branch — it predates it — so it is out of the delta-judge's scope, but no contradiction was found on inspection.
- One process note (non-blocking, pre-existing, out of scope for this branch): `SubtypeCreaturesHaveQuotedAbilityRule.cs`'s doc comment attributes a quoted paraphrase — `"Some effects and static abilities can grant an object an activated ability."` — to CR 113.3, but 113.3's actual text in `rules-structure.json` is the four-category ability list header (`"There are four general categories of abilities:"`), not that sentence. This is pre-existing text unmodified by this branch (confirmed via `git show` on the base commit), so it does not FAIL this delta judgment, but it's worth a follow-up citation cleanup pass.

## Glossary gaps

None found for this scope — the mechanics involved (Aura enchant, Equipment equip, tribal-anthem grant, activated/triggered ability, mana ability) are all pre-existing, already-covered concepts.

## Process notes

- This is a pure span-provenance fix; no new AST shapes, discriminators, or effect types are introduced, so scope is narrow by design (2 rule files, 2 corrected gold fixtures).
- Both corrected fixture spans were independently re-derived from raw oracle text (not merely diffed against the committed value) and matched exactly, including a pre-existing "leading space retained in the effect span" quirk in the inner cost/effect splitter that is unrelated to this fix and was faithfully preserved (only its absolute basis moved).
- The CR 113.3 citation mismatch noted above is pre-existing and out of scope for this branch's delta judgment; flagging for a future citation-cleanup pass, not blocking here.
