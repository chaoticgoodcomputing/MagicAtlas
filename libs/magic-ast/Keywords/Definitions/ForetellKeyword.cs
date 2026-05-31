namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Foretell {cost}: During your turn, you may pay {2} and exile this card from your
/// hand face down. Cast it on a later turn for its foretell cost. Rule 702.143.
/// MAST records the keyword and the foretell cost; the exile-and-deferred-cast
/// machinery is engine territory.
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class ForetellKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Foretell")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Foretell",
      Effects = [new ForetellEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
