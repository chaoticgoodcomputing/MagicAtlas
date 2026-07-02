namespace MagicAtlas.Rules.Helpers;

/// <summary>
/// Provides text normalization utilities for consistent output formatting.
/// </summary>
public static class TextNormalizer
{
  /// <summary>
  /// Normalizes Unicode characters (curly quotes, apostrophes, dashes) to ASCII equivalents.
  /// </summary>
  /// <param name="text">The text to normalize.</param>
  /// <returns>The normalized text with ASCII characters.</returns>
  public static string NormalizeText(string text)
  {
    return text.Replace("‘", "'") // Left single quote
      .Replace("’", "'") // Right single quote (apostrophe)
      .Replace("“", "\"") // Left double quote
      .Replace("”", "\"") // Right double quote
      .Replace("–", "-") // En dash
      .Replace("—", "-") // Em dash
      .Replace("•", "*"); // Bullet point
  }
}
