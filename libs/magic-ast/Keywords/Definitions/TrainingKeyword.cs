namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Training: Whenever this creature attacks with another creature with greater
/// power, put a +1/+1 counter on this creature. Rule 702.151. MAST records
/// keyword presence; the attack trigger, power comparison, and
/// counter-placement are engine territory.
/// </summary>
[Keyword]
public sealed class TrainingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Training",
      RuleReference = "702.151",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Training",
        Effects = [new TrainingEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Training")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Training",
      Effects = [new TrainingEffect()],
      Reminder = reminder,
    }
  );
}
