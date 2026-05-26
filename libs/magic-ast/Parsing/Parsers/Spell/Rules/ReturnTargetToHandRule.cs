namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [type] to its owner's hand." with "up to N" / "you may" optional flags.
/// </summary>
[SpellRule]
public sealed class ReturnTargetToHandRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Return\s+(?:(?<opt>up\s+to\s+\w+|you\s+may)\s+)?(?<target>(?:another\s+)?(?:up\s+to\s+\w+\s+)?target\s+(?:creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+you\s+control)?)\s+to\s+(?:its?\s+owner'?s|their\s+owners'?|your)\s+hand(?:s)?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var lower = text.ToLowerInvariant();
    var isOptional = m.Groups["opt"].Success || lower.Contains("you may return") || Regex.IsMatch(lower, @"return\s+up\s+to\s+");
    var targetText = m.Groups["target"].Value.ToLowerInvariant();

    var characteristics = new List<string>();
    if (targetText.StartsWith("another "))
    {
      characteristics.Add("other");
    }

    var cardTypes = new List<string>();
    foreach (var t in new[] { "creature", "artifact", "enchantment", "land", "permanent", "planeswalker" })
    {
      if (Regex.IsMatch(targetText, $@"\b{t}s?\b"))
      {
        cardTypes.Add(t);
      }
    }
    if (cardTypes.Count == 0)
    {
      return false;
    }
    ControllerFilter? controller = targetText.Contains("you control") ? ControllerFilter.You : null;

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Characteristics = characteristics.Count > 0 ? characteristics : null,
          Controller = controller,
        },
      },
      IsOptional = isOptional,
    };
    return true;
  }
}
