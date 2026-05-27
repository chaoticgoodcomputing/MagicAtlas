# Batch 30 briefing — 2026-05-27

Single mech family. All AST infrastructure exists; only a triggered rule is missing.

| Family | Cluster | Yield | Surface |
|---|---|---|---|
| A: Oblivion-Ring pattern (ETB exile-until-leaves) | #5 | 12 | new TriggeredRule using existing ExileEffect + UntilLeavesBattlefieldDuration |

Skipping cluster #1 (Fear — AST shape deferred), cluster #2 (As-enters-choose-type — deferred), cluster #3 (Unleash — embedded conditional), cluster #4 (Start your engines! — Max Speed GrantedAbility design needed).

---

## Family A: Oblivion-Ring pattern (cluster #5, +12 yield)

**Failure signal:** Oracle line `When this enchantment enters, exile target nonland permanent an opponent controls until this enchantment leaves the battlefield.` — ETB-triggered exile with `untilLeavesBattlefield` duration. Trigger detection works (ETB self), no `[TriggeredRule]` handles the verb+duration combination.

### Verified rule citations
- **701.10 Exile** — "To exile an object, put it into the exile zone." (`UntapTargetRule` from batch 18 cites this; same verb)
- **611 Continuous effects** — durations including "until [object] leaves the battlefield" (sub-rule 611.2c implicit).
- **607 Linked abilities** — the implicit pairing with the LTB return ability is engine territory; MAST records the ETB-with-duration only.

### Cards in this family
Hieromancer's Cage and Oblivion Ring variants:
- **Hieromancer's Cage** — `When this enchantment enters, exile target nonland permanent an opponent controls until this enchantment leaves the battlefield.`
- **Stormplain Detainment** — same body.
- **Static Net** (`Static Net` has the body line + a separate ETB life-gain + token line as a sibling — verify sibling parseability)
- **Journey to Oblivion** — body line + Affinity-style cost sibling.
- **Deputy of Detention** — variant: "exile ... and all other nonland permanents that player controls with the same name as that permanent..." (more complex; SKIP if surface gets too wide).

Find via:
```bash
jq -r '.[] | select(.oracle_text != null and (.oracle_text | test("exile target nonland permanent an opponent controls until this"))) | .name + "|" + (.oracle_text | split("\n") | join("\\n"))' \
  tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### Existing infrastructure
- `ExileEffect` — already exists.
- `UntilLeavesBattlefieldDuration` — already exists at `libs/magic-ast/AST/Effects/Duration.cs`. Has optional `Object: string?` field.
- `ObjectReferenceKind.Target` for the targeted nonland permanent.
- `ObjectFilter { CardTypes: [...], IsNonland: true|null, Controller: Opponent }` — verify the "nonland" filter shape (might be `Characteristics: ["nonland"]` or a dedicated `IsNonland` field; check existing fixtures).

### Parser surface
New `[TriggeredRule]` file `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ExileNonlandUntilLeavesTriggeredRule.cs` (or similar). Receives post-trigger effect text. Match `^exile target nonland permanent an opponent controls until this (?:creature|artifact|enchantment|permanent) leaves the battlefield\.?$`. Emit:
```csharp
new ExileEffect {
  Target = new ObjectReference {
    Kind = ObjectReferenceKind.Target,
    Filter = new ObjectFilter {
      CardTypes = ["nonland", "permanent"],  // OR a dedicated IsNonland flag — verify
      Controller = ControllerFilter.Opponent,
    },
  },
  Duration = new UntilLeavesBattlefieldDuration {
    Object = "this enchantment",  // OR null with Self semantics; verify convention
  },
}
```

### Gold AST shape
```json
{
  "Kind": "triggered",
  "Trigger": { "Timing": "When", "Event": "Enters", "Filter": { "CardTypes": ["enchantment"] } },
  "Effects": [{
    "EffectType": "exile",
    "Target": {
      "Kind": "Target",
      "Filter": {
        "CardTypes": ["nonland", "permanent"],
        "Controller": "Opponent"
      }
    },
    "Duration": {
      "DurationType": "untilLeavesBattlefield",
      "Object": "this enchantment"
    }
  }]
}
```

(Verify exact `IsNonland` filter representation by inspecting other "nonland" fixtures if any.)

### Anti-patterns
- Do NOT model the linked LTB-return ability (engine territory per 607.2).
- Do NOT confuse "until this enchantment leaves" with "until end of turn" — different duration entirely.
- Do NOT widen scope to Deputy-of-Detention's "and all other permanents with the same name" — that's a separate, more complex shape (SKIP that card; pick the simpler Hieromancer's Cage / Stormplain Detainment).

### Cards to fixture (3)
- **Hieromancer's Cage** (clean, single-ability)
- **Stormplain Detainment** (clean, single-ability)
- One more single-ability card from the cluster.

Pre-validate sibling abilities. Multi-ability cards like Static Net or Journey to Oblivion may bail on siblings.

---

## Cross-family notes

- **Single-family batch** — keeping the loop tight.
- **No new AST types** — ExileEffect + UntilLeavesBattlefieldDuration both exist.
- **Pre-verified rule citations:** 701.10 (Exile) + 611 (continuous effects/duration).
