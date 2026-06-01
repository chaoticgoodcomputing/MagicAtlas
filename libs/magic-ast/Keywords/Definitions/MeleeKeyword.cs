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
/// Melee: Whenever this creature attacks, it gets +1/+1 until end of turn for each
/// opponent you attacked with a creature this combat.
///
/// CR 702.121a (verbatim): "Melee is a triggered ability. 'Melee' means 'Whenever
/// this creature attacks, it gets +1/+1 until end of turn for each opponent you
/// attacked with a creature this combat.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Melee",
///   Trigger:{ Timing:"Whenever", Event:"Attacks",
///             Filter:{CardTypes:["creature"]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:"It"},
///     PowerModifier:CalculatedQuantity{Expression:"for each opponent you attacked with a creature this combat"},
///     ToughnessModifier:CalculatedQuantity{Expression:"for each opponent you attacked with a creature this combat"},
///     Duration:untilEndOfTurn } ] }.
///
/// The per-opponent amount is not a simple integer or a CountQuantity (the
/// "opponents attacked" set is a game-state query outside MAST's ObjectFilter
/// scope); it is carried as a CalculatedQuantity{Expression} per the ADR
/// free-text residual doctrine for quantities not yet expressible structurally.
/// </summary>
[Keyword]
public sealed class MeleeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  private static readonly CalculatedQuantity PerOpponent = new()
  {
    Expression = "for each opponent you attacked with a creature this combat",
  };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Melee")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Melee,
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
          Target = new ObjectReference { Kind = ObjectReferenceKind.It },
          PowerModifier = new CalculatedQuantity
          {
            Expression = "for each opponent you attacked with a creature this combat",
          },
          ToughnessModifier = new CalculatedQuantity
          {
            Expression = "for each opponent you attacked with a creature this combat",
          },
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    }
  );
}
