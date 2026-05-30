namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature becomes [color] until end of turn." — layer-5 color-changing
/// continuous effect (CR 105.3). Recognizes the standard activated-ability pattern
/// on a target creature.
///
/// <para>
/// CR 105.3 (verbatim): "Effects may change an object's color or give a color to
/// a colorless object. If an effect gives an object a new color, the new color
/// replaces all previous colors the object had (unless the effect said the object
/// became that color 'in addition' to its other colors)."
/// </para>
///
/// <para>
/// Supported colors: white, blue, black, red, green, colorless (CR 105.1).
/// Color codes follow the WUBRG convention used throughout the MAST schema.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 980)]
public sealed class ChangeColorEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+becomes\s+(?<color>white|blue|black|red|green|colorless)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"]     = "W",
      ["blue"]      = "U",
      ["black"]     = "B",
      ["red"]       = "R",
      ["green"]     = "G",
      ["colorless"] = "C",
    };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var colorName = match.Groups["color"].Value;
    if (!_colorCodes.TryGetValue(colorName, out var colorCode))
    {
      return null;
    }

    return new ChangeColorEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Colors = [colorCode],
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
