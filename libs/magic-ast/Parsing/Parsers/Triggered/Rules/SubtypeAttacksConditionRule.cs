namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a [Subtype] you control attacks" / "Whenever another [Subtype] you
/// control attacks" — subtype-filtered attack trigger. The attacks analog of
/// <see cref="AnotherSubtypeDiesConditionRule"/>/<see cref="AnotherSubtypeEntersConditionRule"/>:
/// the trigger fires when a permanent of a specific creature subtype that you
/// control is declared as an attacker.
///
/// <para>CR 508 (Declare Attackers Step) — a creature is declared as an attacker
/// during this step. CR 205.3m — creature subtypes (e.g. Knight, Dragon, Vampire)
/// are named on the type line; the subtype filter narrows which attacking creatures
/// fire the ability. CR 603.1: triggered abilities use "when," "whenever," or "at"
/// to watch for an event.</para>
///
/// <para>Examples:
///   "Whenever a Knight you control attacks, ..." (Knights' Charge).
///   "Whenever a Sliver you control attacks, ..." (Leeching Sliver).
///   "Whenever another Cat you control attacks, ..." (Arahbo, Roar of the World) —
///   "another" excludes the source (CR 109.5), mirroring the dies/enters siblings.</para>
///
/// <para>Priority 995 (matching the dies/enters subtype siblings) so this specific
/// subtype form is tried before the generic <see cref="AttacksConditionRule"/> (987),
/// whose <c>ParseObjectFilter</c> has no subtype path and would otherwise fail to
/// match "a Knight you control attacks" at all (silently dropping the whole trigger,
/// not just the subtype).</para>
///
/// <para>Right-anchored on "attacks" (mirrors <see cref="AttacksFirstTimeEachTurnConditionRule"/>):
/// a card with an appended qualifier on the SAME clause — e.g. Stinkdrinker Bandit's
/// "Whenever a Rogue you control attacks and isn't blocked, ..." — must NOT match here,
/// since the shared subject filter this rule builds carries no room for the trailing
/// qualifier and would otherwise silently drop it. Right-anchoring lets that qualified
/// sibling continue to fail to parse (rather than being lossily mislabeled) until a
/// dedicated rule for that shape exists.</para>
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class SubtypeAttacksConditionRule : ITriggerConditionRule
{
  // Matches "a|another <Subtype> you control attacks" with "attacks" as the LAST word
  // of the trigger text (right-anchored, mirrors the ordinal-suffix sibling above).
  // Subtype must be a proper-noun (capitalised first letter, one or two words) to
  // distinguish creature subtypes ("Knight", "Sliver", "Dragon") from the generic type
  // word "creature" — CR 205.3m. NOT IgnoreCase, so "a creature you control attacks"
  // does NOT match here and falls through to AttacksConditionRule via ParseObjectFilter.
  private static readonly Regex _pattern = new(
    @"\b(?<article>a|another)\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+attacks\s*$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attacks"))
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
    var excludeSelf = string.Equals(m.Groups["article"].Value, "another", System.StringComparison.Ordinal)
      ? (bool?)true
      : null;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
        ExcludeSelf = excludeSelf,
      },
    };
  }
}
