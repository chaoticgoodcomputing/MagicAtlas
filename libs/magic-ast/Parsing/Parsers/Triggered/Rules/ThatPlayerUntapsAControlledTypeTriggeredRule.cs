namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "that player untaps a land they control" — the resolution half of an
/// each-player time-trigger (paired with the trigger condition handled by
/// <see cref="PhaseTriggerConditionRule"/>) that lets the triggering player
/// untap one permanent of a given type from among the ones they themselves
/// control. Canonical card: Hokori, Dust Drinker ("At the beginning of each
/// player's upkeep, that player untaps a land they control.").
///
/// <para>
/// The chosen permanent is not targeted (no "target" keyword in the oracle
/// text), so it is modelled as <see cref="ObjectReferenceKind.Any"/> — the
/// indefinite controller-choice reference — restricted to objects the
/// triggering player controls via <see cref="ControllerFilter.ThatPlayer"/>
/// (the player named by the trigger condition, CR 109.5), not the ability's
/// own controller.
/// </para>
///
/// <para>
/// Rule 701.20 (Tap and Untap): "To untap a permanent, rotate it back to the
/// upright position from a sideways position. Only tapped permanents can be
/// untapped."
/// </para>
///
/// <para>
/// Generalizes over the card-type noun (land / creature / artifact /
/// permanent) rather than a card-literal "a land" regex, so a sibling shape
/// with a different subject type is covered by the same rule. ANCHORED
/// (^…$) to prevent matching a substring of a more specific sibling clause.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThatPlayerUntapsAControlledTypeTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^\s*that\s+player\s+untaps\s+a[n]?\s+(?<type>land|creature|artifact|permanent)\s+they\s+control\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var cardType = match.Groups["type"].Value.ToLowerInvariant();

    effect = new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = [cardType],
          Controller = ControllerFilter.ThatPlayer,
        },
      },
    };
    return true;
  }
}
