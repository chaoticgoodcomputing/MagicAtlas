namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Recognises the "a [Subtype] is put into a graveyard from the battlefield" trigger
/// condition — the longform "dies" trigger (CR 700.4) qualified by an artifact
/// subtype rather than a card type (e.g. "Food", "Treasure", "Blood", "Clue").
///
/// <para>
/// CR 700.4 (verbatim): "The word 'dies' means 'is put into a graveyard from the
/// battlefield.'"  Subtype-qualified dies triggers fire on the same event as creature
/// dies triggers, but the filter names an artifact subtype (e.g. Food, CR 111.10b)
/// rather than the card type "creature". MAST maps to <see cref="TriggerEvent.Dies"/>
/// because the zone-change is identical; the filter carries
/// <see cref="ObjectFilter.Subtypes"/> to record the named subtype faithfully.
/// </para>
///
/// <para>
/// Canonical card: Ygra, Eater of All (BLB) — "Whenever a Food is put into a graveyard
/// from the battlefield, put two +1/+1 counters on Ygra."
/// CR 700.4 (Dies = put into graveyard from battlefield).
/// CR 111.10b (Food token is a colorless Food artifact token).
/// </para>
///
/// <para>
/// Priority 993 — above <see cref="DiesConditionRule"/> (991) and above
/// <see cref="NontokenCreatureToGraveyardFromBattlefieldConditionRule"/> (992) so this
/// more-specific subtype-qualified shape fires before the generic "dies" fallback.
/// Anchored pattern prevents substring matches against longer, more-specific siblings.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 993)]
public sealed class SubtypePutIntoGraveyardConditionRule : ITriggerConditionRule
{
  // Matches: "Whenever/When/At a [Subtype] is put into a graveyard from the battlefield"
  // ParseTriggerCondition passes the FULL triggerPart including the leading timing word
  // ("Whenever", "When", "At") — the pattern must consume that prefix.
  // [Subtype] is a capitalised word (PascalCase artifact/creature subtypes per CR 205.3).
  // Anchored at end ($) to prevent matching against extended trigger conditions that add
  // qualifiers after "battlefield" (e.g. "...from the battlefield this turn").
  private static readonly Regex _pattern = new(
    @"^\s*(?:whenever|when|at)\s+a\s+(?<subtype>[A-Z][A-Za-z]*)\s+is\s+put\s+into\s+a\s+graveyard\s+from\s+the\s+battlefield\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick reject: must contain the dies longform.
    if (!lower.Contains("is put into a graveyard from the battlefield"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    // Preserve PascalCase from oracle text — subtype names are proper-noun-ish
    // (Food, Treasure, Blood, Clue…) per CR 205.3.
    var subtype = m.Groups["subtype"].Value;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
      },
    };
  }
}
