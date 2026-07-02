namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a bare "Unleash" oracle line into the two static abilities defined
/// by CR 702.98a (mirroring the <see cref="DashStaticRule"/> /
/// <see cref="ReconfigureStaticRule"/> multi-ability precedent).
///
/// <para>
/// CR 702.98a (verbatim): "Unleash is a keyword that represents two static
/// abilities. 'Unleash' means 'You may have this permanent enter with an
/// additional +1/+1 counter on it' and 'This permanent can't block as long as it
/// has a +1/+1 counter on it.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so
/// the two-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in
/// <see cref="MagicAST.Keywords.Definitions.UnleashKeyword"/> remains live as a
/// fallback, emitting only the primary enters-with-counter static ability (the
/// keyword expander returns a single <c>Ability</c>; Unleash decomposes into two).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class UnleashStaticRule : IStaticRule
{
  // Matches: a bare "Unleash" line with optional trailing reminder text.
  private static readonly Regex _pattern = new(
    @"^\s*Unleash\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.98a, first clause): the optional enters-with-counter static
    // replacement ability. "You may have this permanent enter with an additional
    // +1/+1 counter on it." Timing ("as this enters") lives on the StaticAbility.When
    // qualifier (CR 603.6d/614.1c), never baked into the effect; the "you may" choice
    // is the OptionalEffect wrapper (EffectWrap.Optional). The reminder rides on this
    // primary ability, matching the combinator path.
    var entersWithCounterAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Unleash,
      When = StaticTimingKind.AsThisEnters,
      Reminder = reminder,
      Effects =
      [
        EffectWrap.Optional(
          new PutCountersEffect
          {
            Target = ObjectReference.Self(),
            CounterType = "+1/+1",
            Count = LiteralQuantity.Of(1),
          },
          isOptional: true
        ),
      ],
    };

    // Ability 2 (CR 702.98a, second clause): the conditional can't-block static
    // ability. "This permanent can't block as long as it has a +1/+1 counter on it."
    // The "as long as" condition rides on the CantBlockEffect's ContinuousEffect
    // Duration (mirroring AsLongAsStaticGrantRule), never on the StaticAbility. The
    // condition "it has a +1/+1 counter on it" has no structured arm yet and parses
    // to the typed OtherCondition residual.
    var cantBlockAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Unleash,
      Effects =
      [
        new CantBlockEffect
        {
          Duration = new AsLongAsDuration
          {
            Condition = ConditionParser.Parse("it has a +1/+1 counter on it"),
          },
        },
      ],
    };

    return [entersWithCounterAbility, cantBlockAbility];
  }
}
