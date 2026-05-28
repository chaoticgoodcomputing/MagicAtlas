namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Prowess: Whenever you cast a noncreature spell, this creature gets +1/+1 until end of turn.
/// Rule 702.108. Modeled as a keyword marker per MAST convention;
/// the trigger-and-buff expansion is engine territory.
/// </summary>
[Keyword]
public sealed class ProwessKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Prowess")
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Prowess",
      Effects = [new ProwessEffect()],
      Reminder = reminder,
    }
  );
}
