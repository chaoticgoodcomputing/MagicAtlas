namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "[Self-name] is put into a graveyard from anywhere" — the self-referential
/// zone-change trigger of the Future Sight anti-mill family (Dread, Guile,
/// Hostility, Purity, Vigor, Worldspine Wurm, Serra Avatar). The card names
/// itself and triggers when it lands in a graveyard from ANY zone, not just the
/// battlefield (so this is broader than <see cref="TriggerEvent.Dies"/>).
///
/// <para>
/// Maps to <see cref="TriggerEvent.PutIntoGraveyard"/> with a self filter
/// (<c>CardTypes = ["creature"]</c>, <c>IsSelf = true</c>). The "creature" type is
/// a DEFAULT for this layer (no type line here); a non-creature self-by-name is
/// retyped downstream by SelfReferenceTypeCorrector in CardParser (CR 201.5) — the
/// same convention <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> uses for the
/// "[Self-name] dies/enters" shapes. IsSelf marks the source-self distinction the
/// interaction operator gates (an arbitrary card put into a graveyard is not provably
/// the source).
/// </para>
///
/// <para>
/// Distinct from <see cref="DiesConditionRule"/>: "dies" / "is put into a graveyard
/// from the battlefield" (CR 700.4) is the battlefield→graveyard event mapped to
/// <see cref="TriggerEvent.Dies"/>; "from anywhere" omits the source-zone restriction
/// (hand, library, stack, exile, battlefield) and so is the broader
/// <see cref="TriggerEvent.PutIntoGraveyard"/> event. Distinct from
/// <see cref="PutIntoGraveyardConditionRule"/>, which recognises the "[type] cards
/// are put into your graveyard from anywhere" batch form (The Gitrog Monster) with a
/// type/controller filter rather than a self-by-name subject.
/// </para>
///
/// <para>
/// Rule citations: CR 603.6 (zone-change triggers), CR 700.4 (dies =
/// battlefield→graveyard, the contrast), CR 404.1 (graveyard zone), CR 201.5
/// (a card naming itself is a self-reference).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class SelfPutIntoGraveyardFromAnywhereConditionRule : ITriggerConditionRule
{
  // "[Name] is put into a graveyard from anywhere"
  // The name begins with a capital letter (oracle self-reference convention,
  // CR 201.3) and may contain letters, digits, apostrophes, hyphens, commas, and
  // spaces (covers epithets such as "Kari Zev, Skyship Raider"). Anchored at start
  // (after the timing keyword is already stripped from triggerText) and at the
  // end of the trigger fragment.
  private static readonly Regex _pattern = new(
    @"^(?<name>[A-Z][A-Za-z0-9 ',\-]*?)\s+is\s+put\s+into\s+a\s+graveyard\s+from\s+anywhere$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Cheap guard before the anchored regex.
    if (!lower.Contains("put into a graveyard from anywhere"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.PutIntoGraveyard,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        IsSelf = true,
      },
    };
  }
}
