namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "search your library for [filter]"
/// </summary>
[OracleEffect("searchLibrary")]
public sealed record SearchLibraryEffect : Effect
{
  public required ObjectFilter Filter { get; init; }

  public required Quantity Count { get; init; }

  /// <summary>
  /// Zones searched, e.g. "your library and/or graveyard". When omitted, the
  /// search is library-only (the default Rule 701.23 case, and the shape every
  /// pre-existing fixture carries). Present only when the oracle names additional
  /// or alternative source zones beyond the library.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Zone>? Sources { get; init; }

  public required SearchDestination Destination { get; init; }

  public bool Revealed { get; init; }
}
