namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "put a +1/+1 counter on this creature" / "put a -1/-1 counter on target creature".
/// </summary>
[TriggeredRule]
public sealed class PutCountersTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
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
    if (lower.Contains("target creature you control"))
    {
      var characteristics = hasAnother ? new[] { "another" } : null;
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          Characteristics = characteristics,
        },
      };
    }
    else if (lower.Contains("target creature"))
    {
      var characteristics = hasAnother ? new[] { "another" } : null;
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = characteristics,
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

    effect = new PutCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
      IsOptional = isOptional,
    };
    return true;
  }
}
