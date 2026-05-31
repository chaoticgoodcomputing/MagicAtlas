namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Multikicker {cost}: You may pay an additional {cost} any number of times as you cast
/// this spell.
///
/// <para>
/// CR 702.33c: "Multikicker is a variant of the kicker ability. 'Multikicker [cost]'
/// means 'You may pay an additional [cost] any number of times as you cast this spell.'
/// A multikicker cost is a kicker cost."
/// </para>
///
/// <para>
/// Emits a <c>StaticAbility</c> carrying an <see cref="AdditionalCastCostEffect"/> with
/// <c>IsOptional:true</c> ("you may pay") and <c>Repeatable:true</c> ("any number of
/// times"), sharing the same primitive as Kicker, Buyback, Replicate, etc.
/// </para>
/// </summary>
[Keyword]
public sealed class MultikickerKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Multikicker")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Multikicker",
      Effects =
      [
        new AdditionalCastCostEffect
        {
          Cost = cost,
          IsOptional = true,
          Repeatable = true,
        },
      ],
      Reminder = reminder,
    }
  );
}
