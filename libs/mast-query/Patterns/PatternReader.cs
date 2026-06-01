namespace MagicAST.Query.Patterns;

using System.Text.Json.Nodes;

/// <summary>
/// Reads the JSON pattern form (mast-query ADR-0001) into a <see cref="Pattern"/> tree.
/// Recognised tokens: <c>$descendant</c>, <c>$type</c>, <c>$capture</c>, <c>$in</c>, <c>$any</c>.
/// Any other object key is a field constraint.
/// </summary>
public static class PatternReader
{
  public static Pattern Read(JsonNode node) =>
    node switch
    {
      JsonValue v => v.ToString() == "$any" ? new AnyPattern() : new ScalarEqPattern(v.ToString()),
      JsonObject obj => ReadObject(obj),
      JsonArray => throw new FormatException("A bare array is not a pattern; use { \"$in\": [...] }."),
      _ => throw new FormatException("null is not a valid pattern."),
    };

  private static Pattern ReadObject(JsonObject obj)
  {
    if (obj.TryGetPropertyValue("$descendant", out var inner) && inner is not null)
      return new AnyDepthPattern(Read(inner));

    if (obj.TryGetPropertyValue("$in", out var arr) && arr is JsonArray values)
      return new ScalarInPattern(values.Select(x => x!.ToString()).ToArray());

    string? type = obj.TryGetPropertyValue("$type", out var t) ? t!.ToString() : null;
    string? capture = obj.TryGetPropertyValue("$capture", out var c) ? c!.ToString() : null;

    var fields = new List<FieldConstraint>();
    foreach (var kv in obj)
    {
      if (kv.Key is "$type" or "$capture")
        continue;
      fields.Add(new FieldConstraint(kv.Key, Read(kv.Value!)));
    }

    return new NodePattern
    {
      TypeName = type,
      Capture = capture,
      Fields = fields.Count > 0 ? fields : null,
    };
  }
}
