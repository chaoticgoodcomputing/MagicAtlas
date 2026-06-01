namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Training: Whenever this creature and at least one other creature with power greater
/// than this creature's power attack, put a +1/+1 counter on this creature.
///
/// CR 702.149a (verbatim): "Training is a triggered ability. 'Training' means 'Whenever
/// this creature and at least one other creature with power greater than this creature's
/// power attack, put a +1/+1 counter on this creature.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Training",
///   Trigger:{ Timing:Whenever, Event:Attacks, Filter:{CardTypes:["creature"]} },
///   InterveningIf:OtherCondition{ Text:"this creature attacks with at least one other
///     creature with greater power" },
///   Effects:[ PutCountersEffect{ Target:{Kind:Self}, CounterType:"+1/+1", Count:1 } ] }.
///
/// "At least one other creature with greater power" is a relative/state predicate
/// with no first-class ObjectFilter field; carried as OtherCondition residual per
/// the ADR 0001 free-text doctrine.
/// </summary>
[Keyword]
public sealed class TrainingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Training")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Training,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      InterveningIf = ConditionParser.Parse(
        "this creature attacks with at least one other creature with greater power"),
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(1),
        },
      ],
      Reminder = reminder,
    }
  );
}
