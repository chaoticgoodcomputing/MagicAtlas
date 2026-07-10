namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The kind of quality for protection.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProtectionQualityKind
{
  /// <summary>A color: white, blue, black, red, green</summary>
  Color,

  /// <summary>A card type: creature, artifact, enchantment, etc.</summary>
  CardType,

  /// <summary>A subtype: Demon, Dragon, Human, Equipment, etc.</summary>
  Subtype,

  /// <summary>A characteristic: "multicolored", "monocolored", etc.</summary>
  Characteristic,

  /// <summary>Everything (protection from everything)</summary>
  Everything,

  /// <summary>
  /// A color chosen by the controller at resolution time, i.e. "protection from
  /// the color of your choice". The specific color is not known at parse time —
  /// oracle text records the instruction to choose, not the chosen value.
  /// </summary>
  ChosenColor,

  /// <summary>
  /// A characteristic value chosen earlier by a linked "As this [permanent]
  /// enters, choose a [characteristic]." ability (CR 607) that this protection
  /// quality refers back to, e.g. "protection from the chosen color" (Floating
  /// Shield) — the DEFINITE back-reference to a single already-bound value, every
  /// mention on the card naming the identical color. Distinct from
  /// <see cref="ChosenColor"/>'s "the color of your choice" (a fresh choice made
  /// anew each time that ability resolves, with no persistent binding). See
  /// <see cref="ProtectionQuality.ChosenCharacteristic"/> for the axis (color,
  /// creature type, …), mirroring
  /// <see cref="MagicAST.AST.References.ObjectFilter.ChosenCharacteristic"/>'s
  /// object-reference analogue.
  /// </summary>
  ChosenCharacteristic,
}
