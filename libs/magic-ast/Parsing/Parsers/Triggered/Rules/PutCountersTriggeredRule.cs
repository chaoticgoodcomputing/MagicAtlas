namespace MagicAST.Parsing.Parsers.Triggered.Rules;

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
      return false;
    }

    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(text) ?? 1;
    ObjectReference target;
    if (lower.Contains("target creature you control"))
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
    else if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      target = ObjectReference.Self();
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
    };
    return true;
  }
}
