namespace MagicAST.Parsing;

using MagicAST.Parsing.Tokens;
using Superpower.Model;
// Use our own TextSpan, not Superpower's
using TextSpan = MagicAST.AST.TextSpan;

/// <summary>
/// Represents a clause (ability segment) extracted from oracle text.
/// Each clause typically represents a single ability.
/// </summary>
public sealed record OracleClause
{
  /// <summary>
  /// The tokens in this clause.
  /// </summary>
  public required TokenList<OracleToken> Tokens { get; init; }

  /// <summary>
  /// The raw text of this clause.
  /// </summary>
  public required string RawText { get; init; }

  /// <summary>
  /// The span of this clause in the original oracle text.
  /// </summary>
  public required TextSpan SourceSpan { get; init; }

  /// <summary>
  /// Whether this clause is a modal option (part of a "Choose" ability).
  /// </summary>
  public bool IsModalOption { get; init; }

  /// <summary>
  /// For level-up cards, the level range this clause applies to.
  /// </summary>
  public (int Min, int Max)? LevelRange { get; init; }
}

/// <summary>
/// Splits oracle text into individual ability clauses.
/// Handles paragraph breaks, modal structures, level/chapter markers, and loyalty abilities.
/// </summary>
public sealed class ClauseSplitter
{
  private readonly OracleTokenizer _tokenizer = new();

  /// <summary>
  /// Splits oracle text into individual clauses.
  /// </summary>
  /// <param name="oracleText">The oracle text to split.</param>
  /// <returns>A sequence of clauses.</returns>
  public IReadOnlyList<OracleClause> Split(string oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return [];
    }

    var clauses = new List<OracleClause>();
    var paragraphs = SplitIntoParagraphs(oracleText);

    foreach (var (paragraphText, paragraphStart) in paragraphs)
    {
      var paragraphClauses = ProcessParagraph(paragraphText, paragraphStart);
      clauses.AddRange(paragraphClauses);
    }

    return clauses;
  }

  /// <summary>
  /// Splits oracle text into paragraphs by newline characters.
  /// </summary>
  private static IEnumerable<(string Text, int Start)> SplitIntoParagraphs(string text)
  {
    var start = 0;
    var lines = text.Split('\n');

    foreach (var line in lines)
    {
      var trimmed = line.Trim();
      if (!string.IsNullOrEmpty(trimmed))
      {
        yield return (trimmed, start);
      }

      start += line.Length + 1; // +1 for the newline
    }
  }

  /// <summary>
  /// Processes a single paragraph into one or more clauses.
  /// Handles modal structures and level/chapter markers.
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessParagraph(string paragraphText, int paragraphStart)
  {
    // Check for modal structure: "Choose one —" followed by bullets
    if (IsModalHeader(paragraphText))
    {
      return ProcessModalParagraph(paragraphText, paragraphStart);
    }

    // Check for level-up structure
    var levelRange = TryParseLevelRange(paragraphText);
    if (levelRange.HasValue)
    {
      return ProcessLevelParagraph(paragraphText, paragraphStart, levelRange.Value);
    }

    // Standard single-clause paragraph
    return [CreateClause(paragraphText, paragraphStart)];
  }

  /// <summary>
  /// Checks if a paragraph starts a modal structure.
  /// </summary>
  private static bool IsModalHeader(string text)
  {
    var lower = text.ToLowerInvariant();
    return lower.StartsWith("choose one")
      || lower.StartsWith("choose two")
      || lower.StartsWith("choose three")
      || lower.StartsWith("choose any number")
      || lower.StartsWith("choose up to");
  }

  /// <summary>
  /// Processes a modal paragraph into multiple clauses (header + options).
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessModalParagraph(
    string paragraphText,
    int paragraphStart
  )
  {
    var clauses = new List<OracleClause>();

    // Split by bullet points
    var parts = paragraphText.Split('\u2022'); // •

    // First part is the header (e.g., "Choose one —")
    if (parts.Length > 0)
    {
      var header = parts[0].Trim();
      if (!string.IsNullOrEmpty(header))
      {
        clauses.Add(CreateClause(header, paragraphStart));
      }
    }

    // Remaining parts are modal options
    var offset = parts[0].Length + 1; // +1 for the bullet
    for (var i = 1; i < parts.Length; i++)
    {
      var option = parts[i].Trim();
      if (!string.IsNullOrEmpty(option))
      {
        clauses.Add(CreateClause(option, paragraphStart + offset, isModalOption: true));
      }

      offset += parts[i].Length + 1;
    }

    return clauses;
  }

  /// <summary>
  /// Attempts to parse a level range from level-up card text.
  /// </summary>
  private static (int Min, int Max)? TryParseLevelRange(string text)
  {
    var lower = text.ToLowerInvariant();

    // Match patterns like "LEVEL 1-2", "LEVEL 3-4", "LEVEL 5+"
    if (!lower.StartsWith("level "))
    {
      return null;
    }

    var levelPart = text[6..].Trim();

    // "5+" pattern
    if (levelPart.EndsWith("+"))
    {
      if (int.TryParse(levelPart[..^1], out var min))
      {
        return (min, int.MaxValue);
      }
    }

    // "1-2" pattern
    var dashIndex = levelPart.IndexOf('-');
    if (dashIndex > 0)
    {
      var minStr = levelPart[..dashIndex];
      var maxStr = levelPart[(dashIndex + 1)..];
      if (int.TryParse(minStr, out var min) && int.TryParse(maxStr, out var max))
      {
        return (min, max);
      }
    }

    return null;
  }

  /// <summary>
  /// Processes a level paragraph, extracting the level range.
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessLevelParagraph(
    string paragraphText,
    int paragraphStart,
    (int Min, int Max) levelRange
  )
  {
    // Remove the "LEVEL X-Y" prefix
    var colonIndex = paragraphText.IndexOf(':');
    var contentStart = colonIndex >= 0 ? colonIndex + 1 : 0;
    var content = paragraphText[contentStart..].Trim();

    if (string.IsNullOrEmpty(content))
    {
      return [];
    }

    var clause = CreateClause(content, paragraphStart + contentStart);
    return [clause with { LevelRange = levelRange }];
  }

  /// <summary>
  /// Creates a clause from text, tokenizing it.
  /// </summary>
  private OracleClause CreateClause(string text, int startOffset, bool isModalOption = false)
  {
    var tokenResult = _tokenizer.TryTokenize(text);

    TokenList<OracleToken> tokens;
    if (tokenResult.HasValue)
    {
      tokens = tokenResult.Value;
    }
    else
    {
      // Empty token list for failed tokenization
      tokens = new TokenList<OracleToken>([]);
    }

    return new OracleClause
    {
      Tokens = tokens,
      RawText = text,
      SourceSpan = new TextSpan(startOffset, text.Length),
      IsModalOption = isModalOption,
    };
  }
}
