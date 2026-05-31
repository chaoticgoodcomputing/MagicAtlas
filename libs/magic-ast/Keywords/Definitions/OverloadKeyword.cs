namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Overload {cost}: You may cast this spell for its overload cost. If you do,
/// change its text by replacing all instances of "target" with "each."
/// Rule 702.96. MAST records the keyword and the overload cost; the
/// target-to-each rewrite is engine territory, not a descriptive axis.
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the
/// legacy <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class OverloadKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Overload")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Overload",
      Effects = [new OverloadEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
