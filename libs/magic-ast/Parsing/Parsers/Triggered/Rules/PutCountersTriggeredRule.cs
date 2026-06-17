namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "put a +1/+1 counter on this creature" / "put a -1/-1 counter on target creature" /
/// "that player gets N [type] counters" (poison/energy/etc. given to a player on combat
/// damage — Fynn, the Fangbearer; Blighted Agent; etc.).
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." Poison counters are placed
/// on players; the "gets" verb in oracle text means the player receives those counters.
/// Distinct from "put a counter on [permanent]" only in subject (player vs. permanent)
/// and verb ("gets" vs. "put … on"). MAST models both as <c>putCounters</c> effect —
/// the counter placement is the same action regardless of verb.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class PutCountersTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();

    // "that player gets N [type] counters" — poison/energy counter given to the player
    // who was dealt combat damage (Fynn, the Fangbearer; Blighted Agent family).
    // Rule 122.1: counters on players modify their game state (poison, energy, etc.).
    // CR 702.58a (Infect/Poison): a player with ten or more poison counters loses.
    var getsCountersMatch = Regex.Match(
      text,
      @"^that\s+player\s+gets?\s+(?<count>\w+)\s+(?<type>[\w\-]+)\s+counters?\.?$",
      RegexOptions.IgnoreCase
    );
    if (getsCountersMatch.Success)
    {
      var getsCountRaw = getsCountersMatch.Groups["count"].Value.ToLowerInvariant();
      int getsCount = getsCountRaw switch
      {
        "a" or "an" or "one" => 1,
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ => int.TryParse(getsCountRaw, out var n) ? n : 1,
      };
      var getsCounterType = getsCountersMatch.Groups["type"].Value.ToLowerInvariant();
      effect = new PutCountersEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        CounterType = getsCounterType,
        Count = LiteralQuantity.Of(getsCount),
      };
      return true;
    }

    if (!lower.Contains("put") || !lower.Contains("counter"))
    {
      return false;
    }

    // Extract counter type. Named counters come in two forms:
    //   "+1/+1" / "-1/-1" — P/T counters (the common case)
    //   "a verse counter", "a time counter", "an age counter", etc. — named counters (Rule 122.1)
    // The "a <type> counter" pattern is parsed with a regex that captures <type>.
    string counterType;
    if (text.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (text.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      // Named counter: "put a <type> counter on ..." (Rule 122.1)
      var namedMatch = Regex.Match(
        text,
        @"\bput\s+a(?:n)?\s+(?<type>[\w\-]+)\s+counter\b",
        RegexOptions.IgnoreCase
      );
      if (!namedMatch.Success)
      {
        return false;
      }
      counterType = namedMatch.Groups["type"].Value.ToLowerInvariant();
    }

    var isOptional = lower.Contains("you may");
    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(text) ?? 1;
    var hasAnother = lower.Contains("another target");
    ObjectReference target;

    // "put a +1/+1 counter on each other [Subtype] creature you control" —
    // mass-counter shape for turned-face-up triggers (e.g. Stormwing Dragon).
    // Must be matched before the "target creature" / "this creature" paths so the
    // "each other" quantifier lands correctly on an Each reference.
    var eachOtherMatch = Regex.Match(
      text,
      @"\bon\s+each\s+other\s+(?<subtype>[A-Za-z]+)\s+creature\s+you\s+control\b",
      RegexOptions.IgnoreCase
    );
    if (eachOtherMatch.Success)
    {
      var subtype = eachOtherMatch.Groups["subtype"].Value;
      // Capitalise first letter to match oracle-text convention.
      subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
        },
      };
    }
    else if (lower.Contains("target creature or enchantment you control"))
    {
      // "put a +1/+1 counter on target creature or enchantment you control" — disjunctive
      // type filter: the target may be any creature or enchantment the controller controls.
      // CardTypes as a list covers the disjunction (CR 205.2); "or" in oracle text maps to
      // a multi-element CardTypes filter, consistent with the existing multi-type ObjectFilter
      // convention (e.g. "artifact or creature", "creature or land").
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature", "enchantment"],
          Controller = ControllerFilter.You,
        },
      };
    }
    else if (lower.Contains("target creature you control"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          ExcludeSelf = hasAnother ? true : (bool?)null,
        },
      };
    }
    else if (lower.Contains("target creature"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          ExcludeSelf = hasAnother ? true : (bool?)null,
        },
      };
    }
    else if (
      lower.Contains("this creature")
      || lower.Contains("this permanent")
      || lower.Contains("this enchantment")
      || lower.Contains("this artifact")
      || lower.Contains("this land")
    )
    {
      target = ObjectReference.Self();
    }
    else if (Regex.IsMatch(lower, @"\bon\s+it\b"))
    {
      // "put a +1/+1 counter on it" — "it" is the pronoun referring to the
      // creature that triggered this ability (the attacker, blocker, etc.).
      target = ObjectReference.It();
    }
    else
    {
      target = ObjectReference.Self();
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new PutCountersEffect {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count)}, isOptional);
    return true;
  }
}
