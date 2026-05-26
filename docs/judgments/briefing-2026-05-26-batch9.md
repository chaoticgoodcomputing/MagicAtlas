# MAST TDD briefing — batch 9 (autonomous run 5/10)

**Entering coverage:** 7,563 / 29,614 (25.54%). NUnit 372/0/372.

Two novel-AST families. Both helper-novel work; both parser-mechs are mechanical adds after the AST lands.

## Family A — Skip-untap effect (+24 marginal)

**Parser file:** `StaticAbilityParser` (sentence dispatch).

### Cards (3 fixtures, helper-novel)
Pre-curate single-line cards with EXACTLY `You may choose not to untap this creature during your untap step.`:
```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | contains("choose not to untap")) | "\(.name) | \(.type_line) | \(.oracle_text | gsub("\n"; " | "))"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### AST type
- **`SkipUntapEffect`** — `[OracleEffect("skipUntap")]`. No params (descriptive marker). Inherits standard `Effect + 3 traits`. Source: `libs/magic-ast/AST/Effects/Timing/SkipUntapEffect.cs` (next to `CantBeCastEffect` from earlier batches). Cite Rule 302.6 (Untap step), or Rule 116 (Player actions).

### Gold AST per fixture
```json
{
  "Kind": "static",
  "Effect": { "EffectType": "skipUntap", "IsOptional": false }
}
```

No `KeywordSource` — full sentence, not a keyword.

### Parser surface
StaticAbilityParser — add `TryParseSkipUntap` sentence dispatch. Regex match the exact sentence (or a tolerant pattern allowing minor variation). Mirror `TryParseEntersTapped`/`TryParseCantBlock`.

---

## Family B — Bushido keyword (+24 marginal)

**Parser file:** `OracleParsers` (needs novel **integer-parameterized** keyword combinator — extension to ParameterizedKeyword's existing mana-cost-parameterized shape).

### Cards (3 fixtures, helper-novel)
Single-line Bushido creatures from Kamigawa block:
- **Cunning Bandit** — likely simpler shape, verify
- **Bushi Tenderfoot** — common Bushido creature
- Search corpus for single-line `Bushido N` creatures

```bash
jq -r '.[] | select(.oracle_text != null) | select(.oracle_text | test("^Bushido \\d+ \\(")) | "\(.name) | \(.mana_cost) | \(.oracle_text)"' tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json | head -10
```

### AST type
- **`BushidoEffect`** — `[OracleEffect("bushido")]`. ONE required field: `Value: int` (or `Quantity` if matching existing convention — check `LiteralQuantity` shape; numeric value field on the effect is simplest). Inherits standard 4 trait interfaces. Source: `libs/magic-ast/AST/Effects/Keyword/BushidoEffect.cs`. Cite Rule 702.45.

Doctrine note: this is the first **integer-parameterized** keyword effect in the AST. Cycling/Equip/Morph etc. take a `Cost`; Bushido takes a value. Pick the cleanest shape — likely `Value: int` directly rather than wrapping in `Quantity`.

### Gold AST per fixture
```json
{
  "Kind": "static",
  "KeywordSource": "Bushido",
  "Effect": { "EffectType": "bushido", "Value": 2, "IsOptional": false }
}
```

### Parser surface
This is the novel-architecture piece — extend `OracleParsers` to support keyword + integer-token (digit) consumption + optional reminder. Cycling's combinator consumes mana symbols; this consumes a single integer token. New combinator shape, but well-bounded (parallels Cycling's structure).

Specifically: add a new combinator type or generalize ParameterizedKeyword. Likely cleanest:
- Add a sibling chain `IntegerParameterizedKeyword` or
- Add an entry to ParameterizedKeyword that uses `Token.Matching(t => t.Kind == OracleTokenKind.Number)` instead of mana symbols

Sub-agent decides. Bounded single-file change in OracleParsers.cs.

---

## Dispatch plan

**Wave 1 (1 helper-novel only, no helper-mech this batch):**
- `[sub:helper-novel]` (Opus): SkipUntapEffect + BushidoEffect + 6 fixtures (3 + 3).

**Wave 2 (2 parallel parser-mechs):**
- `[sub:mech]` (Sonnet) Family A: StaticAbilityParser sentence dispatch.
- `[sub:mech]` (Sonnet) Family B: OracleParsers integer-keyword combinator (novel shape per above).

**Yield ceiling:** ~48 cards.
