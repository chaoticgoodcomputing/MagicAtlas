# MAST judge — batch verdict

**Date:** 2026-05-27
**Scope:** batch 33 — 3 fixtures + parser extensions to `StaticAbilityParser` (`BuildBareGrantFilterTarget` bare-plural-subtype arm, `MapKeywordToStaticAbility` menace case) + `TriggeredAbilityParser.IsSelfByNameTrigger` lowercase-function-word fix + new `EachOpponentDiscardsRule` triggered rule
**Result:** FAIL

## Summary

- PASS: 4 (3 fixtures + IsSelfByNameTrigger fix)
- FAIL: 2 (rule-citation errors in source comments)

## FAIL verdicts

### `libs/magic-ast/Parsing/Parsers/StaticAbilityParser.cs` (menace mapping)

**Verdict:** FAIL
**Issue:** Source comment misnumbers the Menace rule.
**Rule citation:** 702.111 (Menace), subrule 702.111b for the "two or more creatures" parameter.
**Rule text:**
> 702.111 Menace
> 702.111a Menace is an evasion ability.
> 702.111b A creature with menace can't be blocked except by two or more creatures. (See rule 509, "Declare Blockers Step.")

**What the source says** (line 2212):
```csharp
// Menace: this creature can't be blocked except by two or more creatures.
// Rule 702.110. EvasionEffect with MinimumBlockers=2; …
```

**Why this misrepresents the rule:** 702.110 is the Exploit keyword, not Menace. The comment was likely transcribed from the briefing, which itself stated `menace = 702.110`. The AST shape (`EvasionEffect { CanBeBlockedBy: creature, MinimumBlockers: 2 }`) is correct against 702.111b — only the citation in the comment is wrong, but a wrong rule citation in an AST surface is a discriminator-grade defect: future judges and contributors will follow the cite, not the prose.

**Suggested fix:** Change the comment to `Rule 702.111 (Menace), subrule 702.111b.`

### `libs/magic-ast/Parsing/Parsers/Triggered/Rules/EachOpponentDiscardsRule.cs`

**Verdict:** FAIL
**Issue:** Doc-comment misnumbers the Discard rule.
**Rule citation:** 701.9 (Discard).
**Rule text:**
> 701.9 Discard
> 701.9a To discard a card, move it from its owner's hand to that player's graveyard.

**What the doc-comment says** (line 13):
```csharp
/// "each opponent discards a card" — ETB discard effect imposed on all opponents
/// simultaneously. Rule 701.7 (discard); Rule 800.4b (simultaneous actions for
/// multiple players). …
```

**Why this misrepresents the rule:** 701.7 is the Create keyword action (token creation), not Discard. Discard is 701.9. Same defect class as the Menace miscite: AST surface advertises an incorrect rule pointer. Compounding: the simultaneous-players citation `800.4b` refers in current rules-structure.json to player-leaves-the-game effects, not "Simultaneous actions" — `800.4b` text reads "If an object would change to the control of a player who has left the game …" Verify whether the multiplayer-simultaneous semantics live elsewhere (likely rule 101.4 — "If an effect has each player choose…" / APNAP order — or rule 800.4 parent without subrule). If the engine concern is out-of-MAST-scope anyway (per `feedback_mast_describes_not_executes`), drop the multiplayer cite entirely.

**Suggested fix:** Change `Rule 701.7 (discard)` to `Rule 701.9 (Discard)`. Either correct or remove the `800.4b` engine-side cite (multiplayer simultaneous-action handling is engine territory and does not belong in the MAST descriptive AST comment).

## PASS verdicts

- `tests/magic-ast-tests/Data/HandParsedCards/ARB/MadrushCyclops.json` — PASS. `gainAbility` with `Target { Kind: Each, Filter { CardTypes: [creature], Controller: You } }` and `GainedAbility` carrying `KeywordSource: Haste` / `EffectType: haste` correctly models "Creatures you control have haste" per Rule 613.1f (Layer 6 ability-adding effects) + Rule 702.10 (Haste).
- `tests/magic-ast-tests/Data/HandParsedCards/M10/GoblinChieftain.json` — PASS. Three-ability decomposition (intrinsic Haste, "Other Goblins you control get +1/+1" ModifyPT, "Goblins you control have haste" GainAbility) correct. The third ability uses the bare-subtype filter shape (`Subtypes: [Goblin]`, no `CardTypes`) matching the Somberwald/Sachi precedent. Rules: 613.1f + 702.10.
- `tests/magic-ast-tests/Data/HandParsedCards/WOE/HagOfNoxiousNightmares.json` — PASS. ETB `discardCards` with `Player: EachOpponent`, `Count: 1` models the trigger per Rule 603.6a (ETB) + Rule 701.9 (Discard). Nested Menace shape (`KeywordSource: Menace`, `EvasionEffect { CanBeBlockedBy: { CardTypes: [creature] }, MinimumBlockers: 2 }`) matches the canonical Rograk Son of Rohgahh fixture, correctly grounding Rule 702.111b.
- `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs` IsSelfByNameTrigger lowercase-function-words — PASS. The whitelist `(of|the|a|an|from|for|to|in|at|with|by|and|or|as)` is a conservative under-approximation rather than an over-approximation: it requires the **first** word to be capitalised, so "Sachi, Daughter of Seshiro" continues to match (Daughter is capitalised; "of" is in the whitelist; Seshiro is capitalised). The whitelist does not introduce false positives because lowercase content words in the trigger body (e.g., "deals damage") still bound the name on the verb side via the `(enters|dies|attacks)` anchor. No regression risk surfaced by the 676/676 green run.

