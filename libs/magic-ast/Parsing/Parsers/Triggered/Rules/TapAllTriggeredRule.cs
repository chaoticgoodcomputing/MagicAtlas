namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "tap all [color]? [type]s [you control]?" — mass-tap triggered effect.
///
/// Covers patterns such as:
/// <list type="bullet">
///   <item>"tap all red creatures" (Wrath of Marit Lage)</item>
///   <item>"tap all lands you control"</item>
///   <item>"tap all creatures"</item>
/// </list>
///
/// Produces a <see cref="TapEffect"/> whose Target is
/// <see cref="ObjectReferenceKind.Each"/> filtered to the named card type,
/// with an optional <see cref="ObjectFilter.Colors"/> constraint when a color
/// qualifier precedes the type noun, and with
/// <see cref="ControllerFilter.You"/> when "you control" is present (or no
/// controller filter otherwise, e.g. "tap all creatures"). Mirrors
/// <see cref="UntapAllTriggeredRule"/> (the untap sibling of this exact
/// template) plus the color axis, parallel to how
/// <see cref="MagicAST.Parsing.Parsers.Static.SubjectDoesntUntapDuringControllersUntapStepsRule"/>
/// keys "Red creatures don't untap..." with the same Colors + CardTypes
/// combination (CR 105 "Destroy target green or white creature." precedent).
///
/// Rule 701.26 (Tap and Untap).
/// </summary>
[TriggeredRule]
public sealed class TapAllTriggeredRule : ITriggeredRule
{
  // Named groups:
  //   color      — optional color qualifier (White|Blue|Black|Red|Green)
  //   type       — the card-type noun (land, creature, permanent, artifact, …)
  //   controller — present when "you control" is in the text
  private static readonly Regex Pattern = new(
    @"^tap\s+all\s+(?:(?<color>white|blue|black|red|green)\s+)?(?<type>[a-z]+)s?\s*(?<controller>you\s+control)?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant().TrimEnd('s');
    var hasController = m.Groups["controller"].Success;

    string[]? colors = null;
    var colorGroup = m.Groups["color"];
    if (colorGroup.Success && _colorNameToCode.TryGetValue(colorGroup.Value, out var colorCode))
    {
      colors = [colorCode];
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = [cardType],
        Colors = colors,
        Controller = hasController ? ControllerFilter.You : null,
      },
    };

    effect = new TapEffect { Target = target };
    return true;
  }
}
