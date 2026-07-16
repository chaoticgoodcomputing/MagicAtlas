namespace MagicAST.Interaction;

using System.Text;
using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 2 — the <b>structured</b> form of a port, emitted ALONGSIDE the legacy colon-label
/// (dual-emit). A port is <c>side : stem [attribute-set]</c> (ADR-0003 §2): the <see cref="Stem"/> is the
/// event-named is-a spine (<c>removal:creature</c>, <c>deployment:creature</c>, <c>mana</c>), and
/// <see cref="Attributes"/> is the unordered set of orthogonal facets (<c>manner</c>, <c>from</c>/<c>to</c>,
/// <c>control</c>, <c>color</c>, …), each carrying <see cref="Provenance"/>.
///
/// <para><b>Migration role.</b> Matching moves onto this structure at Stage 3; until then the legacy engine
/// still matches on the string label. The invariant Stage 2 must hold (the byte-for-byte gate) is
/// <c>ToLegacyLabel(structure, subject) == &lt;the label the ADR-0002 generator produced&gt;</c> for every
/// port — i.e. the structure is a lossless superset of today's label. <see cref="ToLegacyLabel"/> is the
/// compatibility shim that encodes the ADR-2 ↔ ADR-3 correspondence per family; it is deleted at Stage 4
/// cutover, when the label becomes a direct ADR-3 serialization.</para>
/// </summary>
public sealed record PortStructure
{
  public required PortSide Side { get; init; }

  /// <summary>The is-a spine — <c>&lt;supergroup&gt;:&lt;card-type&gt;</c> or a scalar-resource stem
  /// (<c>mana</c>). Colon-nested ONLY for genuine is-a descent (ADR-0003 §2).</summary>
  public required string Stem { get; init; }

  /// <summary>The unordered attribute set on the leaf — orthogonal facets, never colon-nested. Held as a
  /// list sorted by <see cref="PortAttribute.Key"/> for deterministic serialization (an unordered set with
  /// a canonical order).</summary>
  public IReadOnlyList<PortAttribute> Attributes { get; init; } = [];

  /// <summary>Look up an attribute value by key (null if absent — an open-world unknown, ADR-0003 §3).</summary>
  public string? Attr(string key) =>
    Attributes.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.Ordinal))?.Value;

  /// <summary>Canonical ADR-3 serialization (display/debug) — <c>side:stem[k=v,k=v]</c> with attributes in
  /// key order. NOT the legacy label; see <see cref="ToLegacyLabel"/>. Provenance/polarity are omitted here
  /// (they live in the rollup, not the port identity).</summary>
  public string Canonical()
  {
    var sb = new StringBuilder();
    sb.Append(Side.ToString().ToLowerInvariant()).Append(':').Append(Stem);
    if (Attributes.Count > 0)
    {
      sb.Append('[');
      sb.Append(string.Join(",", Attributes.OrderBy(a => a.Key, StringComparer.Ordinal).Select(a => $"{a.Key}={a.Value}")));
      sb.Append(']');
    }
    return sb.ToString();
  }

  /// <summary>Convenience builder — a structure with attributes given as (key, value) asserted pairs.</summary>
  public static PortStructure Of(PortSide side, string stem, params (string Key, string Value)[] attrs) =>
    new()
    {
      Side = side,
      Stem = stem,
      Attributes = attrs.Select(a => new PortAttribute { Key = a.Key, Value = a.Value }).ToList(),
    };
}

/// <summary>One attribute instance on a port (ADR-0003 §2/§7): a key/value plus its provenance and (for
/// producer-choice axes, §6) its polarity.</summary>
public sealed record PortAttribute
{
  public required string Key { get; init; }
  public required string Value { get; init; }

  /// <summary>Asserted (the parse states it) vs Derived (over-approximated — caps Reliability at Unknown,
  /// ADR-0003 §7).</summary>
  public Provenance Provenance { get; init; } = Provenance.Asserted;

  /// <summary>The §6 polarity for producer-choice axes (e.g. <c>producer-choice</c> on <c>color</c>); null
  /// for a fixed value.</summary>
  public string? Polarity { get; init; }
}

/// <summary>ADR-0003 §7 — whether a projected attribute value is asserted by the parse or derived
/// (over-approximated by the projection).</summary>
public enum Provenance
{
  Asserted,
  Derived,
}
