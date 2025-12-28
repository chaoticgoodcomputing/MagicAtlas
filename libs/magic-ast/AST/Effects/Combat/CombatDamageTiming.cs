namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Combat damage timing options.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CombatDamageTiming
{
  /// <summary>Deals damage in the first strike combat damage step only.</summary>
  First,

  /// <summary>Deals damage in both first strike and normal combat damage steps.</summary>
  Both,
}
