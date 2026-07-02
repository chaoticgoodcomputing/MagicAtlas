namespace MagicAST.Query;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Deterministic, key-sorted JSON serialization — the cross-engine identity basis (magic-ast
/// ADR-0008). This is what a capture binding and (later) a shape-hash index key are computed from.
/// Never use record <c>GetHashCode</c> for identity: .NET string hashing is per-process randomized,
/// so it is neither reproducible across runs nor portable to a second-language engine.
/// </summary>
public static class CanonicalJson
{
  public static string Serialize(JsonNode? node)
  {
    var sb = new StringBuilder();
    Write(node, sb);
    return sb.ToString();
  }

  /// <summary>
  /// Stable, cross-process content hash (lowercase-hex SHA-256) of the canonical serialization —
  /// the basis for the interaction engine's port-identity scheme (a port keyed by its
  /// canonical-subtree hash, mast-interaction ADR-0001). Key-order-invariant and reproducible
  /// across runs/languages, unlike record <c>GetHashCode</c>.
  /// </summary>
  public static string Hash(JsonNode? node) =>
    Convert
      .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Serialize(node))))
      .ToLowerInvariant();

  private static void Write(JsonNode? node, StringBuilder sb)
  {
    switch (node)
    {
      case null:
        sb.Append("null");
        break;
      case JsonObject obj:
        sb.Append('{');
        var first = true;
        foreach (var kv in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
          if (!first)
            sb.Append(',');
          first = false;
          sb.Append(JsonSerializer.Serialize(kv.Key));
          sb.Append(':');
          Write(kv.Value, sb);
        }
        sb.Append('}');
        break;
      case JsonArray arr:
        sb.Append('[');
        for (var i = 0; i < arr.Count; i++)
        {
          if (i > 0)
            sb.Append(',');
          Write(arr[i], sb);
        }
        sb.Append(']');
        break;
      default:
        sb.Append(node.ToJsonString());
        break;
    }
  }
}
