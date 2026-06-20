# MAST judge — batch verdict (trigger-families, round 2)

**Date:** 2026-06-19
**Branch:** feat/mast-improvements
**Scope:** 18 targets (10 fixtures, 7 AST/rule files, 1 projection decision)
**Result:** FAIL

## Summary

- PASS: 17
- FAIL: 1

All 10 gold fixtures are rules-faithful. All oracle text verified against
`tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (not memory).
The single FAIL is an incorrect CR citation in a rule .cs doc-comment (SacrificeConditionRule),
not a fixture-modeling error.

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Triggered/Rules/SacrificeConditionRule.cs
**Verdict:** FAIL
**Issue:** Cited rule's text contradicts the modeling it is attached to.
**Rule citation:** CR 109.5
**Rule text:** > "The words \"you\" and \"your\" on an object refer to the object's controller, its would-be controller (if a player is attempting to play, cast, or activate it), or its owner (if it has no controller). ..." (verified in rules-structure.json — 109.5 is solely about "you"/"your")
**What the doc-comment says:**
> `CR 109.5: a permanent named "another" excludes the source object of the ability.`
> `... "another" self-exclusion (CR 109.5) are recovered the same way as for the dies/enters families.`
**Why this misrepresents the rule:** CR 109.5 governs the meaning of "you/your" (= the controller). It says nothing about the word "another" or about a permanent excluding the source object. The doc-comment attaches CR 109.5 to the "another" self-exclusion (`ExcludeSelf:true`) semantics, where the cited rule's text does not support that claim. The codebase's own established convention (AnotherLegendaryCreatureDiesConditionRule, lines 25-27 and citation list line 40) explicitly treats "another" as **plain-language ("any object other than this source"), not backed by a specific CR rule** — so this file both mis-cites and diverges from precedent. Note: CR 109.5 IS used correctly elsewhere in the same file for the `Controller=You` overlay (lines 13/64) — only the two "another"-attached usages are wrong.
**Suggested fix:** Drop CR 109.5 from the "another" self-exclusion lines (summary line and the inline comment) and treat "another" as plain-language, matching the dies-family convention; keep CR 109.5 only on the `Controller=You` overlay, and keep CR 701.21a for the "controller can only sacrifice what they control" point. No fixture change needed — Bloodbriar/BodyDropper golds already model `ExcludeSelf:true` correctly.

## PASS verdicts

- `ViashinoPyromancer.json` — PASS. ETB -> `dealDamage` Source `It`, Amount literal 2, **IsCombat absent** (noncombat, CR 120.1 vs combat CR 510), disjunctive target union `CardTypes:[player,planeswalker]` matches printed "target player or planeswalker".
- `CinderHellion.json` — PASS. Trample keyword + ETB `dealDamage` Source `It`, literal 2, IsCombat absent, target union `[opponent,planeswalker]` matches printed "target opponent or planeswalker" (correctly "opponent", not "player").
- `BanisherPriest.json` — PASS. ETB `exile` of "target creature" `Controller:Opponent`, `Duration:untilLeavesBattlefield` (CR 603.6e / 611.1 — duration is the linkage; no separate LTB trigger, faithful for the combined-clause form).
- `CitizensArrest.json` — PASS. Same family, target union `[creature,planeswalker]` `Controller:Opponent`, enchantment self-subject; duration linkage faithful.
- `FertileGround.json` — PASS. `Enchant land` static + `TapsForMana` trigger `Filter{land,IsEnchanted}` -> `addMana Mana:"" AnyColor:true Player:Controller` ("its controller" = enchanted-land controller, CR 106.4). Any-color correct.
- `WildGrowth.json` — PASS. Same shape with `Mana:"{G}"`, `Player:Controller`. Faithful.
- `libs/magic-ast/AST/Effects/Resource/AddManaEffect.cs#Player` — PASS (NEW FIELD scrutinized). Adding `Player` as `ObjectReference?` is sound and faithful to CR 106.4 ("an effect instructs a player to add mana"); it is a structured subject mirroring the GainLife/LoseLife `Player` axis, replacing what would otherwise be a free-text "its controller" — exactly the structured-over-free-text doctrine. Doc-comment cites CR 106.4 verbatim. Nullable, omitted for the implicit-controller form.
- `ArgothianOpportunist.json` — PASS. ETB -> `createToken` of a tapped Powerstone (`Types[artifact]`, `Subtypes[Powerstone]`, `EntersTapped:true`), reminder text discarded to the `Reminder` field (CR 207.2 italic / CR 111.10 predefined token). No `AbilityText` body — acceptable: no IUnparsed, the predefined ability is verbatim in the reminder, no lost game info.
- `KoilosRoc.json` — PASS. Flash + Flying + same Powerstone ETB; reminder discarded; Flying evasion is a structured `Characteristics` (keyword Flying/Reach), not free-text.
- `Bloodbriar.json` — PASS. `Sacrifices` trigger `Filter{permanent,Controller:You,ExcludeSelf:true}` matches "whenever you sacrifice another permanent" (CR 701.21a) -> `putCounters` +1/+1 on Self.
- `BodyDropper.json` — PASS. `Sacrifices` trigger `Filter{creature,...,ExcludeSelf:true}` -> `putCounters` +1/+1 on Self; the activated menace bonus (`MinimumBlockers:2`, CR 702.111) is also faithful.
- `ItDealsDamageToTargetTypeDisjunctionRule.cs` — PASS. All cited rules (120/120.1, 510, 115.1, 115.4, 102.2, 603) exist; texts match the disjunctive-target union + noncombat `It`-source modeling.
- `ExileUntilLeavesTriggeredRule.cs` — PASS. All cited rules (611, 406, 701.13, 603.6, 603.7) exist; texts match the temporary-exile-with-duration linkage modeling.
- `TapForManaConditionRule.cs` — PASS. CR 106.12 ("tap for mana") + CR 603.2 present and matching.
- `AddAdditionalManaRule.cs` — PASS. CR 106.4 present and matching.
- `CreateTappedPredefinedTokenRule.cs` — PASS. CR 111/111.10 (predefined token) + CR 207.2 (italic no-game-function reminder) + CR 603.2 present and matching.
- `PortWalkProjection.cs` (projection decision, initiative 03) — PASS. The batch introduces **no new top-level discriminator**: it reuses existing effect-types (`dealDamage`, `exile`, `addMana`, `createToken`, `putCounters`), existing trigger events (`Enters`, `TapsForMana`, `Sacrifices` — `TapsForMana` predates this batch via Forsaken Monument/Kinnan), and existing source kind (`It`). The one new schema element, `AddManaEffect.Player`, is a refining sub-field of an effect whose `addMana` projection already maps to `emit:mana:<color>`; *who* adds the mana does not change the emit-port semantics a flow arm reads, so no new projection decision is required. Sensible.

## Glossary gaps

None surfaced this batch.

## Process notes

- **Inherited free-text (not a new-batch FAIL):** the exile-family golds carry `Duration.Object: "this creature"/"this enchantment"` as a free-text self-reference. This is INHERITED from the existing `UntilLeavesBattlefieldDuration` node the rule correctly reused, and is explicitly whitelisted in `tests/magic-ast-tests/Fixtures/whitelist-freetext.json` (commit fd3a4ecd), matching prior precedent. The item-2 scope (duration = linkage, no separate LTB trigger) is faithful. The free-text `Object` self-reference is pre-existing structural debt to be addressed where that node lives, not in this trigger-family pass.
- **Agents' "verified all CR citations" claim — spot-check result:** mostly accurate. 20+ distinct cited rules across the batch were cross-referenced against rules-structure.json; all exist and (with the one exception below) their text matches the modeling. The one defect is SacrificeConditionRule's CR 109.5-for-"another" misattribution — the rule *number* exists but its *text* is about "you/your", so the claim is unsupported. This is precisely the kind of "cited rule text contradicts the modeling" FAIL the gate exists to catch.
