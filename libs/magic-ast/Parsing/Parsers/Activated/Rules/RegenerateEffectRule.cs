namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Regenerate this creature." / "Regenerate target [type]." (Rule 701.19).
/// MAST records the effect and target only; shield / destruction-replacement
/// semantics are engine territory.
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class RegenerateEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var lower = trimmed.ToLowerInvariant();

    if (!lower.StartsWith("regenerate "))
    {
      return null;
    }

    // "Regenerate this creature" — self-reference
    if (lower == "regenerate this creature" || lower == "regenerate this permanent")
    {
      return new RegenerateEffect { Target = ObjectReference.Self() };
    }

    // "Regenerate enchanted creature" / "Regenerate equipped creature"
    // CR 701.19a: creates a replacement effect protecting the attached permanent.
    if (
      lower == "regenerate enchanted creature"
      || lower == "regenerate equipped creature"
      || lower == "regenerate enchanted permanent"
      || lower == "regenerate equipped permanent"
    )
    {
      return new RegenerateEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      };
    }

    // "Regenerate target [type]"
    var m = Regex.Match(
      trimmed,
      @"^regenerate\s+target\s+(?<type>\w+)$",
      RegexOptions.IgnoreCase
    );
    if (m.Success)
    {
      var cardType = m.Groups["type"].Value.ToLowerInvariant();
      return new RegenerateEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = [cardType] },
        },
      };
    }

    return null;
  }
}
