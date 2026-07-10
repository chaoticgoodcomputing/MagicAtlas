namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When enchanted Swamp is put into a graveyard" — the Kamigawa "Genju" cycle's
/// self-return trigger condition. The Aura's enchant restriction is a single basic
/// land type (CR 305.6), so the trigger subject is "enchanted [BasicLandType]" rather
/// than the generic "enchanted land"/"enchanted creature" noun handled elsewhere
/// (<see cref="TriggeredRuleHelpers.ParseObjectFilter"/>'s "enchanted creature"
/// branch). The attachment is recorded on <see cref="ObjectFilter.IsEnchanted"/>
/// (CR 303.4c / 702.5); the card type is "land" and the named subtype is the printed
/// basic land type word.
///
/// <para>
/// Maps to <see cref="TriggerEvent.PutIntoGraveyard"/> rather than
/// <see cref="TriggerEvent.Dies"/> — the oracle text says "is put into a graveyard"
/// verbatim, without the "from the battlefield" longform that
/// <see cref="DiesConditionRule"/> requires (CR 700.4 defines "dies" specifically as
/// that longform; this line does not use it).
/// </para>
///
/// <para>
/// Canonical card: Genju of the Fens (DIS) — "When enchanted Swamp is put into a
/// graveyard, you may return this card from your graveyard to your hand." The
/// effect half is handled separately by
/// <see cref="ReturnSelfFromGraveyardTriggeredRule"/> (identical effect text to the
/// Eidolon cycle).
/// </para>
///
/// <para>
/// Priority 994 — above <see cref="SubtypePutIntoGraveyardConditionRule"/> (993),
/// <see cref="DiesConditionRule"/> (991) and <see cref="PutIntoGraveyardConditionRule"/>
/// (985), so this more specific "enchanted [BasicLandType]" shape is tried first.
/// Anchored pattern (whole trigger-condition string) prevents substring collisions
/// with longer/differently-scoped siblings.
/// </para>
///
/// Rule 603.2 (Triggered Abilities); Rule 404 (Graveyard); Rule 305.6 (basic land
/// types); Rule 303.4c / 702.5 ("enchanted [type]" refers to the Aura's attached
/// permanent).
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class EnchantedBasicLandTypePutIntoGraveyardConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^\s*(?:whenever|when|at)\s+enchanted\s+(?<subtype>Plains|Island|Swamp|Mountain|Forest)\s+is\s+put\s+into\s+a\s+graveyard\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enchanted") || !lower.Contains("put into") || !lower.Contains("graveyard"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = match.Groups["subtype"].Value;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.PutIntoGraveyard,
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Subtypes = [subtype],
        IsEnchanted = true,
      },
    };
  }
}
