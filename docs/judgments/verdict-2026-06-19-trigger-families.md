# MAST judge — batch verdict

**Date:** 2026-06-19
**Scope:** 5 trigger-parsing families on `feat/mast-improvements` — 12 fixtures + 7 rule files + 1 classifier change + 2 projection decisions (21 judged targets)
**Result:** FAIL

## Summary

- PASS: 20
- FAIL: 1

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Triggered/Rules/EachPlayerSacrificesTriggeredRule.cs
**Verdict:** FAIL
**Issue:** Contradictory CR citation — cites the wrong keyword-action rule for Sacrifice.
**Rule citation:** CR 701.21 (Sacrifice); the doc-comment cites CR 701.16.
**Rule text:** > 701.21 "Sacrifice" — 701.21a "To sacrifice a permanent, its controller moves it from the battlefield directly to its owner's graveyard..."
> In `rules-structure.json`, 701.16 is "Investigate" — 701.16a "'Investigate' means 'Create a Clue token.'"
**What the AST says:** The class doc-comment repeatedly cites "(CR 701.16 — Sacrifice)" and "CR 701.16a — the affected player always chooses which permanent they sacrifice".
**Why this misrepresents the rule:** In this rules dataset, 701.16 is the Investigate keyword action, not Sacrifice. The Sacrifice keyword action is 701.21 (and the "controller moves it" / affected-player-chooses semantics the doc attributes to "701.16a" live at 701.21a). The cited number contradicts the modeled mechanic.
**Suggested fix:** Replace every "CR 701.16" / "CR 701.16a" in the doc-comment with "CR 701.21" / "CR 701.21a". (Fixtures and the AST shape are correct — only the citation is wrong.)

## PASS verdicts

- `tests/.../FleshbagMarauder.json` — PASS. ETB (Enters, IsSelf) -> sacrifice, Target EachPlayer + creature filter; "each player" scope matches oracle; "of their choice" inert (CR 701.21a).
- `tests/.../SlumReaper.json` — PASS. Identical edict shape; EachPlayer matches "each player" oracle; creature filter correct.
- `tests/.../InfernoElemental.json` — PASS. BlocksOrBecomesBlocked trigger + dealDamage Source=Self Target=ThatCreature amount 3; **IsCombat absent (noncombat, CR 120 not CR 510)**; source/target not conflated.
- `tests/.../OrneryGoblin.json` — PASS. Same self-ping, amount 1; IsCombat absent = noncombat; Self vs ThatCreature distinct.
- `libs/.../ThisCreatureDealsDamageToThatCreatureTriggeredRule.cs` — PASS. CR 109.1/120.1/120.2/510.1/603.2 all exist and match; IsCombat-null=noncombat doc correct.
- `tests/.../DragonFangs.json` — PASS. All 3 lines parse (Enchant + +1/+1&trample + trigger); MV>=6 preserved (CR 202.3); returnToBattlefield Graveyard/You with NO ExiledWith + attach; no unparsed.
- `tests/.../DragonScales.json` — PASS. +1/+2 & Vigilance match oracle; graveyard self-return (no ExiledWith) + attach; MV threshold preserved.
- `libs/.../CreatureWithManaValueEntersConditionRule.cs` — PASS. ManaValueComparison GreaterThanOrEqual on filter; CR 107.1/202.3/603.2 exist and match.
- `libs/.../ReturnSelfFromGraveyardAttachedToThatCreatureRule.cs` — PASS. returnToBattlefield Self from Graveyard/You (no ExiledWith — exile-only per CR 406.6) + attach; OptionalEffect for "you may"; cited rules exist, no contradiction.
- `tests/.../HematiteTalisman.json` — PASS. SpellCast Colors=R matches "red spell"; Controller Any; optional->conditionalPay {3} IfYouDo untap target permanent; "you may pay {3}" Instructions whitelisted irreducible.
- `tests/.../LapisLazuliTalisman.json` — PASS. Colors=U matches "blue spell"; conditionalPay->untap correct; Instructions whitelisted.
- `tests/.../MalachiteTalisman.json` — PASS. Colors=G matches "green spell"; conditionalPay->untap correct; Instructions whitelisted.
- `tests/.../OnyxTalisman.json` — PASS. Colors=B matches "black spell"; conditionalPay->untap correct; Instructions whitelisted.
- `tests/.../NacreTalisman.json` — PASS. Colors=W matches "white spell"; conditionalPay->untap correct; Instructions whitelisted.
- `libs/.../ConditionalPayTriggeredRule.cs` — PASS. Wires untap-target as IfYouDo consequent via TapUntapTargetTriggeredRule; no contradictory citation.
- `tests/.../SengirBats.json` — PASS. Dies trigger + History DealtDamageBy{Source:Self, this turn} -> putCounters +1/+1 Self; subject is any creature, "this creature" = damage Source (not dying subject); Timeframe free-text whitelisted by design.
- `libs/.../CreatureDealtDamageThisTurnDiesConditionRule.cs` — PASS. Separates dying subject (any creature) from provenance source (Self); CR 109.5/201.5/603.2/700.4 exist; priority above DiesConditionRule prevents self-ref misread.
- `libs/.../AbilityClassifier.cs#TriggerConditionMentionsThisTurn` — PASS. Declines delayed-trigger when an event verb follows the last "this turn" (provenance qualifier => printed trigger, CR 603.2 not 603.7). Verified non-regression: Glimpse of Nature ("...cast a creature spell this turn" — terminal) and Graceful Reprieve ("...dies this turn" — verb before "this turn") both keep delayed classification; GlimpseOfNature gold still emits createDelayedTrigger.
- `libs/mast-interaction/known-coarse-projections.json#BlocksOrBecomesBlocked` — PASS. Projection decision present (coarse) and sensible: the block-trigger event is not a walkable loop driver; the damage payoff (dealDamage) projects semantically.
- `libs/mast-interaction/PortWalkProjection.cs#trigger-family` — PASS. Item-5 payoff edges project semantically (Dies trigger, putCounters, untap, dealDamage); `dealtDamageBy` is a history predicate (filter on the Dies event), not a port discriminator, so no new projection entry is required; conditionalPay parked coarse is sensible (payoff untap projects).

