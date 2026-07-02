namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Improvise: for each generic mana in this spell's total cost, you may tap an
/// untapped artifact you control rather than pay that mana.
///
/// <para>
/// CR 702.126a: "Improvise is a static ability that functions while the spell with
/// improvise is on the stack. 'Improvise' means 'For each generic mana in this
/// spell's total cost, you may tap an untapped artifact you control rather than pay
/// that mana.'"
/// </para>
/// <para>
/// CR 702.126b: "The improvise ability isn't an additional or alternative cost and
/// applies only after the total cost of the spell with improvise is determined."
/// </para>
/// <para>
/// MAST models the payment substitution as an <see cref="AlternativePaymentEffect"/>
/// on the static ability (per ADR 0003 keyword-decomposition shape): tap artifacts
/// you control to pay generic mana. "Untapped" is a game-state precondition
/// (engine territory), not a filter axis.
/// </para>
/// </summary>
[Keyword]
public sealed class ImproviseKeyword : IKeyword
{
  private static readonly ObjectFilter _artifactsYouControl = new()
  {
    CardTypes = ["artifact"],
    Controller = ControllerFilter.You,
  };

  private static readonly AlternativePaymentEffect _paymentEffect = new()
  {
    Method = AlternativePaymentMethod.TapObject,
    Pays = AlternativePaymentKind.Generic,
    Source = _artifactsYouControl,
  };

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Improvise",
      RuleReference = "702.126",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = KeywordAbility.Improvise,
        Effects = [_paymentEffect],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Improvise")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Improvise,
      Effects =
      [
        new AlternativePaymentEffect
        {
          Method = AlternativePaymentMethod.TapObject,
          Pays = AlternativePaymentKind.Generic,
          Source = new ObjectFilter
          {
            CardTypes = ["artifact"],
            Controller = ControllerFilter.You,
          },
        },
      ],
      Reminder = reminder,
    }
  );
}
