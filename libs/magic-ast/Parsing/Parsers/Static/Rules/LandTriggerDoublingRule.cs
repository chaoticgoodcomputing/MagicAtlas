namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "If a land entering causes a triggered ability of a permanent you control to trigger,
/// that ability triggers an additional time."
///
/// <para>
/// This models Ancient Greenwarden's trigger-multiplication static ability — the
/// land-scoped sibling of Panharmonicon (see <see cref="PanharmoniconTriggerDoublingRule"/>,
/// "an artifact or creature entering") and Drivnod (see
/// <see cref="DrivnodTriggerDoublingRule"/>, "a creature dying"). Like both, the effect is
/// a <see cref="ReplacementEffect"/> over an <see cref="AbilityTriggerEvent"/>. The
/// critical difference: Ancient Greenwarden is scoped to triggered abilities whose cause is
/// "a land entering the battlefield" — encoded via
/// <see cref="AbilityTriggerEvent.CausedByEntering"/> with a single-type card-type filter
/// (land).
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
/// are doubled. <see cref="AbilityTriggerEvent.CausedByEntering"/> = <c>{ CardTypes:
/// ["land"] }</c> — only when the entering object was a land.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class LandTriggerDoublingRule : IStaticRule
{
  // Matches: "If a land entering causes a triggered ability of a permanent you control
  // to trigger, that ability triggers an additional time."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+a\s+land\s+entering\s+causes\s+a\s+triggered\s+ability\s+of\s+a\s+permanent\s+you\s+control\s+to\s+trigger,\s+that\s+ability\s+triggers\s+an\s+additional\s+time\.?\s*$",
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
            // you control fires due to a land entering. AffectedObjects filters the
            // SOURCE of the watched triggered ability — permanent you control.
            Event = new AbilityTriggerEvent
            {
              AffectedObjects = new ObjectFilter
              {
                CardTypes = ["permanent"],
                Controller = ControllerFilter.You,
              },
              // Ancient Greenwarden scope restriction: only ETB-caused triggers, where
              // the entering object was a land.
              CausedByEntering = new ObjectFilter
              {
                CardTypes = ["land"],
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
