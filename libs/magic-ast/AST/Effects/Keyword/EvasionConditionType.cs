namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Types of evasion conditions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EvasionConditionType
{
  /// <summary>"as long as defending player controls a [permanent]" (landwalk)</summary>
  DefendingPlayerControls,

  /// <summary>"as long as defending player controls no [permanent]"</summary>
  DefendingPlayerControlsNone,

  /// <summary>"as long as you control a [permanent]"</summary>
  YouControl,
}
