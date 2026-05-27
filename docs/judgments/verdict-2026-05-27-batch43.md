# MAST judge — batch verdict

**Date:** 2026-05-27
**Scope:** 4 files (3 fixtures, 1 parser rule — reviewed for rule-citation accuracy only; parser code correctness is out of scope per SKILL.md)
**Result:** FAIL

## Summary

- PASS: 0
- FAIL: 3 (all three fixtures)
- Plus: rule-citation drift in the dispatch brief (advisory note, not a separately-judged item)

## Doctrinal ruling — `EffectType: "unparsed"` inside a gold ability

The dispatch raised the question explicitly. Ruling:

**`EffectType: "unparsed"` inside a gold ability's `Effects[]` is a doctrinal violation, equivalent in standing to `Kind: "unparsed"` at the ability level.**

Reasoning:

1. **Spirit of the rule.** `mast-tdd-loop/SKILL.md` §240 names the principle in plain language *before* it enumerates examples: *"The hand-parsed JSON is the gold AST — what a fully-implemented parser SHOULD eventually emit for this card. It is not… partially populated with `UnparsedAbility` nodes where the parser currently falls short."* The enumeration at line 245 is illustrative, not exhaustive. An `UnparsedEffect` nested one level deeper is the same failure mode — a placeholder for what the parser can't yet emit — and it inverts TDD direction in the same way: `Parser_ProducesExpectedOutput` goes green by matching the parser's current limitations rather than driving it forward.

2. **This judge's own SKILL.md already says so.** `mast-judge/SKILL.md` "Unprocessed nodes in gold data" enumerates as FAIL:
   - `"Kind": "unparsed"` anywhere in `Output.Oracle.Abilities`.
   - **`"EffectType": "unparsed"` anywhere in a gold ability's `Effects[]`.**
   - Any nested partial structure where one sub-effect is gold-modeled and another is unparsed.

   The third bullet is exactly what these three fixtures do: the upkeep trigger is gold-modeled, the sacrifice-activated sibling is `UnparsedEffect`. Already-codified judge rule.

3. **Mech's "self-contained parser change" rationale is reasonable engineering and irrelevant to gold doctrine.** Gold AST is eventual-truth. "The parser-rule change I shipped is complete and doesn't depend on the sibling effect" is a fine reason to *land the parser change* — it is not a reason to *poison the gold fixture* with an unparsed placeholder. The fixture and the parser change are decoupled by design: gold represents the destination, the parser represents the current journey. Mixing them collapses that separation.

4. **Precedent for future batches.** When a card's full gold AST requires AST nodes that don't exist yet (e.g., `CalculatedQuantity` with variable-X bound to "the number of verse counters on this enchantment", or a `PreventDamageEffect` with division), the correct moves are, in order of preference:

   a. **Add the missing AST nodes as Red #1**, write the full gold AST using them, and accept that `Parser_ProducesExpectedOutput` stays red for this card until a future batch lands the parser surface. This is the loop's intended flow (`mast-tdd-loop/SKILL.md` §250: "*AST node shapes that don't yet exist — create them in libs/magic-ast/AST/ as Red #1*").

   b. **Defer the card.** If the AST gap is too wide to add in this batch (variable-X bindings, prevention with division, etc.), pull the card out of the fixture set entirely. Land the parser change with a different fixture that exercises the same parser rule without requiring out-of-scope sibling AST. The verse-counter trigger generalization can be proved with a card whose sacrifice-activated sibling IS already modelable (or with a card that has no sibling).

   c. **Never:** ship the fixture with an `UnparsedEffect` placeholder. The fixture is now wrong-by-construction; future batches that re-touch this card will see green tests and stop, leaving the unparsed hole in place indefinitely. The placeholder hides the gap from the orchestrator's signal.

This ruling stands as precedent for future batches.

## FAIL verdicts

### `tests/magic-ast-tests/Data/HandParsedCards/WarDance.json`
**Verdict:** FAIL
**Issue:** Gold AST contains `EffectType: "unparsed"` inside the sacrifice-activated ability's `Effects[]`.
**Rule citation:** N/A (doctrinal violation, not a rules-text mismatch)
**What the fixture says:**
```json
"Effects": [{
  "EffectType": "unparsed",
  "RawText": "Target creature gets +X/+X until end of turn, where X is the number of verse counters on this enchantment.",
  ...
}]
```
**Why this misrepresents the rule:** Gold AST is eventual-truth. An `UnparsedEffect` placeholder encodes "parser can't do this yet" — the precise condition gold is forbidden to encode.
**Suggested fix:** Either (a) add the AST nodes needed to gold-model "+X/+X where X is the number of verse counters" — likely `PumpEffect` + `CalculatedQuantity` with a counter-count variable binding — and accept that `Parser_ProducesExpectedOutput` stays red for this card, OR (b) revert this fixture entirely and prove the `PutCountersTriggeredRule` generalization with a different card whose sacrifice sibling has no unmet AST gap (e.g., a verse-counter card with a simpler sibling, or no sibling). The parser-rule change itself is preserved either way.

