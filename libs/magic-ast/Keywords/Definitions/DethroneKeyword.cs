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
/// Dethrone: Whenever this creature attacks the player with the most life or tied
/// for most life, put a +1/+1 counter on this creature.
///
/// CR 702.105a (verbatim): "Dethrone is a triggered ability. 'Dethrone' means
/// 'Whenever this creature attacks the player with the most life or tied for most
/// life, put a +1/+1 counter on this creature.'"
///
/// MAST shape (ADR 0003 decomposition):
///   TriggeredAbility{
///     KeywordSource: Dethrone,
///     Trigger:{ Timing:Whenever, Event:Attacks, Filter:{CardTypes:["creature"]} },
///     InterveningIf: OtherCondition{ Text: "the player you attacked has the most life
///       or is tied for most life" },
///     Effects:[ PutCountersEffect{
///       Target: Self,
///       CounterType: "+1/+1",
///       Count: LiteralQuantity(1) } ] }
///
/// The "attacks the player with the most life or tied for most life" condition has
/// no structured node; it is modelled via ConditionParser.Parse(...) which yields a
/// typed OtherCondition residual (IResidual, not IUnparsed — acceptable per ADR 0001).
/// </summary>
[Keyword]
public sealed class DethroneKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Dethrone")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Dethrone,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      InterveningIf = ConditionParser.Parse(
        "the player you attacked has the most life or is tied for most life"
      ),
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
