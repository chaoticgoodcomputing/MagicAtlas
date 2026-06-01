namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Battle cry: Whenever this creature attacks, each other attacking creature gets
/// +1/+0 until end of turn.
///
/// CR 702.91a (verbatim): "Battle cry is a triggered ability. 'Battle cry' means
/// 'Whenever this creature attacks, each other attacking creature gets +1/+0 until
/// end of turn.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"BattleCry",
///   Trigger:{ Timing:Whenever, Event:Attacks, Filter:{CardTypes:["creature"]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:Each,
///     Filter:{CardTypes:["creature"], ExcludeSelf:true,
///             Characteristics:[Other("attacking")]}},
///     PowerModifier:+1, ToughnessModifier:+0, Duration:untilEndOfTurn } ] }.
///
/// "Other" is encoded as ExcludeSelf=true on the ObjectFilter (CR 109.5, same as
/// Champion/Soulbond). "Attacking" is a state predicate without a first-class
/// ObjectFilter field, carried as an OtherCharacteristic residual per ADR 0001.
/// </summary>
[Keyword]
public sealed class BattleCryKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from battle in Keyword("Battle")
    from cry in Keyword("cry")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.BattleCry,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              ExcludeSelf = true,
              Characteristics =
              [
                Characteristic.Other("attacking"),
              ],
            },
          },
          PowerModifier = LiteralQuantity.Of(1),
          ToughnessModifier = LiteralQuantity.Of(0),
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    }
  );
}
