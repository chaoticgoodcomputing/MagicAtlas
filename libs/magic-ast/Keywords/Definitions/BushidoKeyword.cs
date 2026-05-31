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
/// Bushido N: Whenever this creature blocks or becomes blocked, it gets +N/+N
/// until end of turn.
///
/// CR 702.45 (verbatim): "Bushido is a triggered ability. 'Bushido N' means
/// 'Whenever this creature blocks or becomes blocked, it gets +N/+N until end
/// of turn.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Bushido",
///   Trigger:{ Timing:"Whenever", Event:"BlocksOrBecomesBlocked",
///             Filter:{CardTypes:["creature"]} },
///   Effects:[ ModifyPTEffect{ Target:{Kind:"It"}, PowerModifier:N,
///                             ToughnessModifier:N, Duration:untilEndOfTurn } ] }.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class BushidoKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Bushido")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Bushido N": whenever this
  /// creature blocks or becomes blocked, it gets +N/+N until end of turn.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Bushido,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BlocksOrBecomesBlocked,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.It },
          PowerModifier = LiteralQuantity.Of(value),
          ToughnessModifier = LiteralQuantity.Of(value),
          Duration = UntilTimeDuration.EndOfTurn,
        },
      ],
      Reminder = reminder,
    };
}
