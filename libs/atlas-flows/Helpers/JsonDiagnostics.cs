using System.Text.Json;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Helpers;

/// <summary>
/// Helper for diagnosing JSON deserialization issues.
/// </summary>
public static class JsonDiagnostics
{
  /// <summary>
  /// Attempts to deserialize each card individually and reports which ones fail.
  /// </summary>
  public static async Task<List<RawScryfallCard>> DeserializeCardsWithDiagnostics(string filePath)
  {
    var cards = new List<RawScryfallCard>();
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };

    using var stream = File.OpenRead(filePath);
    using var document = await JsonDocument.ParseAsync(stream);

    var root = document.RootElement;
    if (root.ValueKind != JsonValueKind.Array)
    {
      throw new InvalidOperationException($"Expected JSON array, got {root.ValueKind}");
    }

    int index = 0;
    foreach (var element in root.EnumerateArray())
    {
      try
      {
        var json = element.GetRawText();
        var card = JsonSerializer.Deserialize<RawScryfallCard>(json, options);
        if (card != null)
        {
          cards.Add(card);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to deserialize card at index {index}:");
        Console.WriteLine($"  Error: {ex.Message}");
        Console.WriteLine(
          $"  JSON preview: {element.GetRawText()[..Math.Min(500, element.GetRawText().Length)]}"
        );
        Console.WriteLine();

        // Try to get card name if possible
        if (element.TryGetProperty("name", out var nameElement))
        {
          Console.WriteLine($"  Card name: {nameElement.GetString()}");
        }

        throw; // Re-throw to stop processing
      }

      index++;
      if (index % 1000 == 0)
      {
        Console.WriteLine($"Successfully processed {index} cards...");
      }
    }

    Console.WriteLine($"Successfully deserialized all {cards.Count} cards!");
    return cards;
  }
}
