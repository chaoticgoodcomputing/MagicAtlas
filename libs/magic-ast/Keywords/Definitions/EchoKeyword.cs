namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Echo [cost]: "At the beginning of your upkeep, if this permanent came under your
/// control since the beginning of your last upkeep, sacrifice it unless you pay [cost]."
///
/// CR 702.30a (verbatim): "Echo is a triggered ability. 'Echo [cost]' means 'At the
/// beginning of your upkeep, if this permanent came under your control since the
/// beginning of your last upkeep, sacrifice it unless you pay [cost].'"
///
/// MAST shape: TriggeredAbility{ KeywordSource:"Echo",
///   Trigger:{ Timing:"At", Event:{ Part:"Upkeep", Edge:"Beginning", Whose:"You" } },
///   InterveningIf:{ ConditionType:"other", Text:"this permanent came under your
///     control since the beginning of your last upkeep" } (CR 603.4 intervening-if),
///   Effects:[ PreventableEffect{ Inner:SacrificeEffect{ Target:{Kind:"Self"} },
///     Unless:{ Player:{Kind:"Controller"}, Cost:[echo cost] } } ] }
///
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EchoKeyword : IKeyword
{
  private static readonly GameTime UpkeepTime = new()
  {
    Part = TurnPart.Upkeep,
    Edge = TimeBoundary.Beginning,
    Whose = ControllerFilter.You,
  };

  private const string InterveningIfText =
    "this permanent came under your control since the beginning of your last upkeep";

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Echo")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = "Echo",
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.At,
        Event = new TimeOccurrence { Time = UpkeepTime },
      },
      InterveningIf = new OtherCondition
      {
        Text = InterveningIfText,
      },
      Effects =
      [
        new PreventableEffect
        {
          Inner = new SacrificeEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          },
          Unless = new UnlessClause
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
            Cost = cost,
          },
        },
      ],
      Reminder = reminder,
    }
  );
}
