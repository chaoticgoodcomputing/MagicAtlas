# MAST judge — DELTA verdict (SLICE PB-1)

**Date:** 2026-06-16
**Slice:** PB-1 — aura IsEnchanted + BearUmbra
**Target (delta-judged):** tests/magic-ast-tests/Fixtures/HandParsedCards/ROE/HyenaUmbra.json (uncommitted, working tree)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

This is a DELTA judgment of one co-regenerated gold (HyenaUmbra is NOT a named slice gold;
the slice's named set is LuminousWake / UnhallowedPact / BearUmbra). HyenaUmbra is an
Umbra-armor card touched by the slice's keyword rename. Judged ONLY for the slice's delta.

## Oracle ground-truth

Scryfall oracle-cards.json for "Hyena Umbra":
> Enchant creature\nEnchanted creature gets +1/+1 and has first strike.\nUmbra armor (If enchanted creature would be destroyed, instead remove all damage from it and destroy this Aura.)

The fixture's Input.OracleText matches verbatim.

## Per-criterion

(a) TARGET structured correctly — PASS. The slice's reachable axis on this gold is the keyword
    rename `Totem armor` -> `Umbra armor` (both `Keyword` and `KeywordSource`), plus the added
    `Reminder.Text`. Cross-referenced CR 702.89 = "Umbra Armor" and CR 702.89b verbatim:
    "Some older cards were printed with the ability 'totem armor'... updated in the Oracle card
    reference to refer to umbra armor instead." The new name is ground-truth; the OLD gold's
    `Totem armor` + the OLD doc-comment's `Rule 702.102` (= "Fuse", unrelated) were wrong.
    HyenaUmbra carried no `Other("enchanted")` filter characteristic to route to IsEnchanted —
    it expresses "enchanted creature" via the `EnchantedOrEquipped` reference — so the IsEnchanted
    axis correctly does not appear here, and HyenaUmbra is rightly absent from whitelist-freetext.json.

(b) NO new free-text/unparsed residual — PASS (PRIMARY criterion). Grep of the regenerated gold
    finds zero residual sinks: no `"unparsed"`, no `OtherCharacteristic`, no `"other"`
    CharacteristicType, no `Description`. Nothing beyond the slice's scope was inlined as prose.

(c) NO regression — PASS. All 3 abilities preserved: (1) static enchantRestriction (creature),
    (2) static composite = modifyPT +1/+1 on EnchantedOrEquipped + gainAbility (first strike static,
    combatDamageTiming Timing:First) on EnchantedOrEquipped, (3) static keywordAbility Umbra armor.
    No ability dropped/added/inverted; co-occurring modifyPT and gainAbility siblings intact.
    Remaining diff is serialization-neutral: KeywordSource field reordering, `IsVariable:false` on
    manaCost, removal of `Power/Toughness: null` from Input (aura has no P/T).

## Out-of-scope residuals (expected, NOT a FAIL)

- The first-strike gained ability is modeled as `combatDamageTiming {Timing:First}` rather than a
  `keywordAbility: First strike`. That is this gold's own keyword-modeling axis, pre-existing and
  untouched by PB-1 (the IsEnchanted / Umbra-armor axis). It is some other slice's debt, if anything.

## Citations cross-referenced

- CR 702.89 "Umbra Armor" — present in rules-structure.json. PASS.
- CR 702.89b (totem armor obsolete -> umbra armor) — present, verbatim match. PASS.
- CR 303.4 (Aura subtype; references 702.5 Enchant) — present, supports IsEnchanted axis. PASS.
- CR 702.5 "Enchant" — present. PASS.

ALL PASS
