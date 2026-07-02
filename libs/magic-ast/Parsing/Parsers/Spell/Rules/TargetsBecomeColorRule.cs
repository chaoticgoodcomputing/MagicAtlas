namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the spell shape "One or more target creatures become [color] until end
/// of turn." (Dwarven Song). A one-or-more-targeting spell that applies a layer-5
/// color-changing continuous effect: each target becomes the named color for the turn.
///
/// <para>
/// CR 105.3 (verbatim): "Effects may change an object's color or give a color to a
/// colorless object. If an effect gives an object a new color, the new color replaces
/// all previous colors the object had (unless the effect said the object became that
/// color 'in addition' to its other colors). Effects may also make a colored object
/// become colorless."
/// </para>
///
/// <para>
/// CR 613.1e (verbatim): "Layer 5: Color-changing effects are applied."
/// </para>
///
/// <para>
/// CR 115.1: "Some spells and abilities require their controller to choose one or more
/// targets for them. ..." — "one or more target creatures" is exactly 115.1's one-or-more
/// targeting, modeled as <see cref="AtLeastQuantity"/> with <c>Minimum = 1</c> (a
/// variable number of targets, floor one, no ceiling; CR 601.2c).
/// </para>
///
/// <para>
/// The becomes-color effect reuses the existing <see cref="ChangeColorEffect"/>
/// (<c>[OracleEffect("changeColor")]</c>) — the same node the activated
/// <c>ChangeColorEffectRule</c> emits — with the color-name→WUBRG map and
/// <see cref="UntilTimeDuration.EndOfTurn"/> duration mirrored from that rule. The verb
/// here is the plural "become" (targets, not a singular target). Default priority (50)
/// is fine: the anchor is specific and non-overlapping.
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetsBecomeColorRule : ISpellRule
{
  // Anchored; trailing period has already been stripped by the spell parser.
  private static readonly Regex _pattern = new(
    @"^One or more target creatures? become (?<color>white|blue|black|red|green|colorless) until end of turn$",
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

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var colorName = match.Groups["color"].Value;
    if (!_colorCodes.TryGetValue(colorName, out var colorCode))
    {
      return false;
    }

    effect = new ChangeColorEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
        Quantity = new AtLeastQuantity { Minimum = 1 },
      },
      Colors = [colorCode],
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
