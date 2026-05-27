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

    // Detect whether the phrasing is a targeted ("target X") or indefinite
    // ("a land you control") reference. Indefinite = no "target" keyword.
    var isTargeted = Regex.IsMatch(lower, @"\btarget\b");

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
    foreach (var t in new[] { "creature", "planeswalker", "artifact", "enchantment", "permanent", "land" })
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
    else if (Regex.IsMatch(lower, @"\ban\s+opponent\s+controls\b"))
    {
      controller = ControllerFilter.Opponent;
    }

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Controller = controller,
    };

    // Targeted phrasing ("return target X") → ObjectReferenceKind.Target.
    // Indefinite phrasing ("return a land you control") → ObjectReferenceKind.Any:
    // the controller picks one qualifying object at resolution without it being a
    // formal target (Rule 701.10; no targeting declaration, no shroud/hexproof
    // interaction). Rule 601.2c contrast: "target" requires a targeting declaration.
    var refKind = isTargeted ? ObjectReferenceKind.Target : ObjectReferenceKind.Any;

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference { Kind = refKind, Filter = filter },
      IsOptional = isOptional,
    };
    return true;
  }
}
