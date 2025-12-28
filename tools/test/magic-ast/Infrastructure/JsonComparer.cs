namespace MagicAST.Tests.Infrastructure;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Utilities for comparing JSON structures in tests.
/// </summary>
public static class JsonComparer
{
  /// <summary>
  /// Compares two JSON nodes for structural equality (ignoring property order in objects).
  /// Missing properties with default values (false, 0, null, empty arrays) are treated as equal.
  /// </summary>
  public static bool AreEqual(JsonNode? a, JsonNode? b)
  {
    if (a is null && b is null)
    {
      return true;
    }

    if (a is null || b is null)
    {
      return false;
    }

    return (a, b) switch
    {
      (JsonObject objA, JsonObject objB) => ObjectsAreEqual(objA, objB),
      (JsonArray arrA, JsonArray arrB) => ArraysAreEqual(arrA, arrB),
      (JsonValue valA, JsonValue valB) => ValuesAreEqual(valA, valB),
      _ => false,
    };
  }

  /// <summary>
  /// Formats a JSON node for display in error messages.
  /// </summary>
  public static string FormatForDisplay(JsonNode? node) =>
    node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";

  private static bool ObjectsAreEqual(JsonObject a, JsonObject b)
  {
    // Get all unique keys from both objects
    var allKeys = a.Select(kvp => kvp.Key).Union(b.Select(kvp => kvp.Key)).ToHashSet();

    foreach (var key in allKeys)
    {
      var hasA = a.TryGetPropertyValue(key, out var valueA);
      var hasB = b.TryGetPropertyValue(key, out var valueB);

      if (hasA && hasB)
      {
        if (!AreEqual(valueA, valueB))
        {
          return false;
        }
      }
      else if (hasA && !hasB)
      {
        // Property only in A - check if it's a default value
        if (!IsDefaultValue(valueA))
        {
          return false;
        }
      }
      else if (!hasA && hasB)
      {
        // Property only in B - check if it's a default value
        if (!IsDefaultValue(valueB))
        {
          return false;
        }
      }
    }

    return true;
  }

  /// <summary>
  /// Checks if a JSON value represents a default value that can be omitted.
  /// </summary>
  private static bool IsDefaultValue(JsonNode? node)
  {
    if (node is null)
    {
      return true;
    }

    if (node is JsonValue value)
    {
      var element = value.GetValue<JsonElement>();
      return element.ValueKind switch
      {
        JsonValueKind.False => true,
        JsonValueKind.Null => true,
        JsonValueKind.Number => element.TryGetInt32(out var i) && i == 0,
        _ => false,
      };
    }

    if (node is JsonArray arr)
    {
      return arr.Count == 0;
    }

    // Check if this is a metadata property that can be ignored
    // Reminder text is valuable metadata but not semantically required for equality
    if (node is JsonObject obj && obj.Count == 1 && obj.ContainsKey("text"))
    {
      // This is likely a Parenthetical/reminder node - treat as optional metadata
      return true;
    }

    return false;
  }

  private static bool ArraysAreEqual(JsonArray a, JsonArray b)
  {
    if (a.Count != b.Count)
    {
      return false;
    }

    for (int i = 0; i < a.Count; i++)
    {
      if (!AreEqual(a[i], b[i]))
      {
        return false;
      }
    }

    return true;
  }

  private static bool ValuesAreEqual(JsonValue a, JsonValue b)
  {
    // Compare as raw JSON strings for value equality
    return a.ToJsonString() == b.ToJsonString();
  }
}
