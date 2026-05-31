namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Surge {cost} (CR 702.117a): "Surge [cost] means 'You may pay [cost] rather than pay
/// this spell's mana cost as you cast this spell if you or one of your teammates has cast
/// another spell this turn.'" It is a static ability, so the combinator emits a
/// <see cref="StaticAbility"/> carrying the shared <see cref="AlternativeCastEffect"/>
/// primitive (<c>FromZone = Hand</c>, <c>Cost =</c> the surge mana cost,
/// <c>Condition =</c> the cast-history requirement as an <see cref="OtherCondition"/>
/// residual). Decomposed per ADR-0003: the opaque <c>SurgeEffect</c> marker is replaced
/// by the shared alternative-cast primitive that Flashback, Escape, Madness etc. also use.
/// </summary>
[Keyword]
public sealed class SurgeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Surge")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Surge,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Hand,
        Cost = cost,
        Condition = new OtherCondition
        {
          Text = "you or one of your teammates has cast another spell this turn",
        },
      }],
      Reminder = reminder,
    }
  );
}
