# MagicAST

A type-safe parser for Magic: The Gathering oracle text, producing structured ASTs for card interpretation systems.

## Quick Start

```csharp
using MagicAST;
using MagicAST.Parsing;

var parser = new OracleParser();
var result = parser.Parse("Flying\nWhenever a creature dies, draw a card.");

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Abilities: {result.Metrics.TotalAbilities}");
```

## API Surface

### `OracleParser`

The main entry point for parsing oracle text.

```csharp
public sealed class OracleParser
{
    public ParseResult Parse(string? oracleText);
}
```

## Input Schema

Oracle text is a string with abilities separated by newlines (`\n`). The parser handles:

- **Keyword abilities**: `Flying`, `Trample`, `Haste`
- **Activated abilities**: `{T}: Add {G}.`
- **Triggered abilities**: `Whenever a creature dies, draw a card.`
- **Static abilities**: `Other creatures you control get +1/+1.`
- **Modal abilities**: `Choose one —\n• Draw a card.\n• Destroy target artifact.`
- **Reminder text**: `(This creature can't be blocked.)`
- **Quoted abilities**: `gains "When this creature dies, draw a card."`

## Output Schema

### `ParseResult`

```csharp
public record ParseResult
{
    CardOracle Output;        // The parsed AST
    ParseStatus Status;       // FullyParsed | Partial | Failed
    IReadOnlyList<Diagnostic> Diagnostics;
    ParseMetrics Metrics;
}
```

### `CardOracle`

```csharp
public record CardOracle
{
    string RawText;                    // Original oracle text
    IReadOnlyList<Ability> Abilities;  // Parsed ability nodes
}
```

### Ability Types

| Type               | Pattern               | Example                                  |
| ------------------ | --------------------- | ---------------------------------------- |
| `TriggeredAbility` | When/Whenever/At ...  | `Whenever a creature dies, draw a card.` |
| `ActivatedAbility` | {Cost}: {Effect}      | `{T}: Add {G}.`                          |
| `StaticAbility`    | Declarative statement | `Other creatures you control get +1/+1.` |
| `ModalAbility`     | Choose N —            | `Choose one — • Draw a card.`            |
| `SpellAbility`     | Spell effect          | `Deal 3 damage to any target.`           |
| `UnparsedAbility`  | Parse failure         | Contains raw text + diagnostics          |

### `ParseMetrics`

```csharp
public record ParseMetrics
{
    int TotalAbilities;
    int ParsedAbilities;
    int FailedAbilities;
    double DurationMs;
    double SuccessRate;  // ParsedAbilities / TotalAbilities
}
```

### `Diagnostic`

```csharp
public record Diagnostic
{
    DiagnosticSeverity Severity;  // Error | Warning | Info
    string Message;
    TextSpan Location;
    IReadOnlyList<string>? Expected;
    string? RawText;
    string? Pattern;  // Failure category for aggregation
}
```

## Architecture

```
Oracle Text
    │
    ▼
┌─────────────┐
│ Tokenizer   │  OracleTokenizer (Superpower-based)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Splitter   │  ClauseSplitter (by paragraph/bullet)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Classifier  │  AbilityClassifier (pattern matching)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Parsers   │  Routed by ability type
└──────┬──────┘
       │
       ▼
  ParseResult
```

## Failure Handling

MagicAST never throws on parse failure. Unparseable clauses produce `UnparsedAbility` nodes with:

- Original raw text preserved
- Diagnostic explaining why parsing failed
- Pattern tag for failure aggregation

```csharp
if (result.Status == ParseStatus.Partial)
{
    foreach (var diag in result.Diagnostics)
    {
        Console.WriteLine($"[{diag.Pattern}] {diag.Message}");
    }
}
```

## Status

🚧 **In Development** — Parser infrastructure is complete; individual ability parsers are being implemented incrementally.
