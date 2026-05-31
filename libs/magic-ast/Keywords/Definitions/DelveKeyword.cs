namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Delve — a static ability that allows exile of graveyard cards to pay for generic mana
/// while casting the spell.
///
/// <para>
/// CR 702.66a: "Delve is a static ability that functions while the spell with delve is on
/// the stack. 'Delve' means 'For each generic mana in this spell's total cost, you may exile
/// a card from your graveyard rather than pay that mana.'"
/// </para>
/// <para>
/// CR 702.66b: "The delve ability isn't an additional or alternative cost and applies only
/// after the total cost of the spell with delve is determined."
/// </para>
/// </summary>
[Keyword]
public sealed class DelveKeyword : IKeyword
{
  private static readonly AlternativePaymentEffect DelveEffect = new()
  {
    Method = AlternativePaymentMethod.Exile,
    Pays = AlternativePaymentKind.Generic,
    Source = new ObjectFilter
    {
      CardTypes = ["card"],
      Controller = ControllerFilter.You,
      Zone = Zone.Graveyard,
    },
  };

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Delve",
      RuleReference = "702.66",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Delve,
        Effects = [DelveEffect],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Delve")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Delve,
      Effects = [new AlternativePaymentEffect
      {
        Method = AlternativePaymentMethod.Exile,
        Pays = AlternativePaymentKind.Generic,
        Source = new ObjectFilter
        {
          CardTypes = ["card"],
          Controller = ControllerFilter.You,
          Zone = Zone.Graveyard,
        },
      }],
      Reminder = reminder,
    }
  );
}
