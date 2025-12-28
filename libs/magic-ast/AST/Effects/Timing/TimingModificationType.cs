namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Whether a timing modification grants or restricts timing.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimingModificationType
{
  /// <summary>Grants expanded timing (e.g., Flash grants instant speed)</summary>
  Grant,

  /// <summary>Restricts to specific timing (e.g., "only as a sorcery")</summary>
  Restrict,
}
