namespace MagicAST.Schema;

/// <summary>
/// The machine-readable description of MAST's polymorphic node model (magic-ast ADR-0008): the
/// discriminated hierarchies, their JSON discriminator keys, the discriminator value of each
/// concrete type, and which types are <c>IUnparsed</c>. This is the contract downstream query
/// engines bind to — the C# engine consumes <see cref="SchemaExport.Build"/> in-process; other
/// languages consume the serialized <c>schema/ast-schema.json</c>. A conformance test keeps the
/// two identical, so a parser change is a loud failure rather than silent drift.
/// </summary>
public sealed record AstSchema
{
  /// <summary>Schema format version, bumped when the export's shape changes.</summary>
  public int SchemaVersion { get; init; } = 1;

  /// <summary>Every distinct discriminator JSON property name across all hierarchies (sorted). A
  /// query engine reads these to know which keys carry a node's type.</summary>
  public required IReadOnlyList<string> DiscriminatorKeys { get; init; }

  /// <summary>The (key, value) pairs that identify an <c>IUnparsed</c> node — a parse hole. A match
  /// over a subtree containing one of these is <c>Unknown</c>, never a silent <c>NoMatch</c>.</summary>
  public required IReadOnlyList<UnparsedDiscriminator> UnparsedDiscriminators { get; init; }

  /// <summary>The polymorphic hierarchies (sorted by base type name).</summary>
  public required IReadOnlyList<SchemaBase> Bases { get; init; }

  /// <summary>SHA-256 (lowercase hex) of the canonical, hash-excluded body — a pinnable version handle.</summary>
  public string? SchemaHash { get; init; }
}

/// <summary>A discriminator <c>(key, value)</c> pair that marks an <c>IUnparsed</c> node.</summary>
public sealed record UnparsedDiscriminator
{
  public required string Key { get; init; }
  public required string Value { get; init; }
}

/// <summary>One polymorphic hierarchy: an abstract base, its discriminator key, and its concrete types.</summary>
public sealed record SchemaBase
{
  public required string Type { get; init; }
  public required string DiscriminatorKey { get; init; }
  public required IReadOnlyList<SchemaType> Types { get; init; }
}

/// <summary>One concrete node type: its CLR name, its discriminator value, whether it is a parse
/// hole, and its serialized field names.</summary>
public sealed record SchemaType
{
  public required string Type { get; init; }
  public required string Discriminator { get; init; }
  public bool IsUnparsed { get; init; }
  public required IReadOnlyList<string> Fields { get; init; }
}
