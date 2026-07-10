namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this Equipment becomes unattached from a permanent" — Stitcher's Graft.
///
/// <para>
/// CR 701.3d (verbatim): "To 'unattach' an Equipment from a creature means to move it
/// away from that creature so the Equipment is on the battlefield but is not equipping
/// anything. It should no longer be physically touching any creature. If an Aura,
/// Equipment, or Fortification that was attached to an object or player ceases to be
/// attached to it, that counts as 'becoming unattached [from that object or player]';
/// this includes if that Aura, Equipment, or Fortification leaves the battlefield, the
/// object leaves the zone it was in, or that player leaves the game."
/// </para>
///
/// <para>
/// Emits <see cref="TriggerEvent.BecomesUnattached"/>. The <see cref="TriggerCondition.Filter"/>
/// carries the "from a permanent" complement — the object the source became unattached
/// from — NOT the source ("this Equipment") itself, mirroring the Flanking convention
/// where the trigger's Filter names the OTHER object needed for a later back-reference
/// ("that permanent" → <see cref="ObjectReferenceKind.ThatPermanent"/>).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 50)]
public sealed class BecomesUnattachedConditionRule : ITriggerConditionRule
{
  // "becomes unattached from a/the <filter>"
  private static readonly Regex _pattern = new(
    @"\bbecomes\s+unattached\s+from\s+(?:a|an|the)\s+(?<filter>[A-Za-z]+)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var filterWord = match.Groups["filter"].Value.ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesUnattached,
      Filter = new ObjectFilter { CardTypes = [filterWord] },
    };
  }
}
