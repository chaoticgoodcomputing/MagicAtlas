namespace MagicAST.Keywords.Definitions;

using System.Linq;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Firebending N (Avatar: The Last Airbender).
///
/// CR 702.189a (verbatim): "Firebending is a triggered ability. 'Firebending N'
/// means 'Whenever this creature attacks, add N {R}. Until end of combat, you don't
/// lose this mana as steps and phases end.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Firebending",
///   Trigger:{ Timing:"Whenever", Event:"Attacks", Filter:{CardTypes:["creature"]} },
///   Effects:[ AddManaEffect{ Mana: N×"{R}" } ] }. Mirrors Radha, Heir to Keld
///   ("Whenever Radha attacks, add {R}{R}") — the self-attack trigger uses the
///   creature filter, matching the corpus convention.
///
/// The end-of-combat mana persistence ("you don't lose this mana as steps and phases
/// end") is an exception to the CR 500.4 mana-emptying turn-based action — engine
/// state bookkeeping, not described content; the verbatim clause survives in the
/// reminder text. The added {R} is the keyword's DEFINITIONAL mana, printed only in
/// reminder text, so it does NOT enter color identity (CR 903.4c) — color identity
/// is derived from printed text by <c>ColorIdentityDeriver</c>, which never sees it.
/// Variable-value printings ("Firebending X, …") are out of scope.
/// </summary>
[Keyword]
public sealed class FirebendingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Firebending",
      RuleReference = "702.189",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => BuildAbility(ParseIntValue("Firebending", parameter), null),
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Firebending")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select BuildAbility(int.Parse(value.ToStringValue()), reminder)
  );

  /// <summary>
  /// Builds the decomposed triggered ability for "Firebending N": whenever this
  /// creature attacks, add N red mana.
  /// </summary>
  private static Ability BuildAbility(int value, Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = "Firebending",
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.Attacks,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects = [new AddManaEffect { Mana = string.Concat(Enumerable.Repeat("{R}", value)) }],
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
