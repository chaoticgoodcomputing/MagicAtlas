namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "this creature or another [Subtype] you control dies" — subtype-filtered dies
/// trigger. The dies analog of <see cref="AnotherSubtypeEntersConditionRule"/>:
/// the trigger fires when a permanent of a specific creature subtype that you
/// control is put into a graveyard from the battlefield.
///
/// <para>CR 603.1: triggered abilities use "when," "whenever," or "at" to watch
/// for an event; the subtype filter narrows which dying permanents fire the
/// ability. CR 700.4: "dies" means "is put into a graveyard from the
/// battlefield." CR 205.3: subtypes (e.g. Vampire, Ally, Zombie) are named on
/// the type line; the disjunction "this creature or another Vampire you control"
/// resolves to a single "Vampire you control" filter that includes the source.
/// </para>
///
/// <para>Examples:
///   "Whenever this creature or another Vampire you control dies, ..." (Kalastria
///   Highborn, WWK).</para>
///
/// <para>Priority 995 (matching the enters analog) so this specific subtype form
/// is tried before the generic <see cref="DiesConditionRule"/> (991), whose
/// <c>ParseObjectFilter</c> would resolve "this creature" to a plain
/// <c>CardTypes=["creature"]</c> filter and silently drop the subtype.</para>
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class AnotherSubtypeDiesConditionRule : ITriggerConditionRule
{
  // Matches "another <Subtype> you control dies". Subtype must be proper-noun
  // (capitalised first letter) to distinguish creature subtypes ("Vampire",
  // "Ally", "Zombie") from type words ("creature", "land") — CR 205.3m. NOT
  // IgnoreCase, so "another creature you control dies" does NOT match here and
  // falls through to DiesConditionRule via ParseObjectFilter.
  private static readonly Regex _pattern = new(
    @"another\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+dies",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("dies"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
