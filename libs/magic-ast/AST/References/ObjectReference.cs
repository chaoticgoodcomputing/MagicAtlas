namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// A reference to an object in the game.
/// e.g., "target creature", "this creature", "it", "you"
/// </summary>
public sealed record ObjectReference
{
  /// <summary>
  /// The kind of reference.
  /// </summary>
  [JsonPropertyName("kind")]
  public required ObjectReferenceKind Kind { get; init; }

  /// <summary>
  /// Optional filter describing what objects this refers to.
  /// </summary>
  [JsonPropertyName("filter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  // Factory methods
  public static ObjectReference Self() => new() { Kind = ObjectReferenceKind.Self };

  public static ObjectReference Target(ObjectFilter filter) =>
    new() { Kind = ObjectReferenceKind.Target, Filter = filter };

  public static ObjectReference It() => new() { Kind = ObjectReferenceKind.It };

  public static ObjectReference You() => new() { Kind = ObjectReferenceKind.You };
}

/// <summary>
/// The kind of object reference.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ObjectReferenceKind
{
  /// <summary>"this creature", "this permanent"</summary>
  Self,

  /// <summary>"target creature", "target player"</summary>
  Target,

  /// <summary>"it" - refers to a previously mentioned object</summary>
  It,

  /// <summary>"you" - the controller of the ability</summary>
  You,

  /// <summary>"an opponent", "target opponent"</summary>
  Opponent,

  /// <summary>"each opponent"</summary>
  EachOpponent,

  /// <summary>"each player"</summary>
  EachPlayer,

  /// <summary>"any target" - creature, player, or planeswalker</summary>
  AnyTarget,

  /// <summary>"another creature", "another target"</summary>
  Another,

  /// <summary>"all creatures", "each creature"</summary>
  Each,

  /// <summary>"its controller"</summary>
  Controller,

  /// <summary>"its owner"</summary>
  Owner,

  /// <summary>"the defending player"</summary>
  DefendingPlayer,

  /// <summary>"enchanted creature", "equipped creature"</summary>
  EnchantedOrEquipped,

  /// <summary>"chosen creature" - from a choice earlier</summary>
  Chosen,

  /// <summary>"that player" - the player who triggered an ability or was mentioned earlier</summary>
  ThatPlayer,

  /// <summary>"each other player" - all players except the controller</summary>
  EachOtherPlayer,
}
