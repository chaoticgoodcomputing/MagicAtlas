namespace MagicAST.Keywords.Definitions;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Afterlife N — a triggered keyword ability.
///
/// CR 702.135 (verbatim): "Afterlife is a triggered ability. 'Afterlife N' means
/// 'When this permanent is put into a graveyard from the battlefield, create N 1/1
/// white and black Spirit creature tokens with flying.'"
///
/// MAST shape (ADR 0003 decomposition):
/// TriggeredAbility{ KeywordSource:"Afterlife",
///   Trigger:{ Timing:When, Event:Dies, Filter:{CardTypes:["creature"]} },
///   Effects:[ CreateTokenEffect{ Count:N, Token:{ 1/1, W+B, creature, Spirit, [Flying] } } ]
/// }
/// </summary>
[Keyword]
public sealed class AfterlifeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Afterlife",
      RuleReference = "702.135",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => BuildAbility(ParseIntValue("Afterlife", parameter), null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Afterlife")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Afterlife N": when this permanent
  /// dies, create N 1/1 white and black Spirit creature tokens with flying.
  /// CR 702.135.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Afterlife,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new CreateTokenEffect
        {
          Player = ObjectReference.You(),
          Count = LiteralQuantity.Of(value),
          Token = new TokenDefinition
          {
            Power = "1",
            Toughness = "1",
            Colors = ["W", "B"],
            Types = ["creature"],
            Subtypes = ["Spirit"],
            Abilities =
            [
              new StaticAbility
              {
                KeywordSource = KeywordAbility.Flying,
                Effects =
                [
                  new EvasionEffect
                  {
                    CanBeBlockedBy = new ObjectFilter
                    {
                      CardTypes = ["creature"],
                      Characteristics =
                      [
                        Characteristic.HasKeyword(KeywordAbility.Flying),
                        Characteristic.HasKeyword(KeywordAbility.Reach),
                      ],
                    },
                  },
                ],
              },
            ],
            IsCopy = false,
          },
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
