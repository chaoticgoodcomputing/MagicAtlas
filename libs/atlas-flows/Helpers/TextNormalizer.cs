namespace MagicAtlas.Helpers;

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
    return text.Replace('\u2018', '\'') // Left single quote
      .Replace('\u2019', '\'') // Right single quote (apostrophe)
      .Replace('\u201C', '"') // Left double quote
      .Replace('\u201D', '"') // Right double quote
      .Replace('\u0022', '"') // Quotation mark
      .Replace('\u2013', '-') // En dash
      .Replace('\u2014', '-') // Em dash
      .Replace('\u2022', '*') // Bullet point
      .Replace('\u002B', '+') // Plus sign
      .Replace('\u0027', '\''); // Apostrophe
  }
}
