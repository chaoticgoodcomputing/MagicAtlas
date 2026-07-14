namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Diagnostics;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents an ability that could not be fully parsed.
/// Always contains the raw text and diagnostics explaining the failure.
/// </summary>
[OracleAbility("unparsed")]
public sealed record UnparsedAbility : Ability, IUnparsed
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Unparsed;

  /// <summary>
  /// Non-null projection of the inherited <see cref="Ability.SourceSpan"/> for the
  /// <see cref="IUnparsed"/> contract, which needs a concrete span to attribute the
  /// failure to its oracle line. Every unparsed node is constructed with a span (and
  /// the parser stamps top-level ones), so this is effectively always the real span;
  /// the <c>?? Empty</c> is a defensive floor. An explicit interface implementation —
  /// it is NOT a serialized property, so the single serialized SourceSpan remains the
  /// inherited <see cref="Ability.SourceSpan"/> and there is no duplicate key.
  /// </summary>
  MagicAST.AST.TextSpan MagicAST.AST.IUnparsed.SourceSpan =>
    SourceSpan ?? MagicAST.AST.TextSpan.Empty;

  /// <summary>
  /// The raw text that could not be parsed.
  /// </summary>
  public required string RawText { get; init; }

  /// <summary>
  /// Diagnostics explaining why parsing failed.
  /// </summary>
  public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// If parsing partially succeeded, the best-effort result.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Ability? PartialParse { get; init; }
}
