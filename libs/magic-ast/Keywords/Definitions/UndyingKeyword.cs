namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Undying (CR 702.93, verbatim): "Undying is a triggered ability. 'Undying' means
/// 'When this permanent is put into a graveyard from the battlefield, if it had no
/// +1/+1 counters on it, return it to the battlefield under its owner's control with
/// a +1/+1 counter on it.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Undying",
///   Trigger:{ Timing:"When", Event:"Dies", Filter:{CardTypes:["creature"]} },
///   InterveningIf:{ ConditionType:"other", Text:"it had no +1/+1 counters on it" },
///   Effects:[ ReturnToBattlefieldEffect{
///     Target:{Kind:"It"}, UnderControl:{Kind:"Owner"},
///     WithCounters:{ CounterType:"+1/+1", Count:{QuantityType:"literal", Value:1} }
///   }] }.
///
/// Mirror of Persist (CR 702.78) with opposite counter polarity:
/// Persist checks for no -1/-1 counters; Undying checks for no +1/+1 counters.
/// The intervening-if "if it had no +1/+1 counters on it" is a CR 603.4 condition
/// on the triggered ability, not an effect guard.
/// </summary>
[Keyword]
public sealed class UndyingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Undying",
      RuleReference = "702.93",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => BuildAbility(null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Undying")
    from reminder in OptionalReminder
    select BuildAbility(reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for Undying: when this creature dies,
  /// if it had no +1/+1 counters on it, return it to the battlefield under its
  /// owner's control with a +1/+1 counter on it.
  /// </summary>
  private static Ability BuildAbility(Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Undying,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        // CR 109 self-binding (§6): "this permanent" — the source's own death, so a cross-card
        // sacrifice does not subsume it (operator returns No → bridge falls to Amber, not GREEN).
        Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
      },
      InterveningIf = new OtherCondition { Text = "it had no +1/+1 counters on it" },
      Effects =
      [
        new ReturnToBattlefieldEffect
        {
          Target = ObjectReference.It(),
          UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
          WithCounters = new CounterPlacement
          {
            CounterType = "+1/+1",
            Count = LiteralQuantity.Of(1),
          },
        },
      ],
      Reminder = reminder,
    };
}
