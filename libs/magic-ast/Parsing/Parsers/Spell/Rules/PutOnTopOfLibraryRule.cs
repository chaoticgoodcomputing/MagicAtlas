namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Put target [type] on top of its owner's library."
/// Covers single-target library-top zone change — the Time Ebb / Temporal Spring pattern.
/// </summary>
[SpellRule]
public sealed class PutOnTopOfLibraryRule : ISpellRule
{
  // "Put target [state] [non<x>] [color] <type>[s] [you control|you own] on top of its owner's library."
  private static readonly Regex Pattern = new(
    @"^Put\s+target(?:\s+(?<state>tapped|untapped))?(?:\s+(?<non>non\w+))?(?:\s+(?<color>white|blue|black|red|green))?\s+(?<type>creature|artifact|enchantment|land|permanent|planeswalker)s?(?:\s+(?<you>you\s+(?:control|own)))?\s+on\s+top\s+of\s+its?\s+owner'?s\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var typeWord = m.Groups["type"].Value.ToLowerInvariant();
    var stateWord = m.Groups["state"].Value.ToLowerInvariant();
    var nonWord = m.Groups["non"].Value.ToLowerInvariant();
    var colorWord = m.Groups["color"].Value.ToLowerInvariant();
    var youGroup = m.Groups["you"].Value.ToLowerInvariant();

    var characteristics = new List<string>();
    if (!string.IsNullOrEmpty(stateWord))
    {
      characteristics.Add(stateWord);
    }
    if (!string.IsNullOrEmpty(nonWord))
    {
      characteristics.Add(nonWord);
    }
    if (youGroup.Contains("you own"))
    {
      characteristics.Add("you own");
    }

    IReadOnlyList<string>? colors = null;
    if (!string.IsNullOrEmpty(colorWord) && ColorCode.TryGetValue(colorWord, out var colorCode))
    {
      colors = [colorCode];
    }

    ControllerFilter? controller = youGroup.Contains("you control") ? ControllerFilter.You : null;

    effect = new PutOnTopOfLibraryEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [typeWord],
          Colors = colors,
          Characteristics = characteristics.Count > 0 ? characteristics.Select(Characteristic.FromLabel).ToList() : null,
          Controller = controller,
        },
      },
    };
    return true;
  }
}
