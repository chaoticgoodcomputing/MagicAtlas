# Batch 20 briefing — 2026-05-26

Two parallel families:

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Partner keyword | #5 | 14 | `KeywordDefinitions.cs` (parameterless variant alongside existing `PartnerWith`) |
| B: Attack-trigger +N/+N investigation | #2 | 16 | `TriggeredAbilityParser` — diagnostic, then fix |

Cluster #3 (Fear) deferred pending AST shape decision. Cluster #4 (become target → sacrifice) deferred — adding a new trigger event would conflict with Family B on the same parser file.

---

## Family A: Partner keyword (cluster #5, +14 yield)

**Failure signal:** Oracle line `Partner (You can have two commanders if both have partner.)` — the parameterless Partner keyword is not registered. `PartnerWith` (parameterized: "Partner with [Name]") exists at `KeywordDefinitions.cs:293`, and `PartnerEffect` + `PartnerType` enum both already exist. The gap is just the registration entry for the unadorned `Partner` variant.

### Cards in this family
1. **Reyhan, Last of the Abzan**
2. **Kraum, Ludevic's Opus**
3. **Tana, the Bloodsower**
4. **Ravos, Soultender**
5. **Vial Smasher the Fierce**

Cluster has 14 candidates. Pre-validate each — most are Legendary creatures with at least one sibling ability. Pick 3 with siblings that already parse.

### Relevant rules
- **702.124a Partner** — "Partner is a static ability that modifies the rules for deck construction in the Commander variant. Once the deck is built, partner does nothing."
- **702.124b** — "A card with partner that doesn't list any specific name allows that card and any other card with partner (that also doesn't list any specific name) to be Commanders of the same deck."

MAST records the keyword's presence; the Commander-format deck-construction implications are engine territory (cf. `feedback_mast_describes_not_executes`).

### AST setup
- **`PartnerEffect`** already exists at `libs/magic-ast/AST/Effects/Keyword/PartnerEffect.cs`.
- **`PartnerType.Partner`** enum value already exists in `libs/magic-ast/AST/Effects/Keyword/PartnerType.cs`.

### Parser surface
- New `KeywordDefinition Partner` in `KeywordDefinitions.cs` (parameterless variant, paired with the existing `PartnerWith`). Pattern:
  ```csharp
  public static KeywordDefinition Partner { get; } =
    new()
    {
      Name = "Partner",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Partner",
        Effects = [new PartnerEffect { PartnerType = PartnerType.Partner }],
      },
    };
  ```
- Add `Partner` to the `All` collection.
- Add to `OracleParsers.cs` SimpleKeyword `.Or()` chain.
- **Dispatch ordering:** `PartnerWith` must still win over `Partner` when "Partner with" appears in input. Either:
  - place `PartnerWith.Try()` BEFORE `Partner.Try()` in the OracleParsers chain (likely already correct — verify), or
  - ensure the `Partner` parser matches only when no `with [Name]` follows.

### Gold AST shape
```json
{
  "Kind": "static",
  "KeywordSource": "Partner",
  "Reminder": { "Text": "(You can have two commanders if both have partner.)" },
  "Effects": [{ "EffectType": "partner", "PartnerType": "Partner" }]
}
```

(Verify `PartnerType` discriminator casing in existing fixtures.)

---

## Family B: Attack-trigger +N/+N investigation (cluster #2, +16 yield)

**Failure signal:** Oracle line `Whenever this creature attacks, it gets +N/+N until end of turn.` — `ModifyPTTriggeredRule` already exists at `Parsing/Parsers/Triggered/Rules/ModifyPTTriggeredRule.cs` and handles "it gets +N/+N until end of turn." The trigger-side `Whenever this creature attacks` is detected in `TriggeredAbilityParser.cs:574` (attacks branch). So both halves should compose. **Why doesn't it?**

### Investigation task
**This is a diagnostic-first family.** Before writing any fix code:

1. Pick 1-2 exemplar cards from cluster #2 (e.g., **Steadfast Cathar**: `Whenever this creature attacks, it gets +0/+2 until end of turn.`).
2. Write a gold fixture for one of them (use existing `TriggeredAbility` + `ModifyPTEffect` AST types — no new types needed). Path: `tests/magic-ast-tests/Data/HandParsedCards/{Set}/SteadfastCathar.json`.
3. Run `Parser_ProducesExpectedOutput` on it.
4. Inspect `/tmp/mast-diffs/{Set}_SteadfastCathar.actual.json` vs `.expected.json`.
5. Diagnose the gap: is it (a) trigger detection failing to recognize "this creature attacks", (b) effect dispatch not routing to `ModifyPTTriggeredRule`, (c) `ModifyPTTriggeredRule` running but emitting wrong AST, or (d) something else?
6. Fix the diagnosed gap minimally. Do NOT speculate beyond what the diff shows.

### Hypotheses to test (in order)
1. **Trigger detection** — line 574 matches `lower.Contains("attacks")`. Does `TryParseAttacksTrigger` (line 575) succeed on `Whenever this creature attacks`? `ParseObjectFilter` for "this creature" may be the snag — IsSelfByNameTrigger is the path for `Whenever [Name] attacks` (named self), not for `Whenever this creature attacks` (anonymous self). Check whether the latter path exists.
2. **Effect text post-trigger** — after the trigger is peeled, the effect text is "it gets +0/+2 until end of turn." (no leading comma; trigger detection should strip the comma). Does `ModifyPTTriggeredRule.TryMatch` receive that exact string?
3. **Rule dispatch order** — `[TriggeredRule]` rules are tried in some order. Is `ModifyPTTriggeredRule` ever reached, or does an earlier rule short-circuit?

### Reading order
1. `$WORKTREE_ROOT/libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs:556-690` (trigger detection for enters/attacks/etc.)
2. `$WORKTREE_ROOT/libs/magic-ast/Parsing/Parsers/Triggered/Rules/ModifyPTTriggeredRule.cs` (the effect rule)
3. `$WORKTREE_ROOT/libs/magic-ast/Parsing/Parsers/Triggered/TriggeredRuleHelpers.cs` (rule registration / dispatch)

### Fix scope
Minimal. If hypothesis 1 is right (anonymous self attack trigger missing), add a branch in `TryParseAttacksTrigger` for `Whenever this creature attacks` → `TriggerCondition { Event: Attacks, Filter: { CardTypes: ["creature"], References: [Self] } }` or `{ Subject: Self }` per the existing convention for self-references.

If hypothesis 2 is right (effect-text format mismatch), tighten the regex in `ModifyPTTriggeredRule`.

### Cards to fixture (3)
- **Steadfast Cathar** — `+0/+2`
- **Brazen Wolves** — `+2/+0`
- **Charging Bandits** — verify

All 16 cluster cards share the identical shape. Pick the cleanest single-line printings.

### Anti-patterns
- Do NOT add a new AST type (the gold uses existing `TriggeredAbility` + `ModifyPTEffect` + `UntilEndOfTurnDuration`).
- Do NOT touch `ModifyPTTriggeredRule`'s effect-emission shape if the issue is elsewhere — the existing rule emits the correct gold.

---

## Cross-family notes

- **Files touched are disjoint.** Family A: `KeywordDefinitions.cs` + `OracleParsers.cs` (SimpleKeyword section). Family B: `TriggeredAbilityParser.cs` (and possibly its helpers). No conflict.
- Land both in parallel; if a conflict arises in `OracleParsers.cs` from independent insertions, auto-merge should resolve trivially.
