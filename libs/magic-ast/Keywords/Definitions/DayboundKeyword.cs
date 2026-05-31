namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Daybound: found on the front faces of day/night double-faced cards.
/// Rule 702.145b. MAST records the keyword's presence and phase; the
/// day/night transformation rules (Rule 731) are engine territory.
/// </summary>
[Keyword]
public sealed class DayboundKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Daybound",
      RuleReference = "702.145",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Daybound",
        Effects = [new DayNightEffect { Phase = DayNightPhase.Daybound }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Daybound")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Daybound",
      Effects = [new DayNightEffect { Phase = DayNightPhase.Daybound }],
      Reminder = reminder,
    }
  );
}
