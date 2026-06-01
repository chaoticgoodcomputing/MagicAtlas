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
/// Exalted: Whenever a creature you control attacks alone, that creature gets +1/+1
/// until end of turn.
///
/// CR 702.83a (verbatim): "Exalted is a triggered ability. 'Exalted' means 'Whenever
/// a creature you control attacks alone, that creature gets +1/+1 until end of turn.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Exalted",
///   Trigger:{ Timing:Whenever, Event:Attacks,
///             Filter:{CardTypes:["creature"], Controller:You,
///                     Characteristics:[Other("attacking alone")]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:ThatCreature},
///                             PowerModifier:+1, ToughnessModifier:+1,
///                             Duration:untilEndOfTurn } ] }.
///
/// "attacks alone" is a state predicate with no first-class ObjectFilter field,
/// carried as an OtherCharacteristic residual per ADR 0001 (Mentor precedent).
/// The trigger subject is "a creature you control" (not this creature): Exalted
/// triggers off ANY of the controller's creatures attacking alone. "that creature"
/// maps to ObjectReferenceKind.ThatCreature — the creature named by the trigger's
/// Filter.
/// </summary>
[Keyword]
public sealed class ExaltedKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Exalted")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Exalted,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          Characteristics =
          [
            Characteristic.Other("attacking alone"),
          ],
        },
      },
      Effects =
      [
        new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
          PowerModifier = LiteralQuantity.Of(1),
          ToughnessModifier = LiteralQuantity.Of(1),
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    }
  );
}
