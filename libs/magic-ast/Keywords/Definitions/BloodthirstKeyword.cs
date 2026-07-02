namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Bloodthirst N: static ability on the permanent that causes it to enter with
/// N +1/+1 counters when the condition is met.
///
/// CR 702.54a (verbatim): "Bloodthirst is a static ability. 'Bloodthirst N' means
/// 'If an opponent was dealt damage this turn, this permanent enters with N +1/+1
/// counters on it.'"
///
/// MAST shape (ADR-0003 decomposition):
///   StaticAbility{
///     KeywordSource: Bloodthirst,
///     When: AsThisEnters,
///     Condition: OtherCondition{ Text: "an opponent was dealt damage this turn" },
///     Effects: [ PutCountersEffect{
///       Target: Self, CounterType: "+1/+1", Count: LiteralQuantity(N) } ] }
///
/// The condition "an opponent was dealt damage this turn" has no structured node
/// in the current corpus; ConditionParser.Parse falls through to the typed
/// OtherCondition residual (acceptable per ADR 0001 — residual-debt, not unparsed).
/// Template: KickerConditionalEntersWithCountersRule (StaticAbility{When:AsThisEnters,
/// Condition, PutCountersEffect}).
/// </summary>
[Keyword]
public sealed class BloodthirstKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Bloodthirst",
      RuleReference = "702.54",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = KeywordAbility.Bloodthirst,
        When = StaticTimingKind.AsThisEnters,
        Condition = ConditionParser.Parse("an opponent was dealt damage this turn"),
        Effects = [new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(ParseIntValue("Bloodthirst", parameter)),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Bloodthirst")
    from value in Superpower.Parsers.Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Bloodthirst,
      When = StaticTimingKind.AsThisEnters,
      Condition = ConditionParser.Parse("an opponent was dealt damage this turn"),
      Effects = [new PutCountersEffect
      {
        Target = ObjectReference.Self(),
        CounterType = "+1/+1",
        Count = LiteralQuantity.Of(int.Parse(value.ToStringValue())),
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Integer-parameter guard used by the Definition's CreateExpansion factory.
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
