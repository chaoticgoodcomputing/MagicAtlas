namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mayhem {cost}: A static ability that functions while the card is in the graveyard,
/// allowing it to be cast from there by paying the mayhem cost rather than its mana
/// cost, gated on having discarded the card this turn.
///
/// <para>
/// CR 702.187a: "Mayhem is a static ability that functions while the card with mayhem
/// is in a player's graveyard."
/// </para>
/// <para>
/// CR 702.187b: "'Mayhem [cost]' means 'As long as you discarded this card this turn,
/// you may cast it from your graveyard by paying [cost] rather than paying its mana
/// cost.' Casting a spell using its mayhem ability follows the rules for paying
/// alternative costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Combinator-only keyword: Mayhem has no entry in <c>KeywordDefinitions.All</c>
/// (it is not registered as a <see cref="KeywordDefinition"/>), so
/// <see cref="Definition"/> is <see langword="null"/>.
/// </para>
/// </summary>
[Keyword]
public sealed class MayhemKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Mayhem")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Mayhem,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Graveyard,
        Cost = cost,
        Condition = Condition.Other("you discarded this card this turn"),
      }],
      Reminder = reminder,
    }
  );
}
