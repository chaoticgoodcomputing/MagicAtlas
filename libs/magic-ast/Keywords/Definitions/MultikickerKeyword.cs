namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Multikicker {cost}: You may pay an additional {cost} any number of times as you
/// cast this spell.
/// Rule 702.32c. A Kicker variant where the additional cost may be paid any number
/// of times rather than at most once. MAST records the keyword and the multikicker
/// cost; the "for each time it was kicked" scaling on conditional effects is inferred
/// from the rules (descriptive-not-engine doctrine).
/// </summary>
[Keyword]
public sealed class MultikickerKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Multikicker")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Multikicker",
      Effects = [new MultikickerEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
