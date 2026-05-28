# Keyword file template (Stage B extraction)

One file per keyword in this folder. Each implements `IKeyword`, is decorated with
`[Keyword]`, and is auto-discovered by `KeywordRegistry` via reflection. **No shared
file is edited** — that is the whole point of Phase 2 (it kills the
`KeywordDefinitions.cs` + `OracleParsers.cs` merge-conflict bottleneck).

## What you are doing

For the keyword you are migrating, move two things into a new file here:

1. Its `KeywordDefinition` — copied verbatim from `Keywords/KeywordDefinitions.cs`
   (the `public static KeywordDefinition <Name> { get; } = new() { ... }` block).
2. Its parser combinator — copied verbatim from
   `Parsing/Combinators/OracleParsers.cs`
   (the `public static readonly TokenListParser<OracleToken, StaticAbility> <Name> = ( ... )` field).

**Leave the legacy entries in place.** The bridge tries the registry first and falls
back to legacy, so your file *shadows* the legacy twins. Deleting the legacy content
is Stage C, not your job. (Do not touch the `KeywordDefinitions.All` list or the
`OracleParsers` Or-chains.)

## File shape

```csharp
namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
// ... whatever AST.Effects.* / AST.References / AST.Costs namespaces your
//     Definition + Combinator reference (copy the usings the legacy code needed)
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;                 // only if you use Token.EqualTo(...) directly
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>... doc copied/adapted from the legacy Definition's doc-comment ...</summary>
[Keyword]                                  // add `(Priority = N)` only when ordering matters — see below
public sealed class <Name>Keyword : IKeyword
{
  public KeywordTier Tier => KeywordTier.Simple;        // or Parameterized — see decision rule below

  public KeywordDefinition Definition { get; } =
    new()
    {
      // ↓ verbatim from KeywordDefinitions.<Name>
      Name = "...",
      RuleReference = "...",
      Category = KeywordCategory....,
      HasParameter = ...,
      ParameterType = ...,                 // omit when HasParameter == false
      CreateExpansion = ... => new StaticAbility { ... },
    };

  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    // ↓ verbatim from OracleParsers.<Name>, with the two substitutions below
    from keyword in Keyword("...")
    from reminder in OptionalReminder
    select new StaticAbility { ... }
  );

  // Inline any private helper the Definition used (e.g. ParseIntValue, ParseManaCost)
  // as a private static method here — do NOT reach back into KeywordDefinitions.
}
```

### Two mechanical substitutions when copying the combinator

The legacy combinator used `OracleParsers`-private helpers. Swap them for the shared
`KeywordCombinators` members (imported via `using static`):

| Legacy (OracleParsers private) | Use instead (KeywordCombinators) |
| --- | --- |
| `Keyword("X")` | `Keyword("X")` (same call — now the shared one) |
| `_optionalReminder` | `OptionalReminder` |
| the inline `Token.Matching<OracleToken>(k => k == GenericMana \|\| ...).AtLeastOnce().Select(... ManaCostParser ...)` mana block | `ManaCostSymbols` (yields a `ManaCost` directly) |

`Token.EqualTo(OracleToken.Number)`, `OracleToken.Comma`, etc. stay as-is (add
`using Superpower.Parsers;`).

### Inline the Definition's helpers

Legacy `KeywordDefinitions` shared private helpers (`ParseIntValue`, `ParseManaCost`,
`ParseCrewPower`, `ParseProtectionQualities`, `BuildAffinityFilter`, …). Copy whichever
your Definition calls into a `private static` method on your class. The exemplars show
this:

- `ToxicKeyword` inlines `ParseIntValue`.
- `BuybackKeyword` inlines `ParseManaCost`.

## `Tier` decision rule

- `KeywordTier.Simple` → the legacy combinator lived in the `SimpleKeyword` Or-chain
  (parameterless: `Flying`, `First strike`, `Vigilance`, …).
- `KeywordTier.Parameterized` → it lived in the `ParameterizedKeyword` Or-chain
  (carries a number / mana cost / quality / name: `Toxic`, `Buyback`, `Protection`,
  `Partner with`, …).

The registry builds `Simple.Try().Or(Parameterized)` exactly like
`OracleParsers.AnyKeyword`, so getting the tier right reproduces the legacy two-chain
split.

## `[Keyword]` priority — only when ordering matters

Within a tier the registry folds combinators in **descending priority, then ordinal
class-name**. Superpower's `.Or` is first-success-wins (each candidate is wrapped in
`.Try()` so it backtracks), so a keyword whose oracle text is a **prefix of another
keyword's** must be tried **first** — give it a higher `Priority`.

- Default `[Keyword]` (priority 50) is correct for the overwhelming majority — any
  keyword whose leading token is unique.
- Bump priority (60–100) when your keyword shares a leading token with a shorter
  sibling. Canonical case: **`Partner with [Name]` must outrank bare `Partner`** —
  give `PartnerWithKeyword` a higher priority than `PartnerKeyword`, otherwise
  `Partner` matches first and `with [Name]` is left dangling.
- Lower priority (0–40) for deliberately-last catch-alls (e.g. a generic
  `[Type]cycling` matcher that must yield to specific cycling variants).

Multi-word keywords (`First strike`, `Cumulative upkeep`, `Split second`) are matched
by chaining `Keyword(...)` calls (see `FirstStrikeKeyword`); they only need a priority
bump if their first word collides with another keyword's.

## Before you finish

- Build: `dotnet build` in `libs/magic-ast`.
- Test: `dotnet test` in `tests/magic-ast-tests` (or `nx run mast:test`). Must stay
  **fully green** — every registry combinator must produce output byte-identical to the
  legacy one it shadows. If a fixture changes, your copy diverged: re-check the
  `KeywordSource` string, the effect node, and the `Reminder` wiring.

## Exemplars to copy from (this folder)

| Shape | File |
| --- | --- |
| Simple, parameterless | `FlyingKeyword.cs` |
| Simple, multi-word | `FirstStrikeKeyword.cs` |
| Parameterized, integer | `ToxicKeyword.cs` |
| Parameterized, mana cost | `BuybackKeyword.cs` |
