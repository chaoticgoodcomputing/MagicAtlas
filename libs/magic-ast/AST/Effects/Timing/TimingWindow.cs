namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The timing window for casting or activation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimingWindow
{
  /// <summary>Any time you could cast an instant (instant speed)</summary>
  Instant,

  /// <summary>Only when you could cast a sorcery (main phase, stack empty, your turn)</summary>
  Sorcery,

  /// <summary>During a specific phase (see Phase property)</summary>
  Phase,

  /// <summary>Any player's turn</summary>
  AnyTurn,
}
