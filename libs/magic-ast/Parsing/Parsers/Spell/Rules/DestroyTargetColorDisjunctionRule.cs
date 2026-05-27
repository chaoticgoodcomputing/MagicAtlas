namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target [color1] or [color2] [type]." — destroy a target whose color
/// matches any of the listed colors. Covers patterns such as:
/// <list type="bullet">
///   <item>"Destroy target green or white creature." (Deathmark)</item>
///   <item>"Destroy target black or red creature that's attacking or blocking." (Surge of Righteousness)</item>
/// </list>
/// The <see cref="ObjectFilter.Colors"/> field carries the "any of these" semantics
/// (Rule 105.1–105.3): an object passes the filter if it has at least one of the
/// listed colors.
/// Strips reminder-text qualifiers (e.g. "that's attacking or blocking") into
/// <see cref="ObjectFilter.Characteristics"/> so the core color+type shape always
/// resolves.
/// </summary>
[SpellRule]
public sealed class DestroyTargetColorDisjunctionRule : ISpellRule
{
  private static readonly string[] ColorWords =
    ["white", "blue", "black", "red", "green"];

  // Pattern: "Destroy target {color1} or {color2} {type} [optional qualifier]"
  // The qualifier after the type (e.g. "that's attacking or blocking") is captured
  // in <qual> and stored as a Characteristic string.
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<c1>white|blue|black|red|green)\s+or\s+(?<c2>white|blue|black|red|green)\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)(?:\s+(?<qual>.+))?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly Dictionary<string, string> ColorToCode =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "white", "W" },
      { "blue", "U" },
      { "black", "B" },
      { "red", "R" },
      { "green", "G" },
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var c1 = ColorToCode[m.Groups["c1"].Value];
    var c2 = ColorToCode[m.Groups["c2"].Value];
    var type = m.Groups["type"].Value.ToLowerInvariant();

    // Build the de-duped color list in WUBRG order for readability.
    var colors = BuildColorList(c1, c2);

    List<string>? characteristics = null;
    if (m.Groups["qual"].Success)
    {
      var qual = m.Groups["qual"].Value.Trim().TrimEnd('.');
      if (qual.Length > 0)
      {
        characteristics = [qual];
      }
    }

    var filter = new ObjectFilter
    {
      CardTypes = [type],
      Colors = colors,
      Characteristics = characteristics,
    };

    effect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
    };
    return true;
  }

  /// <summary>
  /// Returns the two color codes in WUBRG order (for readability), deduped.
  /// </summary>
  private static IReadOnlyList<string> BuildColorList(string a, string b)
  {
    const string Wubrg = "WUBRG";
    var pair = new HashSet<string> { a, b };
    var result = new List<string>(2);
    foreach (var c in Wubrg)
    {
      if (pair.Contains(c.ToString()))
      {
        result.Add(c.ToString());
      }
    }
    return result;
  }
}
