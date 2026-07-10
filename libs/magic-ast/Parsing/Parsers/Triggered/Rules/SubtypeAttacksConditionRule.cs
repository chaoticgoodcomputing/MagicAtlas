namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a [Subtype] attacks[, ...]" — a bare creature-subtype attack trigger with no
/// self- or card-type anchor (Najeela, the Blade-Blossom: "Whenever a Warrior attacks, ...").
/// Emits <see cref="TriggerEvent.Attacks"/> (CR 508 — Declare Attackers) with a filter on the
/// named creature subtype (CR 205.3m — creature subtypes follow the type line). Any controller's
/// Warrior fires it, so no <c>Controller</c>/<c>IsSelf</c> constraint is set (the source itself
/// is a Warrior and can trigger it too).
///
/// <para>
/// Distinct from the generic <see cref="AttacksConditionRule"/>, whose shared
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> only recognises card-type subjects
/// ("a creature", "this creature", a self-by-name) and returns null for a bare subtype like
/// "a Warrior" — so that rule declines the whole trigger. This rule fills exactly that gap.
/// </para>
///
/// <para>
/// Priority 989 — above <see cref="AttacksConditionRule"/> (Priority 987) so the subtype form is
/// tried first, but below <see cref="AttacksAnOpponentConditionRule"/> (990) and
/// <see cref="AttacksFirstTimeEachTurnConditionRule"/> (988) which carry stricter surfaces. The
/// subject regex is ANCHORED to "a[n] &lt;Capitalised-word&gt; attacks" (optionally
/// "&lt;subtype&gt; you control attacks"), so it never matches "a creature ... attacks" (lowercase
/// noun) nor a self-by-name trigger ("Najeela attacks" has no leading article) — no sub-string
/// collision with the generic siblings.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 989)]
public sealed class SubtypeAttacksConditionRule : ITriggerConditionRule
{
  // "Whenever/When a[n] <Subtype> [you control] attacks" — the subtype is a single
  // Capitalised word (creature subtypes are single tokens: Warrior, Goblin, Zombie …).
  // The leading article "a"/"an" plus the required capitalisation distinguishes a subtype
  // subject from a lowercase card-type subject ("a creature") and from a self-by-name
  // trigger (no leading article). Anchored at the start of the (timing-prefixed) trigger text.
  // NOT IgnoreCase: the subtype anchor [A-Z] must genuinely require a capital first letter
  // (under IgnoreCase, [A-Z] also matches lowercase and would swallow "a creature attacks").
  // The timing word alternation spells both casings explicitly.
  private static readonly Regex _pattern = new(
    @"^(?:When|Whenever)\s+an?\s+(?<subtype>[A-Z][a-zA-Z]+)(?<control>\s+you\s+control)?\s+attacks\b",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attacks"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    // Capitalise defensively (the anchor already required a capital first letter).
    var raw = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(raw[0]) + raw[1..];

    var filter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Subtypes = [subtype],
      Controller = m.Groups["control"].Success ? ControllerFilter.You : null,
    };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = filter,
    };
  }
}