### `tests/magic-ast-tests/Data/HandParsedCards/SerrasHymn.json`
**Verdict:** FAIL
**Issue:** Same doctrinal violation — `EffectType: "unparsed"` inside the sacrifice-activated ability's `Effects[]`.
**What the fixture says:**
```json
"Effects": [{
  "EffectType": "unparsed",
  "RawText": "Prevent the next X damage that would be dealt this turn to any number of targets, divided as you choose, where X is the number of verse counters on this enchantment.",
  ...
}]
```
**Why this misrepresents the rule:** Same as above. Additionally compounded: this RawText requires `PreventDamageEffect` + variable X + multi-target damage division — a substantial AST surface that almost certainly belongs in its own batch.
**Suggested fix:** Strongly prefer option (b) — revert this fixture. The AST work to gold-model "prevent the next X damage divided as you choose among any number of targets" is large enough that bundling it into a parser-generalization batch obscures both pieces.

### `tests/magic-ast-tests/Data/HandParsedCards/RumblingCrescendo.json`
**Verdict:** FAIL
**Issue:** Same doctrinal violation — `EffectType: "unparsed"` inside the `{R}, Sacrifice`-activated ability's `Effects[]`.
**What the fixture says:**
```json
"Effects": [{
  "EffectType": "unparsed",
  "RawText": "Destroy up to X target lands, where X is the number of verse counters on this enchantment.",
  ...
}]
```
**Why this misrepresents the rule:** Same. Of the three, this is the closest to "easily fixable inline" — `DestroyEffect` exists, "up to X target lands" needs a `CalculatedQuantity` X-binding and an "up to" target-count modifier. If those AST nodes already exist, the gold can be written cleanly; if not, add them as Red #1.
**Suggested fix:** Either (a) gold-model `DestroyEffect` + `Target.Kind = Target` with `CardTypes: ["land"]` and `Count = CalculatedQuantity{ Source: counter-count-on-self, CounterType: "verse", UpTo: true }` (adding AST nodes as needed), OR (b) revert the fixture and prove the parser change elsewhere.

## Rule-citation drift in the dispatch brief

The dispatch cites Rules 603, 122, **506.2** for the upkeep verse-counter trigger. Verified against `rules-structure.json`:

- **Rule 603** (Handling Triggered Abilities) — correct broad cite; the precise subrule is **603.2b** (*"When a phase or step begins, all abilities that trigger 'at the beginning of' that phase or step trigger"*).
- **Rule 122.1** (Counters) — correct. *"A counter is a marker placed on an object or player… Counters with the same name or description are interchangeable."* This is what licenses the parser change from a closed `{+1/+1, -1/-1}` set to an open named-counter set: counters are interchangeable by name, and the rules impose no closed enumeration of valid counter type names.
- **Rule 506.2** — **wrong**. Rule 506.2 covers the combat phase ("the active player is the attacking player…"). The Upkeep Step is **Rule 503** (503.1–503.2). The dispatch cite is wrong by ~3 rules.

This is a brief-level note, not a fixture- or AST-level FAIL. Flagged so the orchestrator stops propagating `506.2 → upkeep` in future batches. Use **503** (Upkeep Step) and **603.2b** ("at the beginning of" trigger semantics) going forward.

## Parser change — rule-citation lens only

The parser change at `libs/magic-ast/Parsing/Parsers/Triggered/Rules/PutCountersTriggeredRule.cs` is out of scope for fixture verdicts (parser correctness is NUnit's job, per `mast-judge/SKILL.md` "Out of scope"). One advisory observation:

- The regex `\bput\s+a(?:n)?\s+(?<type>[\w\-]+)\s+counter\b` is grounded in Rule 122.1 — counters are identified by name, no closed enumeration. The dispatch raised "could fire on text like 'put a Goblin counter on'." The rules-accuracy answer is: **122.1 imposes no constraint**, so "Goblin counter" isn't a rules violation if a card ever printed that text. Any over-permissiveness concern is a *parser-precision* concern (false positives over the actual corpus), not a *rules-accuracy* concern. Out of scope for this skill.
- The `this enchantment` / `this artifact` / `this land` subject branches mirror the existing `this creature` / `this permanent` branches. Rules-side this is clean: "this [permanent type]" is standard self-reference language across the CR, and the AST resolves all of them to `ObjectReference.Self()` — same descriptive concept.

## Glossary gaps

None. "Verse counter" is a named counter (Rule 122.1) and named counters are open-set by design; no glossary entry needed.

## Process notes

The mech's rationale — "10 verse-counter cards share complex sacrifice siblings, no clean alternatives exist" — is true *and* doesn't license the placeholder. The clean alternative was always: **pick a different fixture set**, or **add the missing AST nodes as Red #1 and accept Parser_ProducesExpectedOutput red until the X-binding parser lands**. The parser-rule change is genuinely self-contained and would have been provable with three simpler cards whose siblings are already gold-modelable, or even with a hypothesized minimal card constructed for the test. The temptation to "ship the parser change with whatever fixtures happen to exist in the family" is exactly the failure mode the doctrine exists to prevent.

Recommendation to orchestrator: **revert the three fixtures**, **keep the parser change**, **re-dispatch a follow-up batch with cleaner fixtures** (or with the X-binding AST work bundled as Red #1). The parser generalization is good work; it just needs honest gold to land against.
