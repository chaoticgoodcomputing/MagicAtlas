using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Nested representation of the Scryfall-tag curation, with each canonical entry positioned in
/// the tree implied by its colon-delimited slug (<c>removal:creature</c> becomes a child of
/// <c>removal</c>; <c>tribal:elf</c> becomes a child of <c>tribal</c>).
/// </summary>
/// <remarks>
/// Built by <c>BuildTagHierarchyNode</c> from <c>Catalog.ScryfallTagCuration</c>. Persisted as
/// JSON (nesting is the whole point of this shape, and it's never a Python step input). The
/// sibling Mermaid output renders the same tree as a flowchart for human eyes.
/// </remarks>
[FlowthruSchema]
public partial record TagHierarchyNode
{
  /// <summary>Full colon-delimited path from the curation (e.g. <c>removal:creature</c>).</summary>
  [SerializedLabel("slug")]
  public required string Slug { get; init; }

  /// <summary>Display name from the curation, or a synthesized one for "ghost" parents created
  /// when a sub exists without an explicit parent entry.</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("category")]
  public string? Category { get; init; }

  [SerializedLabel("description")]
  public string? Description { get; init; }

  /// <summary>Number of Scryfall aliases mapped into this canonical. <c>0</c> for ghost parents.</summary>
  [SerializedLabel("alias_count")]
  public int AliasCount { get; init; }

  /// <summary>Whether this node was created as a synthetic ancestor (a curation entry exists
  /// for <c>tribal:elf</c> but not <c>tribal</c>, so <c>tribal</c> shows up as a ghost). Lets
  /// reporting style ghost vs. real differently.</summary>
  [SerializedLabel("is_ghost")]
  public bool IsGhost { get; init; }

  [SerializedLabel("children")]
  public List<TagHierarchyNode> Children { get; init; } = new();
}
