namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "If a creature dying causes a triggered ability of a permanent you control to trigger,
/// that ability triggers an additional time."
///
/// <para>
/// This models Drivnod, Carnage Dominus's trigger-multiplication static ability. Like
/// Panharmonicon (see <see cref="PanharmoniconTriggerDoublingRule"/>), the effect is a
/// <see cref="ReplacementEffect"/> over an <see cref="AbilityTriggerEvent"/>. The
/// critical difference: Drivnod is scoped to triggered abilities whose cause is "a
/// creature dying" — encoded via <see cref="AbilityTriggerEvent.CausedByDying"/> with
/// a single-type card-type filter (creature).
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
/// <see cref="ReplacementEffect.AffectedObjects"/> (via
/// <see cref="ReplacementEvent.AffectedObjects"/>) = <c>{ CardTypes: ["permanent"],
/// Controller: You }</c> — only triggered abilities of <em>permanents you control</em>
/// are doubled. <see cref="AbilityTriggerEvent.CausedByDying"/> = <c>{ CardTypes:
/// ["creature"] }</c> — only when the dying object was a creature.
/// </para>
/// </summary>
[StaticRule(Priority = 972)]
public sealed class DrivnodTriggerDoublingRule : IStaticRule
{
  // Matches: "If a creature dying causes a triggered ability of a permanent you control
  // to trigger, that ability triggers an additional time."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+a\s+creature\s+dying\s+causes\s+a\s+triggered\s+ability\s+of\s+a\s+permanent\s+you\s+control\s+to\s+trigger,\s+that\s+ability\s+triggers\s+an\s+additional\s+time\.?\s*$",
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
            // you control fires due to a creature dying. AffectedObjects filters the
            // SOURCE of the watched triggered ability — permanent you control.
            Event = new AbilityTriggerEvent
            {
              AffectedObjects = new ObjectFilter
              {
                CardTypes = ["permanent"],
                Controller = ControllerFilter.You,
              },
              // Drivnod scope restriction: only dying-caused triggers, where the
              // dying object was a creature.
              CausedByDying = new ObjectFilter
              {
                CardTypes = ["creature"],
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
