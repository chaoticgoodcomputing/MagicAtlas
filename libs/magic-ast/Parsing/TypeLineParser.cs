namespace MagicAST.Parsing;

/// <summary>
/// Parses a card's type line into its structural components.
/// Type lines follow the format: "[Supertypes] [Types] — [Subtypes]"
///
/// Examples:
/// - "Creature — Elf Druid" → types: [Creature], subtypes: [Elf, Druid]
/// - "Legendary Creature — Squirrel Warrior" → supertypes: [Legendary], types: [Creature], subtypes: [Squirrel, Warrior]
/// - "Artifact" → types: [Artifact]
/// - "Basic Land — Forest" → supertypes: [Basic], types: [Land], subtypes: [Forest]
/// - "Legendary Planeswalker — Daretti" → supertypes: [Legendary], types: [Planeswalker], subtypes: [Daretti]
/// </summary>
public sealed class TypeLineParser
{
  // Known supertypes in MTG
  private static readonly HashSet<string> _knownSupertypes =
    new(StringComparer.OrdinalIgnoreCase) { "Basic", "Legendary", "Ongoing", "Snow", "World" };

  // Known card types in MTG
  private static readonly HashSet<string> _knownTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "Artifact",
      "Battle",
      "Conspiracy",
      "Creature",
      "Dungeon",
      "Emblem",
      "Enchantment",
      "Hero",
      "Instant",
      "Kindred", // Formerly "Tribal"
      "Land",
      "Phenomenon",
      "Plane",
      "Planeswalker",
      "Scheme",
      "Sorcery",
      "Vanguard",
    };

  /// <summary>
  /// Parses a type line string into a TypeLineAST.
  /// </summary>
  /// <param name="typeLine">The type line string to parse.</param>
  /// <returns>A TypeLineAST with parsed components.</returns>
  public TypeLineAST Parse(string typeLine)
  {
    ArgumentNullException.ThrowIfNull(typeLine);

    // Split on em-dash (—) or double-dash (--) to separate types from subtypes
    var parts = SplitOnDash(typeLine);
    var typesPart = parts.typesPart.Trim();
    var subtypesPart = parts.subtypesPart?.Trim();

    // Parse the types part (supertypes + types)
    var words = typesPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var supertypes = new List<string>();
    var types = new List<string>();

    foreach (var word in words)
    {
      if (_knownSupertypes.Contains(word))
      {
        supertypes.Add(word);
      }
      else if (_knownTypes.Contains(word))
      {
        types.Add(word);
      }
      else
      {
        // Unknown word in types section - treat as type
        // This handles potential future card types
        types.Add(word);
      }
    }

    // Parse subtypes (everything after the dash, space-separated)
    List<string>? subtypes = null;
    if (!string.IsNullOrWhiteSpace(subtypesPart))
    {
      subtypes = subtypesPart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    return new TypeLineAST
    {
      Raw = typeLine,
      Supertypes = supertypes.Count > 0 ? supertypes : null,
      Types = types,
      Subtypes = subtypes?.Count > 0 ? subtypes : null,
    };
  }

  /// <summary>
  /// Splits the type line on the dash separator.
  /// Handles both em-dash (—) and double-dash (--).
  /// </summary>
  private static (string typesPart, string? subtypesPart) SplitOnDash(string typeLine)
  {
    // Try em-dash first (standard MTG format)
    var emDashIndex = typeLine.IndexOf('—');
    if (emDashIndex >= 0)
    {
      return (
        typeLine[..emDashIndex],
        emDashIndex < typeLine.Length - 1 ? typeLine[(emDashIndex + 1)..] : null
      );
    }

    // Try double-dash as fallback
    var doubleDashIndex = typeLine.IndexOf("--", StringComparison.Ordinal);
    if (doubleDashIndex >= 0)
    {
      return (
        typeLine[..doubleDashIndex],
        doubleDashIndex < typeLine.Length - 2 ? typeLine[(doubleDashIndex + 2)..] : null
      );
    }

    // No subtypes
    return (typeLine, null);
  }
}
