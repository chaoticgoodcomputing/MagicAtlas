namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Persist: triggered dies-ability that returns this permanent to the battlefield with
/// a -1/-1 counter, gated by an intervening-if checking for no existing -1/-1 counters.
///
/// CR 702.79a (verbatim): "Persist is a triggered ability. 'Persist' means 'When this
/// permanent is put into a graveyard from the battlefield, if it had no -1/-1 counters
/// on it, return it to the battlefield under its owner's control with a -1/-1 counter
/// on it.'"
///
/// MAST shape: TriggeredAbility{ KeywordSource:"Persist",
///   Trigger:{ Timing:When, Event:Dies, Filter:{ Kind:Self } },
///   InterveningIf:{ ConditionType:"other",
///     Text:"it had no -1/-1 counters on it" } (CR 603.4 intervening-if),
///   Effects:[ ReturnToBattlefieldEffect{
///     Target:{ Kind:Self },
///     UnderControl:{ Kind:Owner },
///     WithCounters:{ CounterType:"-1/-1", Count:1 } } ] }
/// </summary>
[Keyword]
public sealed class PersistKeyword : IKeyword
{
  private const string InterveningIfText = "it had no -1/-1 counters on it";

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Persist")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Persist,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        Filter = new MagicAST.AST.References.ObjectFilter { CardTypes = ["permanent"] },
      },
      InterveningIf = new OtherCondition
      {
        Text = InterveningIfText,
      },
      Effects =
      [
        new ReturnToBattlefieldEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
          WithCounters = new CounterPlacement
          {
            CounterType = "-1/-1",
            Count = LiteralQuantity.Of(1),
          },
        },
      ],
      Reminder = reminder,
    }
  );
}
