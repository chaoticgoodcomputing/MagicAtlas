namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Freerunning [cost] — CR 702.173a: "Freerunning is a static ability that functions
/// on the stack. 'Freerunning [cost]' means 'You may pay [cost] rather than pay this
/// spell's mana cost if a player was dealt combat damage this turn by a creature that,
/// at the time it dealt that damage, was an Assassin creature or a commander under
/// your control.' Casting a spell for its freerunning cost follows the rules for
/// paying alternative costs in rules 601.2b and 601.2f-h."
///
/// <para>
/// Emits a <see cref="StaticAbility"/> carrying an <see cref="AlternativeCastEffect"/>
/// with <c>FromZone = Zone.Hand</c> and a hard-coded <see cref="OtherCondition"/>
/// residual carrying the verbatim CR 702.173a gate text — the established pattern
/// for Surge/Spectacle conditional cast permissions.
/// </para>
/// </summary>
[Keyword]
public sealed class FreerunningKeyword : IKeyword
{
  private const string ConditionText =
    "a player was dealt combat damage this turn by a creature that, at the time it dealt that damage, was an Assassin creature or a commander under your control";

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Freerunning")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Freerunning,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
          Condition = ConditionParser.Parse(ConditionText),
        },
      ],
      Reminder = reminder,
    }
  );
}
