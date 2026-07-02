# MAST judge — slice verdict (DELTA)

**Date:** 2026-06-16
**Slice:** PB-3 — structured-characteristic megaslice (+ merged comparative-power PB-2)
**Gold judged:** tests/magic-ast-tests/Fixtures/HandParsedCards/AggressiveMammoth.json (uncommitted working-tree regen)
**Mode:** delta (judge only the change this slice made; out-of-scope residuals are other slices' debt)
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## PASS verdict

- `tests/magic-ast-tests/Fixtures/HandParsedCards/AggressiveMammoth.json` — PASS.
  The slice re-pinned the gold to the card's REAL oracle text. Ground truth (the
  local Scryfall dump `oracle-cards.json` AND the live Scryfall API) shows
  Aggressive Mammoth has exactly two lines:
  "Trample (reminder)" + "Other creatures you control have trample." The
  committed gold carried a fabricated THIRD line — "Creatures with power less
  than Aggressive Mammoth's power can't block it." — and a matching
  `cantBeBlocked` ability with a free-text `Description: "with power less than
  this creature's power"`. That clause does not exist on the real card. The
  regen removed it (OracleText/RawText now byte-identical to Scryfall) and the
  two real abilities (Trample keyword + gainAbility-trample anthem) are intact.

## Delta criteria

- (a) TARGET residual structured correctly: N/A-correctly-empty. The PB-2/PB-3
  comparative-power axis (`CantBeBlockedRule` → `PowerComparison{LessThan,
  RelativeTo:Self, RelativeCharacteristic:Power}`) was specced for this gold on
  the assumption it carried "can't be blocked by lesser power" (plan lines 228,
  350). That assumption was wrong — the line is a fabrication. The slice
  correctly structured NOTHING here and removed the fabricated free-text node
  rather than inventing a PowerComparison for a nonexistent clause. Faithful to
  the real card.
- (b) No NEW free-text/unparsed beyond scope: YES (PRIMARY criterion met). The
  only free-text on the regen is the pre-existing `other` OtherCharacteristic on
  the "Other creatures you control" filter — the Slice-6-owned residual, still
  whitelisted (whitelist-freetext.json entry preserved, sink `OtherCharacteristic`).
  The fabricated `with power less than this creature's power` Description was
  REMOVED, not added. No unparsed nodes anywhere.
- (c) No regression: YES. The dropped `cantBeBlocked` ability was never a real
  ability — removing a hallucinated line is a correction, not a dropped/inverted
  effect. The two real abilities and the co-occurring `other` filter are
  preserved unchanged. Remaining literal nodes (manaCost, creatureStats) only
  re-ordered keys / re-added default `IsVariable:false` — serialization noise,
  no semantic change.

## Out-of-scope residual remaining

- `other` (OtherCharacteristic, "Other creatures you control") on the
  gainAbility target filter — owned by Slice 6, correctly LEFT in place and kept
  whitelisted per the DELTA scope contract. Not a FAIL.

## Process notes

- The committed gold was factually wrong (a fabricated lesser-power line). Both
  the local dump and the live Scryfall API agree the real card is two lines.
  This regen is a net correctness improvement, not merely a refactor.
- rules-structure.json now lives under
  `libs/mtg-rules/Data/_03_Primary/Datasets/`; Trample CR 702.19 cross-checked
  there.
