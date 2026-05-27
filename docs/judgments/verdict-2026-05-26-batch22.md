# MAST judge — batch 22 verdict

**Date:** 2026-05-26
**Scope:** 11 files (9 new fixtures + 1 reference fixture + parser/rule sources for 3 families)
**Result:** FAIL

## Summary

- PASS: 8
- FAIL: 5

The fixtures themselves are internally consistent and descriptively represent their oracle text within the AST's existing shapes. **All five FAILs are rule-citation errors in source-code doc-comments** (`SacrificeTriggeredRule.cs`, `TriggeredAbilityParser.cs`, `StaticAbilityParser.cs`). Each cited rule number either does not exist or points to the wrong rule entirely. Citations are the judge's primary tripwire and these are not close calls — `701.17` is Mill, not Sacrifice; `114.9` is in Emblems and does not exist as a subrule; `613.1c` is "Layer 3 Text-changing effects," not the layer 7 PT-modifier rule the comment claims.

The Family B `CountQuantity.CountOf: string` shape is flagged in the engine-lens audit (lines 457-477) as free-text-where-structure-exists. It is pre-existing AST surface, not a regression introduced by this batch, so I'm not failing the fixtures over it — but see Process notes.

---

## FAIL verdicts

### `libs/magic-ast/Parsing/Parsers/Triggered/Rules/SacrificeTriggeredRule.cs`
**Verdict:** FAIL
**Issue:** Doc-comment cites Rule 701.17 as Sacrifice. Rule 701.17 is **Mill**.
**Rule citation:** Sacrifice is **701.21a**.
**Rule text:**
> 701.21a: "To sacrifice a permanent, its controller moves it from the battlefield directly to its owner's graveyard. ..."
> 701.17: "Mill" (unrelated)

**What the source says:**
```
/// "sacrifice it" — triggered self-sacrifice on the creature that fired the
/// trigger (Rule 701.17). ...
```

**Why this misrepresents the rule:** Cites a wrong-mechanic subrule. A reader cross-referencing `701.17` lands on Mill and finds no support for the implemented behavior. The briefing's claimed `701.16` is also wrong (that's Investigate). The actual section is `701.21`.

**Suggested fix:** Change `(Rule 701.17)` to `(Rule 701.21a)` in the XML doc-comment.

---

### `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs` — line 587 comment
**Verdict:** FAIL
**Issue:** Inline comment cites "Rule 114.9" for the becomes-the-target trigger. Rule 114 is "Emblems" and contains only 114.1–114.5; **114.9 does not exist**.
**Rule citation:** The Comprehensive Rules do not define a numbered target-trigger rule in the form the comment implies. The general triggered-ability machinery is **603.1–603.2**; the target relationship itself is defined in **115 (Targets)**, specifically **115.1** ("Only objects on the stack ... can target ... an object becomes a target ..."). The Ward subrule (**702.21a**) confirms "becomes the target of a spell or ability" is the canonical phrasing.

**Rule text:**
> 603.1: "Triggered abilities have a trigger condition and an effect. They are written as '[When/Whenever/At] [trigger condition or event], [effect].' ..."
> 702.21a: "Ward is a triggered ability. Ward [cost] means 'Whenever this permanent becomes the target of a spell or ability an opponent controls, ...'"

**What the source says (lines 586-595):**
```
// BecomesTarget trigger: "When this creature becomes the target of a spell or ability".
// Rule 114.9 — a permanent becomes the target of a spell or ability when that spell or
// ability is placed on the stack. ...
```

**Why this misrepresents the rule:** 114.9 does not exist. The plausible-sounding "a permanent becomes the target of a spell or ability when that spell or ability is placed on the stack" is a paraphrase that doesn't match any subrule in the corpus.

**Suggested fix:** Replace with `Rule 603.1 (triggered abilities) + Rule 115.1 (targets)`. The trigger event word `BecomesTarget` is grounded in oracle convention (e.g., Ward, Rule 702.21a) rather than a single dedicated rule.

