namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put N +1/+1 (or -1/-1) counters on [target]" — "Put a +1/+1 counter on this
/// creature", "Put a +1/+1 counter on target creature you control".
///
/// <para>Also handles named counters in the activated path — "Put a storage
/// counter on this land", "Put a charge counter on this artifact" (the storage-
/// land / charge-artifact "load" abilities). CR 122.1: a counter is a marker
/// placed on an object; storage/charge are named counters, the same placement
/// action as +1/+1. Mirrors the named-counter branch already in the triggered
/// PutCounters rule.</para>
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class PutCountersEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("put") || !lower.Contains("counter"))
    {
      return null;
    }

    // Parse counter type. P/T counters ("+1/+1", "-1/-1") are the common case;
    // named counters ("a storage counter", "a charge counter") are captured by
    // the "put a(n) <type> counter" pattern (CR 122.1).
    string counterType;
    if (lower.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (lower.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      var namedMatch = Regex.Match(
        effectText,
        @"\bput\s+a(?:n)?\s+(?<type>[\w\-]+)\s+counter\b",
        RegexOptions.IgnoreCase
      );
      if (!namedMatch.Success)
      {
        return null; // Unknown counter type
      }
      counterType = namedMatch.Groups["type"].Value.ToLowerInvariant();
    }

    var count = ActivatedRuleHelpers.ParseNumberWord(effectText) ?? 1;

    // Parse target
    ObjectReference target;
    if (
      lower.Contains("this creature")
      || lower.Contains("this permanent")
      || lower.Contains("this artifact")
      || lower.Contains("this enchantment")
      || lower.Contains("this land")
    )
    {
      target = ObjectReference.Self();
    }
    else if (lower.Contains("target creature you control"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      };
    }
    else if (lower.Contains("target creature"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    else
    {
      // Default to self
      target = ObjectReference.Self();
    }

    return new PutCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
  }
}
