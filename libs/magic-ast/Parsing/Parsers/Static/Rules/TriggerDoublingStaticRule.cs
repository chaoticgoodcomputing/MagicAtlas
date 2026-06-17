namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "If a triggered ability of a colorless spell you control or another colorless permanent
/// you control triggers, that ability triggers an additional time."
///
/// <para>
/// This models the trigger-multiplication static ability on Echoes of Eternity. The effect
/// is a <see cref="ReplacementEffect"/> over an <see cref="AbilityTriggerEvent"/>: the
/// replaced event is "a triggered ability of [colorless thing you control] fires", and the
/// replacement is that it fires one additional time
/// (<see cref="ReplacementEffect.OriginalEventOccurs"/> = true,
/// <see cref="ReplacementEffect.Modifier"/> Type = "additionalTime").
/// </para>
///
/// <para>
/// The "another colorless permanent" excludes the Echoes card itself from the source set
/// (modeled via <see cref="ObjectFilter.ExcludeSelf"/> = true on the AffectedObjects
/// filter). "Colorless spell you control" covers spells on the stack; "another colorless
/// permanent you control" covers permanents on the battlefield. The zone distinction and
/// the exact "another" scope are engine territory; MAST records the colorless +
/// controller-you + exclude-self scope.
/// </para>
///
/// <para>
/// CR 603 (triggered abilities trigger whenever the event they watch for occurs).
/// Rule 105.1: "Colorless is not a color." The IsColorless filter axis encodes
/// colorlessness rather than the Colors axis.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class TriggerDoublingStaticRule : IStaticRule
{
  // Matches: "If a triggered ability of a colorless spell you control or another colorless
  // permanent you control triggers, that ability triggers an additional time."
  private static readonly Regex _pattern = new(
    @"^\s*If\s+a\s+triggered\s+ability\s+of\s+a\s+colorless\s+spell\s+you\s+control\s+or\s+another\s+colorless\s+permanent\s+you\s+control\s+triggers,\s+that\s+ability\s+triggers\s+an\s+additional\s+time\.?\s*$",
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
            // The event being replaced/augmented: a triggered ability of a colorless
            // thing you control fires. AffectedObjects filters the SOURCE of the
            // watched triggered ability — colorless, you control, excluding self
            // (the "another" qualifier for permanents).
            Event = new AbilityTriggerEvent
            {
              AffectedObjects = new ObjectFilter
              {
                IsColorless = true,
                Controller = ControllerFilter.You,
                ExcludeSelf = true,
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
