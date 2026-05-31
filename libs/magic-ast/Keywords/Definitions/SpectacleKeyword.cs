namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Spectacle [cost] (CR 702.137a): "Spectacle is a static ability that functions on the
/// stack. 'Spectacle [cost]' means 'You may pay [cost] rather than pay this spell's mana
/// cost if an opponent lost life this turn.' Casting a spell for its spectacle cost follows
/// the rules for paying alternative costs in rules 601.2b and 601.2f-h."
///
/// <para>
/// Decomposed to the shared <see cref="AlternativeCastEffect"/> primitive
/// (<c>FromZone = Hand</c>): Spectacle is a cast-from-hand conditional alternative cost,
/// matching the Surge/Freerunning family. The opponent-lost-life precondition is held in
/// an <see cref="OtherCondition"/> residual (per the established Echo/Persist pattern).
/// </para>
/// </summary>
[Keyword]
public sealed class SpectacleKeyword : IKeyword
{
  private const string ConditionText = "an opponent lost life this turn";

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Spectacle")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Spectacle,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Hand,
        Cost = cost,
        Condition = new OtherCondition { Text = ConditionText },
      }],
      Reminder = reminder,
    }
  );
}
