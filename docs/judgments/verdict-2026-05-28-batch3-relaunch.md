# MAST judge verdict — Batch 3 (relaunch) — 2026-05-28

Judged the 5 novel-shape branches before merge (pure-keyword and pure-composition branches skipped per policy). Read-only review of each branch diff + gold fixture against the Comprehensive Rules (`rules-structure.json`).

| Branch | Verdict | Note |
|---|---|---|
| `b3-escalate` | **FAIL → remediated** | Modal fixture set `ModeSelection {Min:1, Max:1}` but "Choose one or more —" over 3 modes is `Max:3`. Root cause was a pre-existing parser bug: `ModalAbilityParser.TryParseModeSelection` let "choose one or more" fall through to the "choose one" prefix → `ChooseOne()`. Fixed: added `ModeSelection.ChooseOneOrMore(modeCount)` + resolve `Maximum`=mode count in `Parse()`; corrected the fixture. CR 702.120a citation verified. Re-gated green. (Only `WildcallSpree` used the phrase pre-fix and it is Spree, not modal — no other fixture affected.) |
| `b3-draw-second` | PASS | `Ordinal:2`/`PerTurn:true` descriptive (engine defers tally); reuses existing `TriggerEvent.DrawsCard`. CR 603.2 verified. |
| `b3-subtype-enters-return` | PASS | Both abilities modeled; Cartouche trigger via `Subtypes`+`Controller:You`; self-return uses `Target:{Kind:Self}`. CR 603.6/603.2 verified. |
| `b3-destroyland-loselife` | PASS | One spell ability, two effects; "Its controller" is structured `ObjectReference{Kind:Controller}` (existing anaphoric kind), not free text. CR 701.8a/119.3 verified. |
| `b3-threshold` | PASS | `AbilityWord:"Threshold"` (CR 207.2c — ability word, no rules meaning, verified); both effects gated by `asLongAs`; mirrors existing AnuridBarkripper Threshold fixture. |
| `b3-combat-tapdown-v2` (redo) | PASS (orchestrator spot-check) | No unparsed; structured `DealsCombatDamageToCreature` event + `composite` [tap, doesntUntap] with `It` back-reference. CR 510.1/502.3/603.2 verified. |

**Deferred (not judged, not merged):** `b3-chosen-anthem` (Etchings of the Chosen). Briefing premise wrong — the activated line 3 ("Target creature you control gains indestructible…") does not parse; fixture is RED. Re-queue bundled with a "Target creature you control gains [keyword]" activated-effect family.

Final integration gate: **1692/1692 NUnit green.** No-ratchet-tolerance satisfied.
