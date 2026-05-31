namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Menace: This creature can't be blocked except by two or more creatures.
/// Rule 702.111. Evasion keyword whose distinguishing feature is a minimum
/// blocker count rather than a characteristic filter on the blockers.
/// </summary>
[Keyword]
public sealed class MenaceKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Menace",
      RuleReference = "702.111",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Menace,
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
          MinimumBlockers = 2,
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Menace")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Menace,
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
        MinimumBlockers = 2,
      }],
      Reminder = reminder,
    }
  );
}
