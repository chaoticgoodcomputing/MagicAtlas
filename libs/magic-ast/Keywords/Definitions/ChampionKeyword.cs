namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Champion a [type]: When this enters the battlefield, sacrifice it unless you
/// exile another creature of the named type you control. When this leaves the
/// battlefield, that card returns.
/// Rule 702.71. MAST records the keyword's presence and the creature type parameter;
/// the sacrifice-unless and return mechanics are engine territory.
/// </summary>
[Keyword]
public sealed class ChampionKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Champion",
      RuleReference = "702.71",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.CardType,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = $"Champion a {parameter?.Trim() ?? "creature"}",
        Effects = [new ChampionEffect
        {
          CreatureType = parameter?.Trim() ?? "creature",
          IsOptional = false,
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Champion")
    from article in Token.EqualTo(OracleToken.Word)
                         .Where(t => t.ToStringValue().Equals("a", StringComparison.OrdinalIgnoreCase)
                                  || t.ToStringValue().Equals("an", StringComparison.OrdinalIgnoreCase))
    from typeWords in Token.EqualTo(OracleToken.Word).AtLeastOnce()
    from reminder in OptionalReminder
    let creatureType = string.Join(" ", typeWords.Select(t => t.ToStringValue()))
    select new StaticAbility
    {
      KeywordSource = $"Champion a {creatureType}",
      Effects = [new ChampionEffect { CreatureType = creatureType, IsOptional = false }],
      Reminder = reminder,
    }
  );
}