## Glossary gaps

None new — all terms (sacrifice, blocks/blocked, mana value, aura/enchant, untap, dies, +1/+1 counter) are established MTG-domain vocabulary in glossary.json.

## Process notes

- **Item 1 (the FAIL) is citation-only.** The fixtures and the AST shape are fully correct; both Fleshbag Marauder and Slum Reaper say "each player" so `EachPlayer` (not `EachOpponent`) is right, and "of their choice" is correctly inert. The single defect is the doc-comment's CR 701.16 (Investigate in this dataset) where CR 701.21 (Sacrifice) is meant. The same wrong number recurs as "701.16a" for the affected-player-chooses clause (correct content, wrong number — it is 701.21a).
- **Item 2 noncombat distinction is correctly modeled.** IsCombat is omitted (≡ null = noncombat) on both self-ping golds; the doc-comment's CR 120-vs-CR 510 reasoning is accurate. The trigger fires off a block declaration but the ability's damage is not combat damage.
- **Item 3 ExiledWith discipline holds.** Both Auras return from the graveyard (Zone Graveyard, Controller You) with no `ExiledWith:Self` — that marker is exile-only (CR 406.6 linked-exile, e.g. Cloudshift), correctly absent here.
- **Minor reference observation (not a FAIL):** Item 3's `attach` target uses `ObjectReferenceKind.It` (anaphoric "previously mentioned object") rather than `ThatCreature` (the trigger-condition-named back-reference whose doc-comment is the closer match for "that creature"). Both resolve to the entering creature, so there is no semantic contradiction or wrong-object reference; `It` is also the dominant established convention in the corpus (93 fixtures vs 10 ThatCreature). Surfaced for consistency only.
- **Item 4 "you may pay {3}" / Item 5 "this turn" free-text** are both recorded in `tests/.../Fixtures/whitelist-freetext.json` (commit fd3a4ecd) as irreducible/by-design residuals; the structured `ConditionalPayEffect.Cost {3}` and `DealtDamageByPredicate.Timeframe` carry the meaning, so they are not free-text faults.
- **Item 5 classifier heuristic is narrow.** The verb-after-last-"this turn" test only fires for provenance-then-event-verb shapes (the death-watch family); SengirBats is the only existing fixture matching it. Genuine spell-created delayed triggers terminate the condition at "this turn" and are untouched.
