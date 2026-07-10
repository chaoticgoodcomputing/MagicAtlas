namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses "Creatures you control with a/+1/+1 counter(s) on them/it have &lt;keyword&gt;."
/// — a lord-grants-keyword static continuous effect (CR 604.2) whose subject filter is
/// narrowed to creatures the controller controls that additionally carry a counter of the
/// named kind (CR 122). Badgermole: "Creatures you control with +1/+1 counters on them have
/// trample."
///
/// <para>
/// CR 702.19a (verbatim): "Trample is a static ability." The grant is continuous for as
/// long as the source remains on the battlefield with the ability (CR 604.2), reaching
/// every creature the controller controls that has a counter of the stated type at any
/// given time — not just those that had one when the source entered.
/// </para>
///
/// <para>
/// Sibling to <see cref="BareKeywordGrantRule"/>'s Arm 5 ("Each creature you control with
/// a +1/+1 counter on it has &lt;keyword&gt;.") — that arm is the singular
/// "Each creature ... has" phrasing; this rule is the plural "Creatures ... have" phrasing
/// ("them"/"it" both accepted for the counter back-reference, "counter"/"counters" both
/// accepted for the count). Kept as a separate file (rather than folding into
/// <c>BareKeywordGrantRule</c> or <see cref="ControlledFilterHaveKeywordListRule"/>) so
/// the new anchored pattern cannot collide with either sibling's arms.
/// </para>
///
/// <para>
/// The counter-presence predicate is structured as a
/// <see cref="MagicAST.AST.References.CounterCharacteristic"/> (CR 122) via
/// <see cref="MagicAST.AST.References.Characteristic.FromLabel"/>, mirroring how
/// <c>BareKeywordGrantRule</c>'s Arm 5 and <see cref="MagicAST.AST.References.ObjectFilter"/>'s
/// other characteristic predicates (tapped/untapped/combat-state) are structured.
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching as a substring of a more specific sibling clause.
/// Reminder text is stripped before matching so a trailing parenthetical explanation still
/// lets the rule fire.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class ControlledCreaturesWithCounterHaveKeywordRule : IStaticRule
{
  // "Creatures you control with a/+1/+1 counter(s) on them/it have <kw1>[ and <kw2>]."
  // <counterType> permits a slash-bearing counter name (+1/+1, -1/-1, etc.); "a "
  // is optional (singular "a +1/+1 counter" vs. plural "+1/+1 counters").
  private static readonly Regex _pattern = new(
    @"^\s*Creatures\s+you\s+control\s+with\s+(?:a\s+)?(?<counterType>[+\-]?\d*/?[+\-]?\d*|[a-z]+)\s+counters?\s+on\s+(?:them|it)\s+have\s+" +
    @"(?<kw1>[a-z][a-z]*(?:\s+[a-z]+)*?)(?:\s+and\s+(?<kw2>[a-z][a-z ]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value.Trim();
    if (counterType.Length == 0)
    {
      return null;
    }

    var filter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Controller = ControllerFilter.You,
      Characteristics = [new CounterCharacteristic { CounterType = counterType }],
    };

    var kw1 = match.Groups["kw1"].Value.Trim().ToLowerInvariant();
    var granted1 = StaticRuleHelpers.MapKeywordToStaticAbility(kw1);
    if (granted1 is null)
    {
      return null;
    }

    var effects = new List<Effect>
    {
      new GainAbilityEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
        GainedAbility = granted1,
      },
    };

    if (match.Groups["kw2"].Success)
    {
      var kw2 = match.Groups["kw2"].Value.Trim().ToLowerInvariant();
      var granted2 = StaticRuleHelpers.MapKeywordToStaticAbility(kw2);
      if (granted2 is null)
      {
        // First keyword resolved but the second didn't — decline entirely so
        // the fallback surfaces the gap rather than emitting a partial grant.
        return null;
      }

      effects.Add(new GainAbilityEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
        GainedAbility = granted2,
      });
    }

    return [new StaticAbility { Effects = effects }];
  }
}
