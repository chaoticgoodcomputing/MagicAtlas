namespace MagicAST.Query.Patterns;

/// <summary>
/// A declarative match-tree node (mast-query ADR-0001). Patterns are data — authored as JSON —
/// and evaluated against the canonical serialization of a card AST. The match algebra here is the
/// minimal staged set; predicates, quantifiers, and the cross-query join layer are added as
/// fixtures demand them.
/// </summary>
public abstract record Pattern;

/// <summary>
/// Matches a JSON object. When <see cref="TypeName"/> is set, the object's polymorphic
/// discriminator (e.g. <c>EffectType</c>, <c>Kind</c>) must equal it; when null, only the field
/// constraints apply — an un-discriminated object such as an <c>ObjectReference</c> or
/// <c>ObjectFilter</c>. Each entry in <see cref="Fields"/> must be present and match its
/// sub-pattern. A non-null <see cref="Capture"/> binds the matched node for later reference.
/// </summary>
public sealed record NodePattern : Pattern
{
  public string? TypeName { get; init; }
  public string? Capture { get; init; }
  public IReadOnlyList<FieldConstraint>? Fields { get; init; }
}

/// <summary>A named field of a <see cref="NodePattern"/> and the sub-pattern its value must match.</summary>
public sealed record FieldConstraint(string Field, Pattern Value);

/// <summary>Wildcard: matches any present node (<c>$any</c>).</summary>
public sealed record AnyPattern : Pattern;

/// <summary>Matches if <see cref="Inner"/> matches this node or any descendant (<c>$descendant</c> / <c>//</c>).</summary>
public sealed record AnyDepthPattern(Pattern Inner) : Pattern;

/// <summary>Matches a JSON leaf whose text equals <see cref="Value"/>.</summary>
public sealed record ScalarEqPattern(string Value) : Pattern;

/// <summary>Matches a JSON leaf whose text is one of <see cref="Values"/> (<c>$in</c>).</summary>
public sealed record ScalarInPattern(IReadOnlyList<string> Values) : Pattern;
