namespace MagicAST.AST.Effects.Traits;

using System.Text.Json.Serialization;

/// <summary>
/// Describes an effect whose oracle text carries an "unless [player] pays
/// [cost]" clause — a syntactic pattern where the effect can be prevented
/// by paying a stated cost. Common on cards like Rhystic Study, Mystic
/// Remora, and Ward.
///
/// <para>This trait records the *presence and shape* of the unless clause
/// in oracle text. The decision-and-payment workflow is a runtime concern
/// for consumers of the AST — MAST is not a rules engine.</para>
/// </summary>
public interface IPreventableEffect
{
  /// <summary>
  /// The "unless [player] pays [cost]" clause attached to this effect,
  /// or null if no such clause appears in the oracle text.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  UnlessClause? UnlessClause { get; init; }
}
