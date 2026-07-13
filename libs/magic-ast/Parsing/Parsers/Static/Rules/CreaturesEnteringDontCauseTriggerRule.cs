namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// "Creatures entering don't cause abilities to trigger." (Torpor Orb)
///
/// <para>
/// This is the suppressing mirror of the trigger-multiplication statics handled by
/// <see cref="PanharmoniconTriggerDoublingRule"/> and <see cref="TriggerDoublingStaticRule"/>:
/// both model a static ability that intercepts the event of a triggered ability triggering,
/// via a <see cref="ReplacementEffect"/> wrapping an <see cref="AbilityTriggerEvent"/>. Where
/// Panharmonicon scopes <see cref="AbilityTriggerEvent.CausedByEntering"/> and then AUGMENTS
/// the trigger occurrence (<c>OriginalEventOccurs = true</c>, <c>Modifier.Type =
/// "additionalTime"</c>), Torpor Orb scopes the identical <c>CausedByEntering</c> filter
/// (creatures) and instead SUPPRESSES the occurrence entirely: <c>OriginalEventOccurs =
/// false</c> with no <see cref="ReplacementEffect.Replacement"/> — the watched
/// triggering-event is replaced with nothing, mirroring the "Skip your draw step" pattern
/// (CR 614.10: "'Skip [something]' is the same as 'Instead of doing [something], do
/// nothing.'").
/// </para>
///
/// <para>
/// CR 603.2 (verbatim, cited in the dispatch brief): "Whenever a game event or game state
/// matches a triggered ability's trigger event, that ability automatically triggers." Torpor
/// Orb is a continuous static ability (CR 611/604 static-ability territory, not itself
/// triggered) that watches for the CR 603.2 trigger-matching event — specifically, any
/// creature entering the battlefield causing some OTHER triggered ability to want to trigger
/// — and replaces that occurrence with nothing, so the would-be triggered ability never goes
/// on the stack. No <see cref="ReplacementEvent.AffectedObjects"/> filter is present because
/// the suppression is unscoped as to WHOSE abilities are affected ("abilities", not "abilities
/// of permanents you control"); <see cref="AbilityTriggerEvent.CausedByEntering"/> = <c>{
/// CardTypes: ["creature"] }</c> narrows the scope to triggers caused by a CREATURE entering
/// (as opposed to Panharmonicon's "artifact or creature").
/// </para>
///
/// <para>
/// MAST describes the scope and suppression of the trigger-matching event; it does not model
/// the trigger/stack engine itself (descriptive-not-engine doctrine).
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CreaturesEnteringDontCauseTriggerRule : IStaticRule
{
  // Matches: "Creatures entering don't cause abilities to trigger."
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+entering\s+don't\s+cause\s+abilities\s+to\s+trigger\.?\s*$",
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
            // The event being replaced: some other triggered ability would trigger
            // because a creature entered the battlefield. CausedByEntering narrows the
            // scope to creature-entering-caused triggers; no AffectedObjects filter
            // because "abilities" is unscoped as to controller.
            Event = new AbilityTriggerEvent
            {
              CausedByEntering = new ObjectFilter
              {
                CardTypes = ["creature"],
              },
            },
            // The would-be trigger occurrence is fully suppressed — it never happens.
            OriginalEventOccurs = false,
          },
        ],
      },
    ];
  }
}
