namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Rampage N: Whenever this creature becomes blocked, it gets +N/+N until end of
/// turn for each creature blocking it beyond the first.
///
/// CR 702.23a (verbatim): "Rampage is a triggered ability. \"Rampage N\" means
/// \"Whenever this creature becomes blocked, it gets +N/+N until end of turn for
/// each creature blocking it beyond the first.\" (See rule 509, \"Declare Blockers
/// Step.\")"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Rampage",
///   Trigger:{ Timing:"Whenever", Event:"BecomesBlocked",
///             Filter:{CardTypes:["creature"]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:"It"},
///     PowerModifier:CalculatedQuantity{Operand:N, Operation:"multiply",
///       Expression:"for each creature blocking it beyond the first"},
///     ToughnessModifier:CalculatedQuantity{Operand:N, Operation:"multiply",
///       Expression:"for each creature blocking it beyond the first"},
///     Duration:untilEndOfTurn } ] }.
///
/// The parameter N is captured structurally via <see cref="CalculatedQuantity.Operand"/>
/// (per the type's own worked example: "+2 for each … → Operation=\"multiply\",
/// Operand=2"), while "creatures blocking it beyond the first" is a combat-state
/// query outside MAST's <see cref="ObjectFilter"/> scope and is carried as the
/// free-text <see cref="CalculatedQuantity.Expression"/> residual — identical
/// doctrine to Melee's per-opponent Expression.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class RampageKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Rampage")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Rampage N": whenever this
  /// creature becomes blocked, it gets +N/+N until end of turn for each creature
  /// blocking it beyond the first.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder)
  {
    var calc = new CalculatedQuantity
    {
      Operand = value,
      Operation = "multiply",
      Expression = "for each creature blocking it beyond the first",
    };

    return new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Rampage,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BecomesBlocked,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.It },
          PowerModifier = calc,
          ToughnessModifier = calc,
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    };
  }
}
