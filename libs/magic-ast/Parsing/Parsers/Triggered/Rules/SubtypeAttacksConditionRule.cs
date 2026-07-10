namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a[nother] &lt;Subtype&gt; [you control] attacks" — a subtype-filtered attack
/// trigger. The attacks analog of <see cref="AnotherSubtypeDiesConditionRule"/>/
/// <see cref="AnotherSubtypeEntersConditionRule"/>: the trigger fires when a permanent of a
/// specific creature subtype is declared as an attacker.
///
/// <para>
/// This single rule covers two closely-related surfaces (combined from what were two
/// same-named batch-9 sibling rules — Knights' Charge "a Knight <b>you control</b> attacks"
/// and Najeela "a Warrior attacks"):
/// <list type="bullet">
/// <item>the optional <c>you control</c> qualifier → <see cref="ControllerFilter.You"/> when
/// present, otherwise no controller restriction (any player's creature of that subtype fires
/// it — Najeela's own Warrior included);</item>
/// <item>the article <c>another</c> → <c>ExcludeSelf = true</c> (CR 109.5's "another" excludes
/// the source, e.g. Arahbo, Roar of the World), whereas plain <c>a</c>/<c>an</c> is
/// unrestricted.</item>
/// </list>
/// </para>
///
/// <para>CR 508 (Declare Attackers Step) — a creature is declared as an attacker during this
/// step. CR 205.3m — creature subtypes (e.g. Knight, Warrior, Dragon) are named on the type
/// line; the subtype filter narrows which attacking creatures fire the ability. CR 603.1:
/// triggered abilities use "when," "whenever," or "at."</para>
///
/// <para>The subtype token requires a capitalised first letter (one or two words), so the
/// generic type word "creature" ("a creature you control attacks") does NOT match here and
/// falls through to <see cref="AttacksConditionRule"/>/<c>ParseObjectFilter</c> — hence NOT
/// <see cref="RegexOptions.IgnoreCase"/>. Priority 995 (matching the dies/enters subtype
/// siblings) puts it above the generic <see cref="AttacksConditionRule"/> (987). Right-anchored
/// on <c>attacks$</c> so it never swallows a longer clause such as Stinkdrinker Bandit's
/// "attacks and isn't blocked" (which is left to fail cleanly until a dedicated rule exists,
/// rather than being lossily mislabeled).</para>
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class SubtypeAttacksConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"\b(?<article>a|an|another)\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)" +
    @"(?<control>\s+you\s+control)?\s+attacks\s*$",
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
        Controller = m.Groups["control"].Success ? ControllerFilter.You : null,
        ExcludeSelf = excludeSelf,
      },
    };
  }
}
