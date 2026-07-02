# MAST judge — delta verdict (kavu-scout)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-kavu-scout
**Base:** 176e495d
**Scope:** regenerated gold `libs/magic-ast/schema/ast-schema.json` (+ KavuScout fixture, DomainQuantity node)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## Task

Kavu Scout — "Domain — This creature gets +1/+0 for each basic land type among lands you control."
Oracle text confirmed verbatim against oracle-cards.json (`{2}{R}`, 0/2, Creature — Kavu Scout).

## Per-item verdicts

- `libs/magic-ast/schema/ast-schema.json#DomainQuantity` — PASS. Schema delta is purely additive: one
  new value-quantity discriminator `{"Type":"DomainQuantity","Discriminator":"domain","IsUnparsed":false,"Fields":[]}`
  plus the recomputed SchemaHash. No existing entry dropped, renamed, or reshaped. `domain` is a
  reference-not-resolution game value (ADR 0004) sitting in the same value family as `devotion`
  (CR 700.5) / die-roll / any-amount — field-less by design, engine counts the distinct basic land
  types (CR 305.6) at eval time. Quantity nodes are not effect/cost/trigger/restriction discriminators,
  so no PortWalk projection decision is required.

- `tests/magic-ast-tests/Fixtures/HandParsedCards/KavuScout.json` — PASS. Single `static` ability,
  `AbilityWord:"Domain"` (domain is a CR 207.2c ability word with no rules meaning — the italic prefix
  is correctly carried as an ability word, not swallowed into the effect). Body is `modifyPT` on
  `Target Self` ("This creature"), `PowerModifier {QuantityType:"domain"}` (+1 per basic land type,
  single-increment convention → bare domain count), `ToughnessModifier` literal 0 (+0). Faithful to the
  card, describe-not-execute (no pre-resolution to a literal), no baked-in timing (a layer-7 continuous
  effect, CR 613.4, not a trigger). No `unparsed` / `EffectType:"unparsed"` and no rules-bearing
  free-text anywhere. Out-of-axis attributes (manaCost `{2}{R}` MV 3, colors/colorIdentity R,
  creatureStats 0/2) match the printed card. New file, so no sibling regression.

## Citation cross-reference

- CR 305.6 (basic land types = Plains/Island/Swamp/Mountain/Forest) — exists, verbatim match.
- CR 207.2 (italic text with no game function) / 207.2c (ability words list includes **domain**) — exists, matches.
- CR 613.4 (layer 7 P/T sublayers) — exists, matches "+N/+0-for-each, not set-P/T" claim.
- CR 700.5 (devotion) — exists, correctly cited only as the sibling value-quantity family, not as the domain rule itself.

All cited rules are present in rules-structure.json and none contradict the modeling.

## Glossary gaps

- "Domain" — referenced as AbilityWord in `KavuScout.json`; absent from glossary.json. NOTE only (not a
  FAIL): CR 207.2c confirms domain is an ability word with no individual CR entry, so the gap is expected.

ALL PASS
