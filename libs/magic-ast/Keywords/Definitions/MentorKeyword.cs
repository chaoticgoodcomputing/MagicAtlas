namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mentor: Whenever this creature attacks, put a +1/+1 counter on target attacking
/// creature with power less than this creature's power.
/// Rule 702.134. Although mechanically a triggered ability, MAST records the
/// keyword's presence only — the trigger / target-selection / counter-placement
/// are engine territory (same approach as Evolve, Flanking, Exalted).
/// </summary>
[Keyword]
public sealed class MentorKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Mentor",
      RuleReference = "702.134",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Mentor",
        Effects = [new MentorEffect()],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from kw in Keyword("Mentor")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Mentor",
      Effects = [new MentorEffect()],
      Reminder = reminder,
    }
  );
}