---

### `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs` — line 1041 doc-comment
**Verdict:** FAIL
**Issue:** Method doc-comment repeats the spurious "Rule 114.9 trigger" claim.
**Rule citation:** Same as above — 603.1 + 115.1, not 114.9.

**What the source says (lines 1040-1044):**
```
/// "When this creature becomes the target of a spell or ability" —
/// Rule 114.9 trigger. The subject "this creature" is the source permanent;
```

**Suggested fix:** `Rule 603.1 + 115.1 (target-triggered ability).`

---

### `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` — line 178 comment
**Verdict:** FAIL
**Issue:** Comment cites "Rule 613.1c, layer 7C." Rule **613.1c is Layer 3 (text-changing effects)**, not Layer 7c. The PT-modifier sublayer is **613.4c** (Layer 7c — "Effects and counters that modify power and/or toughness").
**Rule citation:** **613.4c**, with **613.1g** (Layer 7) as the parent.

**Rule text:**
> 613.1c: "Layer 3: Text-changing effects are applied. See rule 612, 'Text-Changing Effects.'"
> 613.4c: "Layer 7c: Effects and counters that modify power and/or toughness (but don't set power and/or toughness to a specific number or value) are applied."

**What the source says (lines 176-180):**
```
// "This creature gets +N/+M for each <filter> you control." — self
// P/T modifier scaled by a count of permanents the controller controls
// (Rule 613.1c, layer 7C). ...
```

**Why this misrepresents the rule:** A reader following `613.1c` lands on text-changing effects (Layer 3) and concludes the parser is in the wrong layer entirely. The "layer 7C" label is right; the rule number is the wrong subrule of 613 (`.1c` vs `.4c`).

**Suggested fix:** Change `Rule 613.1c, layer 7C` to `Rule 613.4c (Layer 7c)`.

---

### `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` — `TryParseSelfPTForEach` doc-comment (around line 460)
**Verdict:** FAIL
**Issue:** No rule citation on the new method. Every nearby parser surface in the same file cites the rule it implements (`Rule 601.5`, `Rule 303/702.5`, etc.). This one omits the citation entirely, which is inconsistent with the file's documentation conventions and leaves the same vacuum the line-178 wrong-rule comment tried to fill.

