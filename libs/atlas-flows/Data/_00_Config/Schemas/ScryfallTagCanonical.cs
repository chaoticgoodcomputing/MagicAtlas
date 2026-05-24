using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// One canonical archetype in the Scryfall-tag curation allowlist. Each entry merges N Scryfall
/// otag slugs (its <see cref="Aliases"/>) under one canonical name. Colon syntax in the
/// <see cref="CanonicalSlug"/> encodes hierarchy (<c>removal:creature</c> is a sub-archetype
/// of <c>removal</c>), parsed by the hierarchy-builder downstream.
/// </summary>
[FlowthruSchema]
public partial record ScryfallTagCanonical
{
  [SerializedLabel("canonical_slug")]
  public required string CanonicalSlug { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("category")]
  public required string Category { get; init; }

  [SerializedLabel("description")]
  public string Description { get; init; } = "";

  [SerializedLabel("aliases")]
  public List<string> Aliases { get; init; } = new();

  /// <summary>
  /// Optional regex patterns that deterministically attribute matching oracle lines to this
  /// canonical (Pass 0 in <c>build_canonical_line_assignments</c>). Patterns are matched against
  /// the line's text — including the synthetic single-keyword lines emitted by
  /// <c>ProjectOracleLines</c>, which makes keyword-style patterns trivial (e.g. <c>^Flying$</c>
  /// matches the synthetic "Flying" line on every flying creature). Attributions emitted via
  /// pattern carry <c>source="pattern"</c> and <c>confidence=1.0</c>, short-circuiting the
  /// inference pass for the same (line, canonical) pair. Hierarchy applies: an
  /// <c>evasion:flying</c> attribution participates in <c>evasion</c> parent-level groupings via
  /// <c>canonical_family</c>.
  /// </summary>
  [SerializedLabel("patterns")]
  public List<string> Patterns { get; init; } = new();
}
