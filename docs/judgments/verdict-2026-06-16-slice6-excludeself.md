# MAST judge — batch verdict (Slice 6: another -> ExcludeSelf)

**Date:** 2026-06-16
**Scope:** 23 targets (20 migrated golds + 2 out-of-scope confirmations + 1 PortWalk projection decision)
**Result:** FAIL

## Summary

- PASS: 22
- FAIL: 1

Migrated 22 golds (incl. 2 deliberately-left-untouched) from free-text `Characteristic.Other("other")` /
`Description:"another"` to structured `ObjectFilter.ExcludeSelf=true`, modeling CR 109.2/109.5 "other"/"another"
source-object self-exclusion. 19 of 20 in-scope golds are clean, faithful parses of the real card with every
co-occurring axis preserved. One gold (GoblinChieftain) regressed its anthem line to an `unparsed` node.

## FAIL verdicts

### tests/magic-ast-tests/Fixtures/HandParsedCards/M10/GoblinChieftain.json
**Verdict:** FAIL
**Issue:** The second ability "Other Goblin creatures you control get +1/+1 and have haste" is now a
`"Kind": "unparsed"` node in `Output.Oracle.Abilities`.
**Rule citation:** CR 109.2 / 109.5 (object self-exclusion); SKILL "Unprocessed nodes in gold data".
**Rule text:** > 109.2 "If a spell or ability uses a description of an object that includes a card type or
subtype ... it means a permanent of that card type or subtype on the battlefield." (the filtered set the
anthem buffs)
**What the fixture says:**
> `{ "Kind": "unparsed", "RawText": "Other Goblin creatures you control get +1/+1 and have haste.",
>   "Diagnostics": [ { "Pattern": "LordPowerToughnessBuff", "Message": "Static ability parser not yet
>   implemented" } ] }`
**Why this misrepresents the rule:** Gold fixtures encode eventual-truth — what a complete parser would emit.
An `unparsed` node is a hole in the AST and is explicitly forbidden in gold by the SKILL ("PASS ... no unparsed
nodes"). This is a *regression*: the prior gold fully modeled this line as a `modifyPT` (Other Goblins +1/+1)
PLUS a `gainAbility` (Goblins have haste). The conjunction re-point swallowed BOTH the +1/+1 anthem and the
haste-grant — and with them the very `ExcludeSelf`/"Other Goblin" concept this slice was supposed to add — into
an opaque blob. The card now carries zero `ExcludeSelf`. Parking it on whitelist-unparsed.json keeps the ratchet
green as named debt, but the gold itself is no longer a faithful, complete parse.
**Suggested fix:** Split the conjunction into two abilities and apply the ExcludeSelf migration to the anthem:
(1) static `modifyPT` Target `Each` Filter `{CardTypes:[creature], Subtypes:[Goblin], Controller:You,
ExcludeSelf:true}` Power/Toughness +1/+1, and (2) static `gainAbility` over the same `ExcludeSelf` Goblin filter
granting Haste — matching the modeling the other 19 golds use. The oracle-text re-point to authoritative Scryfall
("Other Goblin creatures...get +1/+1 and have haste") is correct and should stay; only the unparsed Output node
must be replaced with the structured pair.

## PASS verdicts

In-scope migrated golds (free-text `other` removed, `ExcludeSelf:true` added, all co-occurring axes preserved):

- `AdeptWatershaper.json` — "Other tapped creatures you control have indestructible": ExcludeSelf + tapped:true + Controller:You; gainAbility(Indestructible) intact.
- `AggressiveMammoth.json` — "Other creatures you control have trample": ExcludeSelf + Controller:You; self-Trample static + reminder + gainAbility(Trample) intact.
- `BenalishMarshal.json` — "Other creatures you control get +1/+1": ExcludeSelf + Controller:You; +1/+1 modifiers preserved.
- `C16/RavosSoultender.json` — "Other creatures you control get +1/+1": ExcludeSelf + Controller:You; flying + upkeep-return + partner untouched.
- `CHK/SachiDaughterOfSeshiro.json` — "Other Snake creatures you control get +0/+1": ExcludeSelf + Subtypes:[Snake] + Controller:You; Power 2->1 matches authoritative Scryfall errata (1/3).
- `DTK/StormwingDragon.json` — "each other Dragon creature you control": ExcludeSelf + Subtypes:[Dragon] + Controller:You; CounterType +1/+1 preserved on the turned-face-up trigger.
- `FelhidePetrifier.json` — "Other Minotaur creatures you control have deathtouch": ExcludeSelf + Subtypes:[Minotaur]; GainedAbility(Deathtouch) preserved.
- `HeraldOfDromoka.json` — "Other Warrior creatures you control have vigilance": ExcludeSelf + Subtypes:[Warrior]; GainedAbility(Vigilance) preserved.
- `LCI/RegalImperiosaur.json` — "Other Dinosaurs you control get +1/+1": ExcludeSelf + Subtypes:[Dinosaur] + Controller:You.
- `MH1/KingOfThePride.json` — "Other Cats you control get +2/+1": ExcludeSelf + tribal-lord CardTypes:[creature] + Subtypes:[Cat]; +2/+1 modifiers correct.
- `RIX/LegionLieutenant.json` — "Other Vampires you control get +1/+1": ExcludeSelf + Subtypes:[Vampire] + Controller:You.
- `RatColony.json` — "+1/+0 for each other Rat you control": ExcludeSelf:true nested in `CountOf` of the PowerModifier (comparative count), Target:Self, ToughnessModifier literal 0 — exactly models "for each OTHER".
- `SarythTheVipersFang.json` — all 3 abilities self-exclude: tapped->deathtouch & untapped->hexproof (tapped/untapped axes preserved) + activated "untap another target creature or land you control" ExcludeSelf:true (3 ExcludeSelf total).
- `UltramarinesHonourGuard.json` — "Other creatures you control get +1/+1": ExcludeSelf + Controller:You; Squad static + enters-token trigger untouched.
- `WindstormDrake.json` — "Other creatures you control with flying get +1/+0": ExcludeSelf with keyword:Flying characteristic preserved; +1/+0 modifiers correct.

Non-uniform (trigger/target/cost) shapes:

- `M21/BarrinTolarianArchmage.json` — "return up to one other target creature or planeswalker": returnToHand Target ExcludeSelf:true, CardTypes:[creature,planeswalker], "up to" via `optional`.
- `M21/NiambiEsteemedSpeaker.json` — "return another target creature you control": returnToHand Target ExcludeSelf:true + Controller:You; `optional` + `IfYouDo` gainLife(derived ManaValue) preserved.
- `MOM/LivingTotem.json` — "put a +1/+1 counter on another target creature": putCounters Target ExcludeSelf:true, +1/+1, Count 1; `optional` + Convoke preserved.
- `LTR/MerryEsquireOfRohan.json` — "attack with Merry and another legendary creature": Attacks trigger filter Supertypes:[Legendary] + Controller:You + ExcludeSelf:true; haste + equipped-first-strike untouched.

Out-of-scope confirmations (correctly NOT migrated, retained on whitelist-freetext with accurate re-tags):

- `CN2/GrenzoSRuffians.json` — "each other opponent" is a PLAYER filter (Target.Kind:EachOpponent), not an ObjectFilter self-exclusion. Correctly left as free-text debt.
- `ExpeditionRaptor.json` — "other target creatures" inside support 2 (SupportTriggeredRule), a separate mechanic. Free-text "other" deliberately retained as debt.

Projection decision (initiative 03):

- `libs/mast-interaction/PortGraphEngine.cs#ExcludeSelf-projection` — the new `ExcludeSelf` discriminator gets a SEMANTIC projection, not a coarse park: `PortLabel.Exclusion` projects `ExcludeSelf -> ':another'`, the operator decides it via `ObjectFilterRelations` returning `Reason=="ExcludeSelf"`, and the cross-card carve-out promotes a lone-ExcludeSelf `Unknown -> Yes` (a cross-card `from` can never be the excluded self). NOT present in `known-coarse-projections.json`. Sensible: a flow rule (aristocrat sac-bridge, e.g. Warren Soultrader `sac:creature:controlled:another`) genuinely consumes the `:another` facet.

## Glossary gaps

None. "other"/"another" self-exclusion is documented in `ObjectFilter.cs` and the GLOSSARY (Battle cry entry,
"'Other' is encoded as ExcludeSelf=true on the ObjectFilter (CR 109.5)").

## Process notes

- **Citation note (not a FAIL):** the slice and the `ObjectFilter.ExcludeSelf` doc-comment cite CR 109.5, but
  the live text of CR 109.5 in `rules-structure.json` is the "you/your" controller rule, and CR 109.2 is the
  "type description = permanent on the battlefield" rule — neither carries a numbered "another/other" exclusion
  clause (the rules dataset has no dedicated subrule for it). Both cited rules EXIST and are on-point for these
  filtered sets ("you control" -> 109.5; the buffed permanent set -> 109.2), so per the SKILL's narrow
  citation-FAIL bar (absent-from-data OR contradiction only) this is not a FAIL — it is the codebase's settled
  nearest-rule convention. Surfaced for awareness only.
- **GoblinChieftain whitelist mechanics are sound, but do not rescue the gold's fidelity.** The card was correctly
  removed from `oracle-text-quarantine.json` (text re-pointed to authoritative Scryfall) and added to
  `whitelist-unparsed.json` as named debt. That keeps the build/ratchet green, but the judge bar is rules-fidelity
  of the gold AST, and an `unparsed` Output node is a per-SKILL strict FAIL. The conjunction-parse gap
  ("get +1/+1 AND have haste" on one line) is a real engine debt, but the *gold* should still encode the split
  eventual-truth rather than a hole — every other tribal anthem in this very batch does.
- Whitelist bookkeeping for the 20 in-scope golds is consistent: all 20 were removed from `whitelist-freetext.json`
  (so the FreeTextWhitelist ratchet now requires them to be free-text-free), and a grep confirms 0 residual
  `CharacteristicType:"other"` across them. The 2 out-of-scope golds correctly retain their carve-outs.

## Verdict

22 PASS, 1 FAIL. **HALT** on GoblinChieftain until the anthem line is split into structured
`modifyPT(ExcludeSelf Goblin)` + `gainAbility(Haste over ExcludeSelf Goblin)` (replacing the `unparsed` Output
node) — or the orchestrator consciously accepts the gold carrying an unparsed regression, which contradicts the
SKILL.

HALT: M10/GoblinChieftain
