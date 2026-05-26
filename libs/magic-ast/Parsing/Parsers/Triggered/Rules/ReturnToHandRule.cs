namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return ... to its owner's hand" — type-disjunction, you-control, optional "you may"/"up to N".
/// </summary>
[TriggeredRule]
public sealed class ReturnToHandRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
    if (!lower.Contains("return") || !lower.Contains("hand"))
    {
      return false;
    }
    if (!Regex.IsMatch(lower, @"to\s+(its\s+owner'?s|your)\s+hand"))
    {
      return false;
    }

    var isOptional =
      lower.Contains("you may return")
      || lower.StartsWith("you may ")
      || Regex.IsMatch(lower, @"return\s+up\s+to\s+");
    var characteristics = new List<string>();
    if (lower.Contains("another target"))
    {
      characteristics.Add("another");
    }
    else if (Regex.IsMatch(lower, @"\bother\s+target\b"))
    {
      characteristics.Add("other");
    }

    var cardTypes = new List<string>();
    foreach (var t in new[] { "creature", "planeswalker", "artifact", "enchantment", "permanent" })
    {
      if (Regex.IsMatch(lower, $@"\b{t}\b"))
      {
        cardTypes.Add(t);
      }
    }
    if (cardTypes.Count == 0)
    {
      return false;
    }

    ControllerFilter? controller = null;
    if (lower.Contains("you control"))
    {
      controller = ControllerFilter.You;
    }

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Controller = controller,
    };

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      IsOptional = isOptional,
    };
    return true;
  }
}
