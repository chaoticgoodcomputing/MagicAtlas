# MAST judge — DELTA verdict: PB-1 (aura IsEnchanted + BearUmbra)

**Date:** 2026-06-16
**Slice:** PB-1 — aura IsEnchanted + BearUmbra
**Scope:** 1 fixture (ROE/BearUmbra.json) — DELTA judgment (this slice's axis only)
**Result:** PASS

## Summary
- PASS: 1
- FAIL: 0

## What the slice structured (and verified)
- **Input re-pointed FIRST** to the real Scryfall oracle text. Verified byte-exact against
  `oracle-cards.json`: "Enchant creature / Enchanted creature gets +2/+2 and has \"Whenever this
  creature attacks, untap all lands you control.\" / Umbra armor (...)". The prior gold carried
  corrupt input (+3/+3, a standalone "Whenever the enchanted creature attacks" line, "Totem armor").
- **(a) modifyPT +2/+2 on EnchantedOrEquipped** — present, literal 2/2.
- **(b) gainAbility on Target{EnchantedOrEquipped}** whose GainedAbility is the TRIGGERED ability
  "Whenever {creature, IsSelf:true} attacks -> untap Each {land, Controller:You}". The self-reference
  is correctly modeled as `IsSelf: true` on the granted ability's filter (the granted ability's own
  source is the enchanted creature), NOT a separate "enchanted creature" filter — exactly per spec.
  This kills the old `Characteristics:[{CharacteristicType:"other", Description:"enchanted"}]` residual.
- **(c) keyword "Umbra armor"** (current Oracle name) — the obsolete "Totem armor" was replaced, with
  reminder text attached. Confirmed against CR 702.89 (Umbra armor is current; totem armor obsolete).
- Reuses the GorgonsHead/GuardDuty gainAbility-on-Aura precedent (Target{EnchantedOrEquipped} +
  GainedAbility), here with a triggered GainedAbility.
- Removed from `whitelist-freetext.json` (OtherCharacteristic sink).

## Citations cross-referenced
- **CR 303.4** — exists; defines Aura attachment and "enchanted [object]" semantics. Matches.
- **CR 702.5** — exists ("Enchant"). Matches the enchantRestriction.
- **CR 702.89** — exists; "Umbra armor" current / "totem armor" obsolete (renamed). Matches keyword fix.

## Delta criteria
- (a) Target residual structured CORRECTLY: yes — Other("enchanted")/Aura-trigger -> granted triggered
  ability + IsSelf self-ref; PT +2/+2; Umbra armor keyword.
- (b) No NEW free-text/unparsed residual beyond scope: confirmed — residual scan
  (`.. | select(CharacteristicType=="other" or Kind=="unparsed" or EffectType=="unparsed" or has("Description"))`)
  returns empty.
- (c) No regression: ability/effect inventory parity preserved; sibling modifyPT not lost; no
  dropped/inverted/added ability; nodes outside this axis serialize as expected.

## Out-of-scope residual remaining
None observed on this gold.

## Verdict
PASS.
