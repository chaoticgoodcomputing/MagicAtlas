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
}
