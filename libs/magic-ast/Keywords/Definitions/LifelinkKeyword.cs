namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Lifelink: Damage dealt by this creature also causes you to gain that much life.
/// Rule 702.15.
/// </summary>
[Keyword]
public sealed class LifelinkKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Lifelink",
      RuleReference = "702.15",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new LifelinkEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Lifelink")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Lifelink",
      Effects = [new LifelinkEffect()],
      Reminder = reminder,
    }
  );
}