**Suggested fix:** Add an explicit `Rule 613.4c` citation in the XML doc-comment summary block. (This is the same fix as the line-178 comment, but the method's own doc-comment is currently silent on rule grounding.)

---

## PASS verdicts

- `tests/magic-ast-tests/Data/HandParsedCards/VIS/TarPitWarrior.json` — PASS. Models the becomes-the-target trigger + self-sacrifice with `Event: BecomesTarget`, `Filter.CardTypes: ["creature"]`, `SacrificeEffect { Target: It }`. Grounded in Rule 603.1 (triggered abilities) and Rule 701.21a (Sacrifice).
- `tests/magic-ast-tests/Data/HandParsedCards/FRF/FrostWalker.json` — PASS. Same shape as TarPitWarrior; descriptively identical oracle text yields identical AST.
- `tests/magic-ast-tests/Data/HandParsedCards/A25/PhantasmalBear.json` — PASS. Same shape.
- `tests/magic-ast-tests/Data/HandParsedCards/MRD/NimLasher.json` — PASS. ModifyPTEffect with `PowerModifier: CountQuantity { CountOf: "artifact you control" }` and zero-toughness via `LiteralQuantity`. Grounded in Rule 613.4c.
- `tests/magic-ast-tests/Data/HandParsedCards/DOM/BenalishHonorGuard.json` — PASS. Same shape with `CountOf: "legendary creature you control"`.
- `tests/magic-ast-tests/Data/HandParsedCards/M11/EarthServant.json` — PASS. Mirror shape on the toughness side (`CountOf: "Mountain you control"`); literal zero on power.
- `tests/magic-ast-tests/Data/HandParsedCards/EMA/ForceSpike.json`, `M10/ManaLeak.json`, `ZNR/Quench.json` — PASS. CounterSpellEffect with target spell + `UnlessClause { Player: Controller, Cost: ManaCost{generic N} }`. Grounded in Rule 701.6a (Counter). The `Player: Controller` reference (not `TargetController`) matches the existing ClashOfWills convention and the rule's own phrasing ("its controller pays") — the briefing's `TargetController` suggestion was wrong; fixtures correctly followed the existing ClashOfWills shape.
- `tests/magic-ast-tests/Data/HandParsedCards/MM3/ClashOfWills.json` (regression check) — PASS. Existing `{X}` fixture remains green after regex expansion to `(?:\{[^}]+\})+`.

## Ward sibling check

PASS. Ward is parsed in `StaticAbilityParser` as a keyword ability (line 127 of that file), before any text reaches `TriggeredAbilityParser`. The new "becomes the target" matcher in the triggered parser cannot interfere with Ward parsing because Ward is recognized upstream as a static keyword and emits a fully formed `WardAbility` without ever exposing the underlying "becomes the target..." reminder text to the triggered parser. No regression risk on Ward-bearing cards.

## Glossary gaps

None new. All MTG-domain terms used in the new fixtures (sacrifice, counter, target, spell, ability, permanent, controller, mana, mountain, legendary, artifact, creature) are present in `glossary.json`.

## Process notes

**Family B free-text doctrine (the briefing's question 4):**
The `CountQuantity { CountOf: "artifact you control" }` shape uses a `string` field for what is structurally a `(Filter: ObjectFilter, Controller: ControllerFilter)` pair. The engine-lens audit (`docs/ast-engine-lens-audit.md` lines 457-494) already flags this as free-text-where-structure-exists and proposes a structured fix (`CountQuantity { Filter: ObjectFilter }`). The fixtures correctly use the AST as it currently exists; I am not failing the fixtures over a pre-existing AST shape. But the descriptive-doctrine question the briefing asked is settled: **it is lossy**, the audit knows it's lossy, and the structural fix is pre-scoped. The corpus quality won't improve here until the audit's `CountQuantity` refactor lands.

**Family B scope limitation (multiplier-1 only):**
Confirmed at parser source (`StaticAbilityParser.cs:486-489`: `if (Math.Abs(power) > 1 || Math.Abs(toughness) > 1) return null;`). Cards like "+2/+0 for each X" or "+0/+2 for each X" fall through to fallback, which is correct conservative behavior. Not a defect — the parser explicitly bails on shapes it doesn't model rather than mis-encoding them. Worth noting in the batch summary so the next triage pass can target multiplier-N shapes intentionally.

**Family C `Player: Controller` (briefing question on `TargetController`):**
The briefing speculated the player reference should be `TargetController`. Fixtures correctly use `ObjectReferenceKind.Controller` — matching the existing ClashOfWills fixture and the rule's literal phrasing "its controller pays." `Controller` here means "the controller of the targeted spell" by syntactic implication (the `UnlessClause` is attached to a `CounterSpellEffect` whose `Target` is a spell). PASS doctrinally; the briefing's speculation was wrong.

**Descriptive-vs-executive doctrine (judge skill's hard rule):**
All three families respect descriptive-not-executive doctrine. Family A records the trigger verb and the effect verb without modeling stack ordering or target-legality re-checks. Family B records the count source without modeling layer-7c re-application. Family C records the counter verb and the unless clause without modeling stack removal. None of the AST changes leak engine semantics.

**Root cause of the FAILs:**
All five FAILs are clerical rule-citation errors in doc-comments. None affect serialized AST output; tests are still 570/570 green. They are nonetheless real because a future contributor or judge cross-referencing the cited rule number will land on the wrong rule and either (a) waste time confirming nothing is broken or (b) propagate the wrong citation into adjacent code. Fix is local — change five comment strings; no behavior change.