## Convention reconcilability — answering the briefing's structural question

The briefing asked whether three different effect-shaping conventions are reconcilable:

1. `CostReductionEffect.PerObject` (batch 26 Affinity).
2. `StaticAbility.AffectedObjects` (batch 25 Family B type-cost-reduction).
3. `GainAbilityEffect.Target.Filter` (batch 33).

**Verdict: reconcilable, distinct semantic axes.** Each represents a different question the oracle text answers:

- **`PerObject`** answers "how does the magnitude scale?" — the filter defines a *count* that drives the effect's quantitative output. Affinity-style cost reduction is `1 × |matching permanents|`. The filter is an argument to the scaling function.
- **`AffectedObjects`** answers "to which objects does this static ability attach?" — used when the static ability *is* the per-object effect (e.g., "Goblin spells you cast cost {1} less to cast" — the cost-reduction is applied at the moment the matching object is cast, not broadcast as a granted ability).
- **`Effect.Target.Filter` with `Kind: Each`** answers "which objects receive a one-time-broadcast effect?" — the `GainAbilityEffect` is the canonical "Layer 6 ability-adding" surface (Rule 613.1f), and the filter defines the set of recipients of the granted ability.

These axes are orthogonal in principle, but the distinction between `AffectedObjects` (Option 1) and `Effect.Target.Filter` (Option 2) is the one judges should watch for drift on. The current rule of thumb — verified against Somberwald/Sachi precedent — is: when the effect carries an explicit `Target: ObjectReference`, the filter rides on the target reference (Option 2); `AffectedObjects` is reserved for static abilities whose effect has no separate target conceptually (the static applies to a class of objects without a "broadcast" verb). The batch 33 fixtures correctly use Option 2. No convention drift in the gold data.

## Sibling-addition justification — five-criteria scrutiny

Per Step 7 (sibling-shape allowance: single-shape, not in another in-flight family, covered by existing AST types, genuinely smaller than the family work, recorded in the manifest):

1. **Menace mapping** — JUSTIFIED. Fills a single missing keyword in an established expansion table (`MapKeywordToStaticAbility`). 8 lines. No new AST types (`EvasionEffect` exists with `MinimumBlockers`). Strictly smaller than the family work. Necessary to land Hag of Noxious Nightmares. *Only quibble:* the source comment cites the wrong rule number (see FAIL above).
2. **IsSelfByNameTrigger lowercase function words** — JUSTIFIED. Single shape (capitalised first word + capitalised content words OR whitelisted function words, terminated by an event verb). Not a family in flight. No new AST. Conservative whitelist closes the precise gap "Hag *of* Noxious Nightmares" without opening the door to arbitrary lowercase words. No regression on Sachi (Daughter is capitalised). Justified.
3. **EachOpponentDiscardsRule** — JUSTIFIED on Step 7 grounds. Single shape (`^each opponent discards a card\.?$`). No new AST types (uses existing `DiscardCardsEffect` + `ObjectReferenceKind.EachOpponent`). Smaller than the family work. Recorded in the briefing. **However,** the doc-comment misciting Rule 701.7 instead of 701.9 (FAIL above) makes this a defective surface that needs correction before merge.

## Glossary gaps

None. All terms (haste, menace, evasion, discard, opponent, controller, subtype, layer 6) exist in `glossary.json`.

## Process notes

- Both FAILs are pure rule-citation defects in comments / doc-comments, not structural defects in the AST or gold data. The 676/676 green test run is uncompromised; the merge already landed. These are post-merge correctness fixes against the source-of-truth comments.
- The Menace miscite originates upstream in the briefing itself (`menace = 702.110`). Recommend updating the briefing's "Verification checks" template to require lookup against `rules-structure.json` *before* drafting, not after.
- The `800.4b` cite in `EachOpponentDiscardsRule` references engine-territory multiplayer semantics that MAST explicitly avoids (`feedback_mast_describes_not_executes`). Removing this cite is the cleaner fix than re-numbering it.

HALT.
