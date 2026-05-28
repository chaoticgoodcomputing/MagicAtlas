namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Soulshift N: When this creature dies, you may return target Spirit card with mana
/// value N or less from your graveyard to your hand.
/// Rule 702.46. A triggered keyword ability; MAST records the keyword and its integer
/// value. The trigger-and-return expansion is engine territory.
/// </summary>
[Keyword]
public sealed class SoulshiftKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Soulshift")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Soulshift",
      Effects = [new SoulshiftEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );
}
