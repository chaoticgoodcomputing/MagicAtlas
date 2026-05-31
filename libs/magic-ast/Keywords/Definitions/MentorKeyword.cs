namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mentor: Whenever this creature attacks, put a +1/+1 counter on target attacking
/// creature with power less than this creature's power.
///
/// CR 702.134 (verbatim): "Mentor is a triggered ability. 'Mentor' means 'Whenever
/// this creature attacks, put a +1/+1 counter on target attacking creature with power
/// less than this creature's power.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Mentor",
///   Trigger:{ Timing:Whenever, Event:Attacks, Filter:{CardTypes:["creature"]} },
///   Effects:[ PutCountersEffect{ Target:{Kind:Target,
///     Filter:{CardTypes:["creature"],
///             Characteristics:[Other("attacking"),
///                              Other("with power less than this creature's power")]}},
///     CounterType:"+1/+1", Count:1 } ] }.
///
/// "Attacking" and "with power less than this creature's power" are relative/state
/// predicates that do not yet have first-class ObjectFilter fields, so they are
/// carried as OtherCharacteristic residuals per the ADR 0001 free-text doctrine
/// and the existing CantBeBlockedRule convention (CantBeBlockedRule.cs line 168).
/// </summary>
[Keyword]
public sealed class MentorKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Mentor")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = "Mentor",
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new PutCountersEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics =
              [
                Characteristic.Other("attacking"),
                Characteristic.Other("with power less than this creature's power"),
              ],
            },
          },
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(1),
        },
      ],
      Reminder = reminder,
    }
  );
}
