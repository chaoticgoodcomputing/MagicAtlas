namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Convoke (CR 702.51a/b): "Convoke is a static ability that functions while the
/// spell with convoke is on the stack. 'Convoke' means 'For each colored mana in
/// this spell's total cost, you may tap an untapped creature of that color you control
/// rather than pay that mana. For each generic mana in this spell's total cost, you
/// may tap an untapped creature you control rather than pay that mana.'"
///
/// CR 702.51b: "The convoke ability isn't an additional or alternative cost and
/// applies only after the total cost of the spell with convoke is determined."
/// Accordingly, this is modeled as an <see cref="AlternativePaymentEffect"/> on a
/// static ability — NEVER in a cost slot.
///
/// <see cref="AlternativePaymentKind.Generic"/> is the shared baseline (one tapped
/// creature pays {1}); <see cref="AlternativePaymentEffect.ColorMustMatchMana"/> =
/// <c>true</c> is Convoke's discriminator (a creature may instead pay one mana of
/// its own color — CR 702.51a).
/// </summary>
[Keyword]
public sealed class ConvokeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Convoke")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Convoke,
      Effects =
      [
        new AlternativePaymentEffect
        {
          Method = AlternativePaymentMethod.TapObject,
          Pays = AlternativePaymentKind.Generic,
          Source = new ObjectFilter
          {
            CardTypes = ["creature"],
            Controller = ControllerFilter.You,
          },
          ColorMustMatchMana = true,
        }
      ],
      Reminder = reminder,
    }
  );
}
