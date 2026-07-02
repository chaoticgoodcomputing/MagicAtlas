namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return ... to its owner's hand" — type-disjunction, you-control, optional "you may"/"up to N",
/// and non-prefix qualifiers (e.g. "nonland permanent", "nontoken creature").
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
    // "another target" / "other target" — self-exclusion (CR 109.5). Carried on the
    // structured ExcludeSelf axis rather than a free-text characteristic.
    bool? excludeSelf =
      lower.Contains("another target") || Regex.IsMatch(lower, @"\bother\s+target\b")
        ? true
        : (bool?)null;

    // Capture any "non<X>" qualifier that precedes a card-type token.
    // e.g. "target nonland permanent" → characteristics: ["nonland"]
    var nonMatch = Regex.Match(lower, @"\b(non\w+)\s+(?:creature|planeswalker|artifact|enchantment|permanent|land)\b");
    if (nonMatch.Success)
    {
      characteristics.Add(nonMatch.Groups[1].Value);
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
      Characteristics = characteristics.Count > 0 ? characteristics.Select(Characteristic.FromLabel).ToList() : null,
      Controller = controller,
      ExcludeSelf = excludeSelf,
    };

    // Targeted phrasing ("return target X") → ObjectReferenceKind.Target.
    // Indefinite phrasing ("return a land you control") → ObjectReferenceKind.Any:
    // the controller picks one qualifying object at resolution without it being a
    // formal target (no targeting declaration, no shroud/hexproof interaction).
    // Rule 115.1 / 601.2c contrast: only the "target" keyword creates a target.
    var refKind = isTargeted ? ObjectReferenceKind.Target : ObjectReferenceKind.Any;

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new ReturnToHandEffect {
      Target = new ObjectReference { Kind = refKind, Filter = filter }}, isOptional);
    return true;
  }
}
