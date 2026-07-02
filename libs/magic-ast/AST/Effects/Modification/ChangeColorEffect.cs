namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Target [object] becomes [color(s)] until end of turn." — layer-5 (CR 613.1e)
/// color-changing continuous effect. Describes the oracle-text declaration that
/// the named permanent's colors are replaced by the specified set for the
/// duration of the effect.
///
/// <para>
/// CR 105.3 (verbatim): "Effects may change an object's color or give a color to
/// a colorless object. If an effect gives an object a new color, the new color
/// replaces all previous colors the object had (unless the effect said the object
/// became that color 'in addition' to its other colors)."
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Metathran Transport — "{U}: Target creature becomes blue until end of
///   turn." → Colors: ["U"], Duration: untilEndOfTurn</item>
/// </list>
/// </para>
///
/// <para>
/// Colors are encoded as one-character color codes matching the oracle-text color
/// name: white = "W", blue = "U", black = "B", red = "R", green = "G",
/// colorless = "C". The "colorless" case is included per CR 105.3 ("give a color
/// to a colorless object") and its inverse (making a colored object colorless),
/// encoded as Colors: [] (empty list) — but for "becomes colorless" the single
/// entry "C" is used for clarity.
/// </para>
///
/// <para>
/// MAST is descriptive: this node records what the oracle line says. The rules
/// engine is responsible for how layer-5 color changes interact with other
/// continuous effects (CR 613.7).
/// </para>
/// </summary>
[OracleEffect("changeColor")]
public sealed record ChangeColorEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent whose color is being changed.
  /// Typically <see cref="ObjectReferenceKind.Target"/> for activated-ability lines
  /// such as "Target creature becomes blue until end of turn."
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The color(s) the target becomes. Encoded as one-character WUBRG codes (e.g.
  /// ["U"] for blue, ["W", "U"] for white and blue). An empty list encodes the
  /// "becomes colorless" variant. Follows the color-code convention used throughout
  /// the MAST schema (e.g., <c>ColorsAttribute</c>, <c>ObjectFilter.Colors</c>).
  /// </summary>
  public required IReadOnlyList<string> Colors { get; init; }
}
