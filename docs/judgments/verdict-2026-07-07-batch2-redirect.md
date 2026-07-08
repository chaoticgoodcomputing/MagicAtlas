# MAST judge — batch2 redirect verdict

**Date:** 2026-07-07
**Branch:** `mast-tdd/2026-07-07-damage-redirect-enkor` (base `02bae0fd`)
**Scope:** 4 surfaces (1 AST node, 1 citation set, 1 projection decision, 1 fixture)
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `libs/magic-ast/AST/Effects/Damage/RedirectDamageEffect.cs#RedirectDamageEffect` — PASS. A NEW node is the right call: redirection is not prevention (PreventDamageEffect removes damage and has no recipient; a redirect needs a `To`), and it is not the static permanent-scoped `ReplacementEffect` (that persists while its source is on the battlefield and bears no duration). This shield is created by the *resolution* of a `{0}` activated ability and is turn-scoped, so per ADR-0005 (Duration lives only on `ContinuousEffect`; one-shot CR 608 effects "cannot carry a duration") the "this turn" bound genuinely forces `: ContinuousEffect`. Exact structural sibling of `PreventDamageEffect : ContinuousEffect` ("Prevent the next N damage ... this turn"). Faithful to CR 614.9 (redirection effect) / CR 614.1a ("instead" = replacement).
- `libs/magic-ast/AST/Effects/Damage/RedirectDamageEffect.cs#citations` — PASS. Both cited rules exist and match: CR 602.1 is verbatim the `[Cost]: [Effect]` activated-ability shape (the `{0}:` ability); CR 614.1 is the replacement-effect doctrine ("watch for a particular event ... completely or partially replace"). Glossary "Redirection Effect" pinpoints CR 614.9 ("damage dealt to one ... creature ... with the same damage dealt to another ... called redirection effects") — a subrule of the cited 614.1, so the parent-level citation is correct (subrule imprecision is not a FAIL).
- `libs/mast-interaction/known-coarse-projections.json#redirectDamage` — PASS. Discriminator is unique within the `Effect` union (one schema Type entry, one `[OracleEffect("redirectDamage")]`). The consciously-inert projection is DEFENSIBLE, not false-inert. Verified against the actual arm: the damage flow arm is **source-keyed** — `dealDamage`/`DealsDamage*` emit and trigger ports both ride `Subject = the damage SOURCE`, with recipient only a label facet (`PortWalkProjection.cs:45,72-81`; `PortGraph.cs:443-460,814`). A redirect reroutes the RECIPIENT of an *already-in-flight* damage instance; it originates no source-keyed emit. The would-be consumer of a recipient-side redirect (a "creature is dealt damage" payoff, e.g. Daru Spiritualist) projects to `CreatureDealtDamage`, which is ITSELF parked coarse for exactly this reason (line 222: "watches the RECIPIENT, not the source; the damage arm is source-keyed, so this stays coarse until a recipient-keyed model lands"), as is the prevention sibling `preventDamage` (line 128). With no live recipient-keyed port to connect to, no flow rule can consume a `redirectDamage` emit today — so coarse is internally consistent, and en-Kor's combo relevance (the free `{0}` repeatable activation, and Nomads-style becomes-target vectors) rides other arms, not this effect discriminator. Revisit alongside the future recipient-keyed damage model that `CreatureDealtDamage`'s own note anticipates.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/TMP/WarriorEnKor.json` — PASS. Gold models the card's single `{0}` activated ability; the effect is fully typed — `Amount {literal,1}`, `From {Self}`, `To {Target, Filter:{CardTypes:[creature], Controller:You}}` ("target creature you control"), `Duration {untilTime, Turn/End}` ("this turn"). No `unparsed` Kind, no `unparsed` EffectType, no `Diagnostics`, no free-text characteristic strings, no describe-vs-execute prose, no dropped sibling ability.

## Glossary gaps

_None._ Both "Redirection Effect" (→ CR 614.9) and "Replacement Effect" (→ CR 614) are present in `glossary.json` and align with the modeling.

## Process notes

- This is the novel-shape branch and was judged hardest on the false-inert risk. The projection reason string's two claimed sibling precedents were both verified in-file rather than trusted: `preventDamage` (coarse, line 128) and `CreatureDealtDamage` (coarse with an explicit source-vs-recipient justification, line 222). The determining fact is that the entire recipient-perspective damage subsystem is uniformly coarse under the source-keyed arm, so parking `redirectDamage` there is consistent, not a hole hiding a live edge.
- The `ast-schema.json` `Fields` list for the node shows only `["From","To"]` because `Amount` (nullable) and the inherited `Duration` (nullable) are omit-when-null; `IsUnparsed:false`. Consistent with the sibling nodes' schema shape — not a concern.
