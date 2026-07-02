namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Recognises the "one or more [type] cards are put into your graveyard from anywhere"
/// trigger condition (Rule 603 — Handling Triggered Abilities; Rule 404 — Graveyard, the
/// destination zone; "put into a graveyard" is a zone change, not a keyword action). Maps
/// to <see cref="TriggerEvent.PutIntoGraveyard"/> with a filter that
/// captures the card type and the controller whose graveyard is the destination.
///
/// <para>
/// The "one or more" qualifier is a threshold condition on the trigger event: the ability
/// fires once per batch-triggering event, regardless of whether one or many qualifying
/// cards entered the graveyard simultaneously. MAST records this descriptively (the filter
/// names the type; the threshold semantics are engine territory).
/// </para>
///
/// <para>
/// "from anywhere" omits a source-zone restriction; the filter carries only the
/// destination (owner's graveyard). A future zone-of-origin axis on the filter could
/// refine this when needed, but the current corpus does not require it.
/// </para>
///
/// <para>
/// Representative cards: The Gitrog Monster (SOI) — "Whenever one or more land cards
/// are put into your graveyard from anywhere, draw a card."
/// Rule citations: CR 603 (Triggered Abilities), CR 404 (Graveyard — the destination zone).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class PutIntoGraveyardConditionRule : ITriggerConditionRule
{
  // Matches the pattern "[type] cards are put into your graveyard [from anywhere/from zone]"
  // within the full trigger text (not anchored at start — the timing word "Whenever" precedes).
  // Named group "type" captures the card type: land, creature, artifact, etc.
  private static readonly Regex _typePattern = new(
    @"\b(?:one\s+or\s+more\s+)?(?<type>land|creature|artifact|enchantment|planeswalker|spell|permanent|card)\s+cards?\s+(?:are|is)\s+put\s+into\s+your\s+graveyard\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("put into") || !lower.Contains("graveyard"))
    {
      return null;
    }

    var m = _typePattern.Match(lower);
    if (!m.Success)
    {
      return null;
    }

    var typeName = m.Groups["type"].Value.ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.PutIntoGraveyard,
      Filter = new ObjectFilter
      {
        CardTypes = [typeName],
        Controller = ControllerFilter.You,
      },
    };
  }
}
