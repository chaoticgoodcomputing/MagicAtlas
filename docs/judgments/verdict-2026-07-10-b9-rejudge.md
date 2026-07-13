# MAST judge — batch-9 remediation re-judge

**Date:** 2026-07-10
**Branch/HEAD:** `feat/loop-trial` @ `0e811565` (batch-9 merge reconciliation)
**Scope:** 4 remediation items (3 citation fixes + 1 merge-combined rule)
**Result:** PASS (4/4)

## Summary
- PASS: 4
- FAIL: 0

## Per-item verdicts

### N20 — Najeela optional token — PASS
`HaveControllerCreateTappedAttackingTokenRule.cs`. The optional "you may" citation was changed from **CR 116.1b → CR 118.12**. Confirmed CR 118.12 exists and reads *"…'[A player] may [do something]. If [that player] [does, doesn't, or can't], [effect].'"* — a sound, in-family citation for the optional "may". CR 116 is "Special Actions" and 116.1 carries **no** subrules (no 116.1b existed — the old citation was fabricated). No residual `116.1b`/`116` in the file.

### N18 — Heart-Shaped Herb monarch — PASS
`OptionalSacrificeReturnWithCountersBecomeMonarchEffectRule.cs`. The monarch citation was changed from **CR 716 → CR 725**. CR 725 = "The Monarch" (725.1: *"The monarch is a designation a player can have…"*) — correct. CR 716 = "Class Cards" — confirmed unrelated. No residual `716`. The rule additionally cites CR 118.12 for the literal "You may sacrifice a creature. If you do…" idiom, which is exactly 118.12's pattern.

### N01 — Crackling Emergence self-reference — PASS
`EnchantedLandWouldBeDestroyedSacrificeGrantIndestructibleRule.cs`. The "this Aura" self-reference citations were changed from **CR 700.4 / 111.7 → CR 109.2**. CR 109.2 governs how an object description resolves; its subrule **109.2d is a literal "this scheme" self-reference** rule — so citing 109.2 for "'this [object]' in an object's own text refers to that object" is reasonable and non-contradictory. The file also cites CR 303.4c (Auras, exists). No residual `700.4`/`111.7`.

### N03/N20 combined — SubtypeAttacksConditionRule — PASS
`SubtypeAttacksConditionRule.cs`. Exactly **one** `SubtypeAttacksConditionRule` class on HEAD (git-grep confirmed). The regex `\b(?<article>a|an|another)\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)(?<control>\s+you\s+control)?\s+attacks\s*$` reproduces **both** golds:
- Knights' Charge "a Knight you control attacks" → `Subtypes:[Knight], Controller:You`.
- Najeela "a Warrior attacks" → `Subtypes:[Warrior], Controller:null`.

`another` → `ExcludeSelf=true` (CR 109.5 rationale in doc, e.g. Arahbo). Right-anchored on `attacks$` so it never swallows "attacks and isn't blocked" (Stinkdrinker Bandit left to fail cleanly). Case-sensitive (no `IgnoreCase`) + capitalized-first-letter subtype token, so lowercase "a creature attacks" correctly falls through to the generic `AttacksConditionRule`. Priority 995 sits above the generic (987). Citations CR 508 / 205.3m / 603.1 all exist and match the modeling. Semantically sound.

## Process notes
Core ring already green at 7161 (both golds match) per dispatch; this re-judge confirms rules-accuracy of the three citation swaps and the merge de-duplication. All old/nonexistent citations (116.1b, 716, 700.4, 111.7) fully removed.

**PROCEED** — 0 FAIL.
