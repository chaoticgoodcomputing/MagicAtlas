namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Soulshift N — a triggered keyword ability.
///
/// CR 702.46a (verbatim): "Soulshift is a triggered ability. 'Soulshift N' means
/// 'When this permanent is put into a graveyard from the battlefield, you may return
/// target Spirit card with mana value N or less from your graveyard to your hand.'"
///
/// "Put into a graveyard from the battlefield" = <see cref="TriggerEvent.Dies"/>
/// (CR 700.4). MAST shape (ADR 0003 decomposition):
/// TriggeredAbility{ KeywordSource:Soulshift,
///   Trigger:{ Timing:When, Event:Dies, Filter:{CardTypes:["permanent"]} },
///   Effects:[ optional(ReturnToHandEffect{ Target:{Kind:Target, Filter:{
///     CardTypes:["card"], Subtypes:["Spirit"], Zone:Graveyard,
///     Owner:You, ManaValueComparison:{Operator:LessThanOrEqual, Value:N} }} }) ]
/// }
/// </summary>
[Keyword]
public sealed class SoulshiftKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Soulshift",
      RuleReference = "702.46",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => BuildAbility(ParseIntValue("Soulshift", parameter), null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Soulshift")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Soulshift N":
  /// when this permanent dies, you may return target Spirit card with mana value
  /// N or less from your graveyard to your hand.
  /// CR 702.46a.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Soulshift,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        Filter = new ObjectFilter { CardTypes = ["permanent"] },
      },
      Effects =
      [
        EffectWrap.Optional(
          new ReturnToHandEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Target,
              Filter = new ObjectFilter
              {
                CardTypes = ["card"],
                Subtypes = ["Spirit"],
                Zone = Zone.Graveyard,
                Owner = ControllerFilter.You,
                ManaValueComparison = new Comparison
                {
                  Operator = ComparisonOperator.LessThanOrEqual,
                  Value = value,
                },
              },
            },
          },
          isOptional: true
        ),
      ],
      Reminder = reminder,
    };

  /// <summary>
  /// Integer-parameter guard.
  /// </summary>
  private static int ParseIntValue(string keywordName, string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException($"{keywordName} requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"{keywordName} parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }
}
