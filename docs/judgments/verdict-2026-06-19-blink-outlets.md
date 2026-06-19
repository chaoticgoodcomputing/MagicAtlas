# MAST judge — batch verdict (blink-outlets)

**Date:** 2026-06-19
**Scope:** 5 judged targets (3 fixtures, 1 AST-node doc-comment, 1 PortWalk projection decision)
**Result:** FAIL

## Summary

- PASS: 4
- FAIL: 1

Oracle text for all three cards verified byte-for-byte against
`tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (not from memory). No
`unparsed` / `OtherX` nodes in any of the three fixtures; no free-text `Characteristics` arrays.

## FAIL verdicts

### libs/magic-ast/Parsing/Parsers/Activated/Rules/TapEffectRule.cs (doc-comment summary)
**Verdict:** FAIL
**Issue:** the `<summary>` doc-comment cites "Rule 701.21" for the tap action.
**Rule citation:** 701.26a (correct); 701.21 (mis-cited).
**Rule text:**
> 701.26a — "To tap a permanent, turn it sideways from an upright position. Only untapped permanents can be tapped."
> 701.21 — "Sacrifice"
**What the AST/comment says:** `... "Tap two target creatures" (Thassa, Deep-Dwelling) (Rule 701.21).`
**Why this misrepresents the rule:** 701.21 is the Sacrifice action, not Tap — a citation that
contradicts the modeled effect. This is a transposition slip: the same file's enchanted-tap branch
correctly cites `CR 701.26a`, so the author knows tap is 701.26.
**Suggested fix:** change "(Rule 701.21)" to "(Rule 701.26)" in the class summary. (Fixture-only fix;
the gold `ThassaDeepDwelling.json` itself is correct and needs no change.)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ThassaDeepDwelling.json` — PASS. Indestructible static
  (702.12); devotion<5 `loseType creature` matches the Theros-God pattern in `THB/HeliodSunCrowned.json`
  one-for-one (700.5 devotion); end-step triggered `exile`(upTo Max1/Min0, ExcludeSelf, Controller You)
  then `returnToBattlefield`(Designated, ExiledWith:Self, UnderControl You) is a faithful blink
  (603.6e / 400.7); `{3}{U}` `tap` ExcludeSelf. Trigger/effect cleanly decomposed; no unparsed.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/Flickerwisp.json` — PASS. ETB `exile`(another target
  permanent → ExcludeSelf, any permanent) + `createDelayedTrigger` whose body returns the exiled card
  (ExiledWith:Self, UnderControl Owner) at `End/Beginning/Next` — a CR-faithful delayed triggered
  ability (603.7), correctly deferred to the next end step rather than modeled as an immediate return.
  Flying evasion present; no unparsed.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AVR/DeadeyeNavigator.json` — PASS. Soulbond keyword
  static (702.95 in this dataset; the glossary's Soulbond entry cites 702.95) + a conditional
  `gainAbility` (Target `BothPaired`, `asLongAs` duration) granting a `{1}{U}` self-blink
  (`exile` Self + `returnToBattlefield` ExiledWith:Self, UnderControl You). Paired-duration encoding is
  identical to the pre-existing `TandemLookout`/`Wingcrafter` Soulbond golds; pairing is carried by the
  typed `BothPaired` reference, so the `ConditionType:"other"` duration text is consistent corpus
  precedent, not a new free-text shortcut. No unparsed.
- `libs/mast-interaction (projection: returnToBattlefield)` — PASS. The load-bearing new blink-outlet
  discriminator `returnToBattlefield` is semantically projected: `emit:blink` when the return is of the
  just-exiled card (`Target.Filter.ExiledWith:Self`), and the whole `composite`/`optional`-wrapped
  `[exile, returnToBattlefield(ExiledWith:Self)]` is folded to one `emit:blink` (PortGraph cites
  603.6e/400.7/117.7). A non-blink return stays coarse `emit:returntobattlefield` — sensible. The
  Flickerwisp-introduced `createDelayedTrigger` is consciously parked in `known-coarse-projections.json`
  with an honest reason; coarse is defensible here because the delayed (next-end-step) return cannot
  close a same-resolution refuel loop the way the immediate Emiel/Displacer blink does. (See Process notes.)

## Glossary gaps

None. Devotion (cites 700.5), Soulbond (cites 702.95), Indestructible (702.12) are all in
`glossary.json` / `rules-structure.json`.

## Process notes

- **Dispatch-prompt citation drift (not a code FAIL):** the Deadeye task framed Soulbond as "CR 702.96",
  but in this rules dataset 702.96 = Overload and **702.95 = Soulbond**. The judged artifact
  (`ExileSelfThenReturnToBattlefieldRule.cs`) correctly cites **702.95**, matching the data, so no FAIL
  is raised on the artifact — only flagging the briefing's parenthetical was off by one subrule index.
- **`createDelayedTrigger` recursion gap (surfaced, not failed):** PortGraph does not recurse into a
  delayed-trigger's inner effects, so Flickerwisp's delayed `returnToBattlefield(ExiledWith:Self)` does
  not currently surface its `emit:blink` through the coarse `createDelayedTrigger` wrapper. This is
  inert for now (the return is to a future end step, not a same-pass refuel) and is honestly recorded in
  the coarse whitelist; if a future flow rule wants to chain delayed blinks, this is the de-coarse point.
  Surfaced for the orchestrator; not a per-item FAIL.

HALT: TapEffectRule.cs (doc-comment cites 701.21=Sacrifice for the tap action; should be 701.26).
