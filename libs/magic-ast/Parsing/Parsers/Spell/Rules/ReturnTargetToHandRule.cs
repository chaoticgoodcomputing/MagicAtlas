namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return [target|all] [type] to [its owner's|their owners'|your] hand[s]."
/// Covers single-target bounce, mass bounce ("Return all X"), "you own" ownership
/// qualifier, and "non*" prefix modifiers (e.g. "nonland permanent").
/// </summary>
[SpellRule]
public sealed class ReturnTargetToHandRule : ISpellRule
{
  // "Return all <type> to their owners' hands."
  private static readonly Regex AllPattern = new(
    @"^Return\s+all\s+(?<filter>\w+)\s+to\s+their\s+owners'?\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "Return [you may] [another|up to N] target [non<x>] <type>[s] [you control|you own] to [its owner's|their owners'|your] hand[s]."
  private static readonly Regex TargetPattern = new(
    @"^Return\s+(?:(?<opt>up\s+to\s+\w+|you\s+may)\s+)?(?<target>(?:another\s+)?(?:up\s+to\s+\w+\s+)?target\s+(?:non\w+\s+)?(?:creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+you\s+(?:control|own))?)\s+to\s+(?:its?\s+owner'?s|their\s+owners'?|your)\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly string[] KnownTypes =
    ["creature", "artifact", "enchantment", "land", "permanent", "planeswalker"];

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // --- "Return all <type> to their owners' hands" ---
    var allMatch = AllPattern.Match(text);
    if (allMatch.Success)
    {
      var filterWord = allMatch.Groups["filter"].Value.ToLowerInvariant();
      // Strip plural 's' to normalise (e.g. "artifacts" → "artifact")
      var normalised = filterWord.TrimEnd('s');
      var cardType = Array.Find(KnownTypes, t => t == normalised) ?? normalised;

      effect = new ReturnToHandEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter
          {
            CardTypes = [cardType],
          },
        },
      };
      return true;
    }

    // --- "Return target [non<x>] <type> [you own|you control] to … hand" ---
    var targetMatch = TargetPattern.Match(text);
    if (!targetMatch.Success)
    {
      return false;
    }

    var lower = text.ToLowerInvariant();
    var isOptional = targetMatch.Groups["opt"].Success
      || lower.Contains("you may return")
      || Regex.IsMatch(lower, @"return\s+up\s+to\s+");
    var targetText = targetMatch.Groups["target"].Value.ToLowerInvariant();

    var characteristics = new List<string>();
    if (targetText.StartsWith("another "))
    {
      characteristics.Add("other");
    }

    // Detect "non<x>" prefix modifier (e.g. "nonland", "nontoken")
    var nonMatch = Regex.Match(targetText, @"\b(non\w+)\b");
    if (nonMatch.Success)
    {
      var nonPrefix = nonMatch.Groups[1].Value; // e.g. "nonland"
      characteristics.Add(nonPrefix);
    }

    // Detect "you own" qualifier
    if (targetText.Contains("you own"))
    {
      characteristics.Add("you own");
    }

    var cardTypes = new List<string>();
    foreach (var t in KnownTypes)
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
