namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return [target|all] [modifier] [type] to [its owner's|their owners'|your] hand[s]."
/// Covers single-target bounce, mass bounce ("Return all X"), "you own" ownership
/// qualifier, "non*" prefix modifiers (e.g. "nonland permanent"), color predicates
/// ("green permanents", "nonblue creatures"), and state adjectives ("tapped creature").
/// </summary>
[SpellRule]
public sealed class ReturnTargetToHandRule : ISpellRule
{
  // "Return all [mod] <type> to their owners' hands."
  // mod can be: non<x> (e.g. "nonblue"), a color word (e.g. "green"), or absent.
  private static readonly Regex AllPattern = new(
    @"^Return\s+all\s+(?:(?<mod>non\w+|white|blue|black|red|green)\s+)?(?<type>\w+)\s+to\s+their\s+owners'?\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "Return [you may] [another|up to N] target [state] [non<x>] [color] <type>[s] [you control|you own] to [its owner's|their owners'|your] hand[s]."
  // state: tapped, untapped, etc.  non<x>: nonland, nontoken, etc.
  private static readonly Regex TargetPattern = new(
    @"^Return\s+(?:(?<opt>up\s+to\s+\w+|you\s+may)\s+)?(?<target>(?:another\s+)?(?:up\s+to\s+\w+\s+)?target\s+(?:(?<state>tapped|untapped)\s+)?(?:non\w+\s+)?(?:white|blue|black|red|green)\s+)?(?<target2>(?:creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+you\s+(?:control|own))?)\s+to\s+(?:its?\s+owner'?s|their\s+owners'?|your)\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Separate targeted pattern to better handle the modifiers before the type.
  // "Return target [state] [non<x>] [color] <type>[s] [you control|you own] to owner's hand."
  private static readonly Regex TargetFullPattern = new(
    @"^Return\s+(?:(?<opt>up\s+to\s+\w+|you\s+may)\s+)?(?:another\s+)?(?:up\s+to\s+\w+\s+)?target(?:\s+(?<state>tapped|untapped))?(?:\s+(?<non>non\w+))?(?:\s+(?<color>white|blue|black|red|green))?\s+(?<type>creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+(?<you>you\s+(?:control|own)))?\s+to\s+(?:its?\s+owner'?s|their\s+owners'?|your)\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly string[] KnownTypes =
    ["creature", "artifact", "enchantment", "land", "permanent", "planeswalker"];

  // Color name to MTG code mapping.
  private static readonly Dictionary<string, string> ColorCode =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // --- "Return all [mod] <type> to their owners' hands" ---
    var allMatch = AllPattern.Match(text);
    if (allMatch.Success)
    {
      var modWord = allMatch.Groups["mod"].Value.ToLowerInvariant();
      var filterWord = allMatch.Groups["type"].Value.ToLowerInvariant();

      // Strip plural 's' to normalise (e.g. "artifacts" → "artifact")
      var normalised = filterWord.TrimEnd('s');
      var cardType = Array.Find(KnownTypes, t => t == normalised) ?? normalised;

      IReadOnlyList<string>? colors = null;
      IReadOnlyList<string>? characteristics = null;

      if (!string.IsNullOrEmpty(modWord))
      {
        if (ColorCode.TryGetValue(modWord, out var code))
        {
          // Positive color predicate: "green" → Colors: ["G"]
          colors = [code];
        }
        else
        {
          // non-prefix or other characteristic: "nonblue", "nonbasic", etc.
          characteristics = [modWord];
        }
      }

      effect = new ReturnToHandEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter
          {
            CardTypes = [cardType],
            Colors = colors,
            Characteristics = characteristics,
          },
        },
      };
      return true;
    }

    // --- "Return target [state] [non<x>] [color] <type> [you control|own] to … hand" ---
    var targetMatch = TargetFullPattern.Match(text);
    if (!targetMatch.Success)
    {
      // Fall back to legacy pattern for complex shapes not covered by TargetFullPattern.
      return TryMatchLegacyTarget(text, out effect);
    }

    var lower = text.ToLowerInvariant();
    var isOptional = targetMatch.Groups["opt"].Success
      || lower.Contains("you may return")
      || Regex.IsMatch(lower, @"return\s+up\s+to\s+");

    var typeWord = targetMatch.Groups["type"].Value.ToLowerInvariant();
    var stateWord = targetMatch.Groups["state"].Value.ToLowerInvariant();
    var nonWord = targetMatch.Groups["non"].Value.ToLowerInvariant();
    var colorWord = targetMatch.Groups["color"].Value.ToLowerInvariant();
    var youGroup = targetMatch.Groups["you"].Value.ToLowerInvariant();

    var targetCharacteristics = new List<string>();
    if (!string.IsNullOrEmpty(stateWord))
    {
      targetCharacteristics.Add(stateWord); // e.g. "tapped"
    }
    if (!string.IsNullOrEmpty(nonWord))
    {
      targetCharacteristics.Add(nonWord); // e.g. "nonland"
    }
    // "you own" is a characteristic (ownership qualifier), not a controller filter.
    if (youGroup.Contains("you own"))
    {
      targetCharacteristics.Add("you own");
    }

    IReadOnlyList<string>? targetColors = null;
    if (!string.IsNullOrEmpty(colorWord) && ColorCode.TryGetValue(colorWord, out var colorCode))
    {
      targetColors = [colorCode];
    }

    ControllerFilter? controller = youGroup.Contains("you control") ? ControllerFilter.You : null;

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [typeWord],
          Colors = targetColors,
          Characteristics = targetCharacteristics.Count > 0 ? targetCharacteristics : null,
          Controller = controller,
        },
      },
      IsOptional = isOptional,
    };
    return true;
  }

  /// <summary>
  /// Legacy fallback for multi-type / "another" / complex target shapes that pre-date
  /// the structured TargetFullPattern.
  /// </summary>
  private static bool TryMatchLegacyTarget(string text, out Effect? effect)
  {
    effect = null;

    var legacyPattern = new Regex(
      @"^Return\s+(?:(?<opt>up\s+to\s+\w+|you\s+may)\s+)?(?<target>(?:another\s+)?(?:up\s+to\s+\w+\s+)?target\s+(?:non\w+\s+)?(?:creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+you\s+(?:control|own))?)\s+to\s+(?:its?\s+owner'?s|their\s+owners'?|your)\s+hands?$",
      RegexOptions.IgnoreCase
    );

    var m = legacyPattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var lower = text.ToLowerInvariant();
    var isOptional = m.Groups["opt"].Success
      || lower.Contains("you may return")
      || Regex.IsMatch(lower, @"return\s+up\s+to\s+");
    var targetText = m.Groups["target"].Value.ToLowerInvariant();

    var characteristics = new List<string>();
    if (targetText.StartsWith("another "))
    {
      characteristics.Add("other");
    }

    var nonMatch = Regex.Match(targetText, @"\b(non\w+)\b");
    if (nonMatch.Success)
    {
      characteristics.Add(nonMatch.Groups[1].Value);
    }

    if (targetText.Contains("you own"))
    {
      characteristics.Add("you own");
    }

    string[] knownTypes = ["creature", "artifact", "enchantment", "land", "permanent", "planeswalker"];
    var cardTypes = new List<string>();
    foreach (var t in knownTypes)
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
