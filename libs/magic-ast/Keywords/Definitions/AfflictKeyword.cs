namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Afflict N: Whenever this creature becomes blocked, defending player loses N life.
///
/// CR 702.130 (verbatim): "Afflict is a triggered ability. 'Afflict N' means
/// 'Whenever this creature becomes blocked, defending player loses N life.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Afflict",
///   Trigger:{ Timing:"Whenever", Event:"BecomesBlocked",
///             Filter:{CardTypes:["creature"]} },
///   Effects:[ LoseLifeEffect{ Player:{Kind:DefendingPlayer},
///                             Amount:LiteralQuantity(N) } ] }.
/// </summary>
[Keyword]
public sealed class AfflictKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Afflict",
      RuleReference = "702.130",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => BuildAbility(ParseIntValue("Afflict", parameter), null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Afflict")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Afflict N": whenever this
  /// creature becomes blocked, defending player loses N life.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Afflict,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BecomesBlocked,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new LoseLifeEffect
        {
          Amount = LiteralQuantity.Of(value),
          Player = new ObjectReference { Kind = ObjectReferenceKind.DefendingPlayer },
        },
      ],
      Reminder = reminder,
    };

  /// <summary>
  /// Integer-parameter guard, inlined from the former <c>KeywordDefinitions.ParseIntValue</c>.
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
