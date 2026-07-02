namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "If an artifact or creature entering causes a triggered ability of a permanent you
/// control to trigger, that ability triggers an additional time."
///
/// <para>
/// This models Panharmonicon's trigger-multiplication static ability. Like Echoes of
/// Eternity (see <see cref="TriggerDoublingStaticRule"/>), the effect is a
/// <see cref="ReplacementEffect"/> over an <see cref="AbilityTriggerEvent"/>. The
/// critical difference: Panharmonicon is scoped to triggered abilities whose cause is
/// "an artifact or creature entering the battlefield" — encoded via
/// <see cref="AbilityTriggerEvent.CausedByEntering"/> with a two-type card-type filter.
/// </para>
///
/// <para>
/// CR 603.2d (verbatim from rules-structure.json):
/// "An ability may state that a triggered ability triggers additional times. In this case,
/// rather than simply determining that such an ability has triggered, determine how many
/// times it should trigger, then that ability triggers that many times."
/// </para>
///
/// <para>
/// <see cref="ReplacementEffect.AffectedObjects"/> = <c>{ CardTypes:["permanent"],
/// Controller: You }</c> — only triggered abilities of <em>permanents you control</em>
/// are doubled. <see cref="AbilityTriggerEvent.CausedByEntering"/> = <c>{ CardTypes:
/// ["artifact", "creature"] }</c> — only when the ETB entering object was an artifact or
/// creature.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class PanharmoniconTriggerDoublingRule : IStaticRule
{
  // Matches: "If an artifact or creature entering causes a triggered ability of a
  // permanent you control to trigger, that ability triggers an additional time."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+an\s+artifact\s+or\s+creature\s+entering\s+causes\s+a\s+triggered\s+ability\s+of\s+a\s+permanent\s+you\s+control\s+to\s+trigger,\s+that\s+ability\s+triggers\s+an\s+additional\s+time\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ReplacementEffect
          {
            // The event being replaced/augmented: a triggered ability of a permanent
            // you control fires due to an artifact or creature entering. AffectedObjects
            // filters the SOURCE of the watched triggered ability — permanent you control.
            Event = new AbilityTriggerEvent
            {
              AffectedObjects = new ObjectFilter
              {
                CardTypes = ["permanent"],
                Controller = ControllerFilter.You,
              },
              // Panharmonicon scope restriction: only ETB-caused triggers, where the
              // entering object was an artifact or creature.
              CausedByEntering = new ObjectFilter
              {
                CardTypes = ["artifact", "creature"],
              },
            },
            // The original trigger occurrence still fires once; "triggers an
            // additional time" adds one more occurrence on top.
            OriginalEventOccurs = true,
            Modifier = new ReplacementModifier
            {
              Type = "additionalTime",
            },
          },
        ],
      },
    ];
  }
}
