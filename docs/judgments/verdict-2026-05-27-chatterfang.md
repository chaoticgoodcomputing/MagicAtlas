# MAST judge — Chatterfang depth-dive verdict

**Date:** 2026-05-27
**Scope:** 1 fixture + 4 source files (3 parser changes, 1 AST node read-only check)
- `tests/magic-ast-tests/Data/HandParsedCards/MH2/Chatterfang.json`
- `libs/magic-ast/Parsing/Parsers/ActivatedAbilityParser.cs` (Sacrifice filter + ModifyPT signed modifier)
- `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (TryParseTokenAugmentationReplacement)
- `libs/magic-ast/AST/Quantities/Quantity.cs` (CalculatedQuantity, read-only — pre-existed)
- `libs/magic-ast/AST/Effects/Replacement/ReplacementEffect.cs` + `TokenCreationEvent.cs` (read-only)

**Result:** FAIL

## Summary

- PASS: 4
- FAIL: 3

The fixture itself parses correctly and the AST shapes are internally coherent. The FAILs are all **rule-citation precision** in the parser doc-comments / dispatch prompt — citations to rules that don't exist or don't say what the prose claims. MAST-judge is strict on subrule precision per skill discipline.

---

## FAIL verdicts

### 1. StaticAbilityParser.cs — token-augmentation doc-comment cites Rule 614.2

**Verdict:** FAIL
**Issue:** Doc-comment says `Rule 614.2 augmentation`. Rule 614.2 does not describe augmentation — it reads "Some replacement effects apply to damage from a source. See rule 609.7." The augmentation pattern ("…instead") is grounded in **Rule 614.1a**, not 614.2.
**Rule citation:** 614.1a
**Rule text:**
> "Effects that use the word 'instead' are replacement effects. Most replacement effects use the word 'instead' to indicate what events will be replaced with other events."
**What the parser claims:** `"…— Rule 614.2 augmentation: the original token creation still occurs, AND an equal number of additional tokens (per the supplied token definition) are also created."`
**Why this misrepresents the rule:** 614.2 is the damage-from-a-source carve-out (`See rule 609.7`). Chatterfang's `…are created instead` clause is a vanilla 614.1a "instead" replacement; the *augmentation* shape (original event still occurs PLUS a new event) is not a separate subrule in CR 614 — it is just an "instead" replacement where the substituted event is `original + additional`. Citing 614.2 misnames the rule and would mislead a future reader looking up the doctrine.
**Suggested fix:** Replace the `Rule 614.2 augmentation` reference in the doc-comment (and any dispatch-prompt manifest entry) with `Rule 614.1a (replacement effects using "instead")`. If the team wants a precedent label for the "original + delta" shape, coin it locally (e.g., "additive-instead pattern") but do not attach a non-existent subrule number to it.

### 2. ActivatedAbilityParser.cs — Sacrifice filter cites Rule 701.16

**Verdict:** FAIL
**Issue:** Dispatch prompt and any associated doc-comment cite `Rule 701.16 / 701.21 (Sacrifice…)`. Rule 701.16 in this rules data is **Investigate**, not Sacrifice. Sacrifice is 701.21 (specifically 701.21a). The dual cite is at best vestigial — at worst it documents the wrong mechanic.
**Rule citation:** 701.21a
**Rule text:**
> "To sacrifice a permanent, its controller moves it from the battlefield directly to its owner's graveyard. A player can't sacrifice something that isn't a permanent, or something that's a permanent they don't control…"
**What the dispatch says:** `**Rule citation:** 701.16 / 701.21 (Sacrifice — recall batch 22 fixed 701.17 to 701.21).`
**Why this misrepresents the rule:** 701.16 is `Investigate` (`"Investigate" means "Create a Clue token." See rule 111.10f.`). Carrying it forward — even with a hedge — pollutes the precedent log. The orchestrator's own note ("recall batch 22 fixed 701.17 to 701.21") shows this lineage has already drifted once; ratcheting on the bad citation will keep tripping future judges.
**Suggested fix:** Drop 701.16 from the citation. The sacrifice cost concept is fully covered by 701.21a (mechanic) plus 117.1 / 118.3 (paying costs). Use `Rule 701.21a` in any doc-comment that lands.

### 3. ActivatedAbilityParser.cs — ModifyPT signed-modifier doc-comment cites Rule 605

**Verdict:** FAIL
**Issue:** Dispatch claims `Rule citation: 605 (PT modifications) + 613.4c (Layer 7c)`. Rule 605 is **Mana Abilities**, not power/toughness modification. The PT-modification doctrine lives entirely in 613.4c (Layer 7c) and, for variable values, in the activated-ability cost-binding rules (601.2 / 107.3).
**Rule citation:** 613.4c
**Rule text:**
> "Layer 7c: Effects and counters that modify power and/or toughness (but don't set power and/or toughness to a specific number or value) are applied."
**What the dispatch says:** `**Rule citation:** 605 (PT modifications) + 613.4c (Layer 7c).`
**Why this misrepresents the rule:** 605.1 begins `"Some activated abilities and some triggered abilities are mana abilities…"` — the rule classifies mana abilities, with no PT semantics in scope. The +X/-X "until end of turn" effect on Chatterfang is purely a 613.4c interaction (the variable binding happens via 601.2f/107.3, not 605). Citing 605 here is structurally wrong, not just imprecise.
**Suggested fix:** Replace `605 (PT modifications)` with `613.4c (Layer 7c: PT modification)`. If a citation for the *variable-X* binding is wanted, use `107.3 (variables in costs/effects)` or `601.2f (announcing X)`; do not use 605.

---

## PASS verdicts

- `tests/magic-ast-tests/Data/HandParsedCards/MH2/Chatterfang.json` — PASS. Three abilities cleanly modeled: Forestwalk evasion with `DefendingPlayerControls` Forest condition (Rule 702.14 landwalk family), token-augmentation replacement (`OriginalEventOccurs: true` faithfully encodes "those tokens **plus** that many"), and the `{B}, Sacrifice X Squirrels: target creature gets +X/-X` activated ability with `Filter { Subtypes: ["Squirrel"] }`, `Quantity: VariableQuantity X`, and the +X / -X composition. No `unparsed` anywhere. Discriminators (`replacement`, `tokenCreation`, `createToken`, `modifyPT`, `sacrifice`, `mana`) all match the rules' terminology.
- `libs/magic-ast/AST/Effects/Replacement/ReplacementEffect.cs` — PASS. Doc-comment cites Rule 614 (correct family); `OriginalEventOccurs` field has a Chatterfang-specific comment that aligns precisely with the augmentation semantics ("those tokens **plus** that many…": original occurs, replacement adds). The shape is rule-faithful: `Event` + `OriginalEventOccurs` + `Replacement` + optional `Modifier` cleanly distinguishes pure-instead from additive-instead from scaled-instead (Doubling Season).
- `libs/magic-ast/AST/Effects/Replacement/TokenCreationEvent.cs` — PASS. Optional `TokenFilter` + `MinimumQuantity` capture the "one or more tokens would be created" precondition without overspecifying. Matches the literal oracle pattern.
- `libs/magic-ast/AST/Quantities/Quantity.cs` (read-only; pre-existed) — PASS for use. `CalculatedQuantity` already exposes `Expression`, `BaseQuantity`, `Operation`, `Rounding` as nullable strings/quantities; no hidden AST change snuck in via this batch. The mech's composition (`"negate"` over `VariableQuantity X`; `"match"` for "that many") fits the pre-existing shape. See process notes below for one terminology concern this leaves on the table.

---

## Specific convention questions raised in the dispatch

### Variable-negation convention (`CalculatedQuantity { Operation: "negate", BaseQuantity: VariableQuantity X }`) — SOUND.
Composition over a new node is the right call. Rejecting `NegatedVariableQuantity` avoids a node whose only job is to negate an existing primitive (the rule of three: not yet). However, `Operation` is a free-text `string?` field on `CalculatedQuantity`. The values currently in flight from this batch (`"negate"`, `"match"`) plus pre-existing uses (`"half"`, `"double"`, `"twice"`?) are accumulating without a fixed vocabulary. **CONCERN (not BLOCKING for this batch):** when a fourth or fifth `Operation` lands, promote the field to an enum or a known-tokens registry; otherwise `Operation = "Negate"` vs `"negate"` vs `"negated"` will eventually drift and become un-queryable. This is a doctrine note for the engine-lens audit, not a FAIL on Chatterfang.

### "That many" augmentation count (`CreateTokenEffect.Count = CalculatedQuantity { Expression: "that many", Operation: "match" }`) — SOUND, with the same `Operation`-vocabulary caveat.
Putting the count on the **replacement's** `CreateTokenEffect.Count` (not on `ReplacementEffect.Modifier`) is correct for the additive-instead shape. The oracle text says `those tokens plus that many 1/1 Squirrels are created instead` — the original tokens are unchanged, and `that many` describes the count of the **added** tokens, which is `CreateTokenEffect.Count`'s job. `ReplacementModifier` is the right slot for Doubling-Season-style `twice that many` because there the *original event* is being scaled, not augmented. Mech's split is doctrinally clean.

Caveat: `Expression: "that many"` is descriptive of the oracle string, but the structural concept — "the same count as the triggering token-creation event" — is more precisely a back-reference to `ReplacementEffect.Event` (here, `TokenCreationEvent`). MAST-as-descriptive accepts the free-text form, but future work that wants to render token math (Chatterfang + Parallel Lives + Doubling Season stacking) will need a structured back-reference. Flag for the engine-lens audit, not a FAIL.

### Singular-vs-plural sacrifice-filter split — SOUND for the corpus tested, with two edge-case risks.
The `wasPlural = type.EndsWith("s") && type != "this"` plus `char.IsUpper(typeRaw[0]) && !wasPlural` gate cleanly separates `Sacrifice Denethor` (capitalized singular self-ref → `Characteristics: ["this permanent"]`) from `Sacrifice X Squirrels` (capitalized plural subtype → `Subtypes: ["Squirrel"]`). The single `type != "this"` guard handles the obvious false-positive.

**Edge cases worth a fixture before relying on this in production:**

1. **Legendary names ending in `s` that don't pluralize.** `Sacrifice Borborygmos`, `Sacrifice Atogatog`, `Sacrifice Tatyova` — the regex `wasPlural` flag would fire (`Borborygmos`.EndsWith("s") == true), incorrectly treating these as plurals. The `!wasPlural` gate fails → they'd singularize to `Borborygmo`/`Atogato`/etc. and land on `Subtypes`, which is wrong on both counts. **MAST today has no such fixture, but the parser will misfire when one lands.**
2. **Multi-word card names.** `Sacrifice Wrenn and Six` — the regex `(?:Sacrifice|sacrifice) (?:a |an |X )?(\w+)` only captures `Wrenn`, dropping `and Six`. That's a separate gap (name boundary detection), not the singular/plural split, but worth noting since the dispatch prompt called it out.

Neither edge case touches Chatterfang's fixture (where the cost is unambiguously `Sacrifice X Squirrels`), so the batch's claim that no existing fixtures regress (corroborated by 664/664 green) holds. But the **doctrine** — "capitalized singular = self-ref; capitalized plural = subtype" — is fragile on legendary names with trailing `s`. The skill-discipline-correct fix is a real noun-form lookup (against subtype glossary) rather than orthographic guessing. Flag for follow-up; **NOT a FAIL on this batch** since no current fixture exercises the bad path.

---

## Glossary gaps

None new. `sacrifice`, `replacement effect`, `token`, `forestwalk`, `until end of turn` are all in `glossary.json` or covered by parent rules (101–122).

---

## Process notes

The three FAILs are all the same shape: **doc-comment / dispatch-prompt cites the wrong rule number**, even though the AST shape and parser behavior are correct. The fixture and the structural conventions are doctrinally sound. This batch can land as soon as the citations are corrected — either inline edits to the three sites (StaticAbilityParser doc-comment, ActivatedAbilityParser sacrifice region, ActivatedAbilityParser ModifyPT region) or a follow-up cleanup commit.

The deeper takeaway for the orchestrator: **cite to subrule clauses, not section banners.** `Rule 614.2` and `Rule 605` are both citation drifts that the rules data immediately falsifies (`614.2` is damage-from-source, `605` is mana abilities). The orchestrator's own dispatch-prompt cribbing forward of `701.16 / 701.21` is the same failure mode — a copy-paste from a half-remembered prior verdict survives into the next batch's manifest. Running `jq '.sections[].subsections[] | select(.number == NN) | .rules[] | select(.number == "NN.MM")'` before writing a citation costs less than this verdict round-trip.

`Operation`-as-free-text on `CalculatedQuantity` is on watch. When the fourth value lands, promote.

---

HALT