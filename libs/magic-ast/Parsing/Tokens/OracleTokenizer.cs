namespace MagicAST.Parsing.Tokens;

using Superpower;
using Superpower.Model;
using Superpower.Parsers;

/// <summary>
/// Tokenizer for Magic: The Gathering oracle text.
/// Transforms character streams into token streams for parsing.
/// </summary>
public sealed class OracleTokenizer : Tokenizer<OracleToken>
{
  /// <summary>
  /// Structural keywords that get special tokens.
  /// Matched case-insensitively, longest match first.
  /// </summary>
  private static readonly (string Text, OracleToken Token)[] _structuralKeywords =
  [
    // Trigger timing (must come before shorter words)
    ("Whenever", OracleToken.Whenever),
    ("When", OracleToken.When),
    ("At", OracleToken.At),
    // Conditionals
    ("Instead", OracleToken.Instead),
    ("Unless", OracleToken.Unless),
    ("Would", OracleToken.Would),
    ("If", OracleToken.If),
    // Modal/Choice
    ("Choose", OracleToken.Choose),
    ("Then", OracleToken.Then),
    ("And", OracleToken.And),
    ("Or", OracleToken.Or),
    // References
    ("Another", OracleToken.Another),
    ("Target", OracleToken.Target),
    ("Each", OracleToken.Each),
    ("This", OracleToken.This),
    ("That", OracleToken.That),
    ("Your", OracleToken.Your),
    ("You", OracleToken.You),
    ("All", OracleToken.All),
    ("Any", OracleToken.Any),
    ("It", OracleToken.It),
  ];

  /// <summary>
  /// Word numbers mapped to their numeric values.
  /// </summary>
  private static readonly Dictionary<string, int> _wordNumbers =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["zero"] = 0,
      ["one"] = 1,
      ["two"] = 2,
      ["three"] = 3,
      ["four"] = 4,
      ["five"] = 5,
      ["six"] = 6,
      ["seven"] = 7,
      ["eight"] = 8,
      ["nine"] = 9,
      ["ten"] = 10,
      ["eleven"] = 11,
      ["twelve"] = 12,
      ["thirteen"] = 13,
      ["fourteen"] = 14,
      ["fifteen"] = 15,
      ["twenty"] = 20,
    };

  /// <summary>
  /// Single-color mana symbols.
  /// </summary>
  private static readonly Dictionary<char, OracleToken> _coloredManaSymbols =
    new()
    {
      ['W'] = OracleToken.WhiteMana,
      ['U'] = OracleToken.BlueMana,
      ['B'] = OracleToken.BlackMana,
      ['R'] = OracleToken.RedMana,
      ['G'] = OracleToken.GreenMana,
      ['C'] = OracleToken.ColorlessMana,
      ['S'] = OracleToken.SnowMana,
    };

  /// <summary>
  /// Variable mana symbols.
  /// </summary>
  private static readonly HashSet<char> _variableManaSymbols = ['X', 'Y', 'Z'];

  /// <summary>
  /// Quote characters (straight and curly).
  /// </summary>
  private static readonly HashSet<char> _quoteChars = ['"', '\u201C', '\u201D'];

  /// <inheritdoc/>
  protected override IEnumerable<Result<OracleToken>> Tokenize(TextSpan span)
  {
    var next = SkipWhiteSpace(span);
    if (!next.HasValue)
    {
      yield break;
    }

    do
    {
      var ch = next.Value;

      // Newline (paragraph separator)
      if (ch == '\n')
      {
        var start = next.Location;
        next = next.Remainder.ConsumeChar();
        yield return Result.Value(OracleToken.Newline, start, next.Location);
        next = SkipWhiteSpace(next.Location);
        continue;
      }

      // Mana/special symbols: {X}
      if (ch == '{')
      {
        var symbolResult = TryTokenizeBracedSymbol(next.Location);
        if (symbolResult.HasValue)
        {
          yield return symbolResult;
          next = symbolResult.Remainder.ConsumeChar();
          next = SkipWhiteSpace(next.Location);
          continue;
        }
      }

      // Loyalty symbols: +N, −N (or -N), 0:
      if (ch == '+' || ch == '\u2212' || ch == '-')
      {
        var loyaltyResult = TryTokenizeLoyaltySymbol(next.Location);
        if (loyaltyResult.HasValue)
        {
          yield return loyaltyResult;
          next = loyaltyResult.Remainder.ConsumeChar();
          next = SkipWhiteSpace(next.Location);
          continue;
        }
      }

      // Handle reminder text (parenthesized content)
      if (ch == '(')
      {
        var reminderResult = TryTokenizeReminderText(next.Location);
        if (reminderResult.HasValue)
        {
          yield return reminderResult;
          next = reminderResult.Remainder.ConsumeChar();
          next = SkipWhiteSpace(next.Location);
          continue;
        }
      }

      // Handle quoted text
      if (_quoteChars.Contains(ch))
      {
        var quotedResult = TryTokenizeQuotedText(next.Location);
        if (quotedResult.HasValue)
        {
          yield return quotedResult;
          next = quotedResult.Remainder.ConsumeChar();
          next = SkipWhiteSpace(next.Location);
          continue;
        }
      }

      // Punctuation
      if (TryTokenizePunctuation(ch, out var punctToken))
      {
        var start = next.Location;
        next = next.Remainder.ConsumeChar();
        yield return Result.Value(punctToken, start, next.Location);
        next = SkipWhiteSpace(next.Location);
        continue;
      }

      // Numbers
      if (char.IsDigit(ch))
      {
        var numberResult = Numerics.Integer(next.Location);
        if (numberResult.HasValue)
        {
          yield return Result.Value(OracleToken.Number, next.Location, numberResult.Remainder);
          next = numberResult.Remainder.ConsumeChar();
          next = SkipWhiteSpace(next.Location);
          continue;
        }
      }

      // Words (identifiers, keywords, etc.)
      if (char.IsLetter(ch) || ch == '\'')
      {
        var wordResult = TokenizeWord(next.Location);
        yield return wordResult;
        next = wordResult.Remainder.ConsumeChar();
        next = SkipWhiteSpace(next.Location);
        continue;
      }

      // Unknown character - skip and continue
      yield return Result.Empty<OracleToken>(next.Location, [$"unexpected character '{ch}'"]);
      next = next.Remainder.ConsumeChar();
      next = SkipWhiteSpace(next.Location);
    } while (next.HasValue);
  }

  /// <summary>
  /// Attempts to tokenize a braced symbol like {W}, {2}, {W/U}, {G/P}, etc.
  /// </summary>
  private static Result<OracleToken> TryTokenizeBracedSymbol(TextSpan start)
  {
    var position = start;

    // Must start with {
    var current = position.ConsumeChar();
    if (!current.HasValue || current.Value != '{')
    {
      return Result.Empty<OracleToken>(start);
    }

    // Collect content until }
    var content = new List<char>();
    position = current.Remainder;
    current = position.ConsumeChar();

    while (current.HasValue && current.Value != '}')
    {
      content.Add(current.Value);
      position = current.Remainder;
      current = position.ConsumeChar();
    }

    if (!current.HasValue || current.Value != '}')
    {
      return Result.Empty<OracleToken>(start);
    }

    var symbolContent = new string(content.ToArray());
    var token = ClassifyManaSymbol(symbolContent);
    return Result.Value(token, start, current.Remainder);
  }

  /// <summary>
  /// Classifies the content of a braced symbol.
  /// </summary>
  private static OracleToken ClassifyManaSymbol(string content)
  {
    // Tap/Untap/Energy
    switch (content)
    {
      case "T":
        return OracleToken.TapSymbol;
      case "Q":
        return OracleToken.UntapSymbol;
      case "E":
        return OracleToken.EnergySymbol;
    }

    // Variable mana: X, Y, Z
    if (content.Length == 1 && _variableManaSymbols.Contains(content[0]))
    {
      return OracleToken.VariableMana;
    }

    // Single colored mana: W, U, B, R, G, C, S
    if (content.Length == 1 && _coloredManaSymbols.TryGetValue(content[0], out var coloredToken))
    {
      return coloredToken;
    }

    // Generic mana: numeric
    if (int.TryParse(content, out _))
    {
      return OracleToken.GenericMana;
    }

    // Hybrid Phyrexian: W/U/P, B/R/P, etc.
    if (content.Contains("/P") && content.Contains('/') && content.Length > 3)
    {
      return OracleToken.HybridPhyrexianMana;
    }

    // Phyrexian: W/P, G/P, etc.
    if (content.EndsWith("/P") && content.Length == 3)
    {
      return OracleToken.PhyrexianMana;
    }

    // Two-hybrid: 2/W, 2/U, etc.
    if (content.Length == 3 && content[0] == '2' && content[1] == '/')
    {
      return OracleToken.TwoHybridMana;
    }

    // Hybrid: W/U, B/R, etc.
    if (content.Length == 3 && content[1] == '/')
    {
      return OracleToken.HybridMana;
    }

    // Default to generic mana for unknown patterns
    return OracleToken.GenericMana;
  }

  /// <summary>
  /// Attempts to tokenize a loyalty symbol: +N, −N, -N, or 0 followed by :
  /// </summary>
  private static Result<OracleToken> TryTokenizeLoyaltySymbol(TextSpan start)
  {
    var position = start;
    var current = position.ConsumeChar();

    if (!current.HasValue)
    {
      return Result.Empty<OracleToken>(start);
    }

    var sign = current.Value;

    // Check for + or − (or ASCII -)
    if (sign == '+')
    {
      // +N pattern
      position = current.Remainder;
      var numberResult = Numerics.Integer(position);
      if (numberResult.HasValue)
      {
        // Check if followed by colon (to distinguish from other uses of +)
        var afterNumber = numberResult.Remainder.ConsumeChar();
        if (afterNumber.HasValue && afterNumber.Value == ':')
        {
          return Result.Value(OracleToken.LoyaltyUp, start, numberResult.Remainder);
        }
      }
    }
    else if (sign == '\u2212' || sign == '-')
    {
      // −N pattern (using proper minus sign or ASCII hyphen)
      position = current.Remainder;
      var numberResult = Numerics.Integer(position);
      if (numberResult.HasValue)
      {
        // Check if followed by colon
        var afterNumber = numberResult.Remainder.ConsumeChar();
        if (afterNumber.HasValue && afterNumber.Value == ':')
        {
          return Result.Value(OracleToken.LoyaltyDown, start, numberResult.Remainder);
        }
      }
    }

    return Result.Empty<OracleToken>(start);
  }

  /// <summary>
  /// Attempts to tokenize punctuation characters.
  /// </summary>
  private static bool TryTokenizePunctuation(char ch, out OracleToken token)
  {
    token = ch switch
    {
      ':' => OracleToken.Colon,
      ',' => OracleToken.Comma,
      '.' => OracleToken.Period,
      '/' => OracleToken.Slash,
      '\u2014' => OracleToken.EmDash, // Em dash
      '\u2013' => OracleToken.EmDash, // En dash (treat same)
      '\u2022' => OracleToken.Bullet, // Bullet
      ')' => OracleToken.CloseParen,
      _ => OracleToken.None,
    };
    return token != OracleToken.None;
  }

  /// <summary>
  /// Tokenizes reminder text (content within parentheses).
  /// Consumes from opening paren to closing paren inclusive.
  /// </summary>
  private static Result<OracleToken> TryTokenizeReminderText(TextSpan start)
  {
    var position = start;
    var depth = 0;
    var current = position.ConsumeChar();

    while (current.HasValue)
    {
      if (current.Value == '(')
      {
        depth++;
      }
      else if (current.Value == ')')
      {
        depth--;
        if (depth == 0)
        {
          return Result.Value(OracleToken.ReminderText, start, current.Remainder);
        }
      }
      else if (current.Value == '\n')
      {
        // Don't span across newlines
        break;
      }

      position = current.Remainder;
      current = position.ConsumeChar();
    }

    return Result.Empty<OracleToken>(start);
  }

  /// <summary>
  /// Tokenizes quoted text (content within double quotes).
  /// Handles nested quotes for abilities granted to other permanents.
  /// </summary>
  private static Result<OracleToken> TryTokenizeQuotedText(TextSpan start)
  {
    var position = start;
    var current = position.ConsumeChar();

    // Skip opening quote
    if (!current.HasValue || !_quoteChars.Contains(current.Value))
    {
      return Result.Empty<OracleToken>(start);
    }

    position = current.Remainder;
    current = position.ConsumeChar();

    // Read until closing quote
    while (current.HasValue)
    {
      if (_quoteChars.Contains(current.Value))
      {
        return Result.Value(OracleToken.QuotedText, start, current.Remainder);
      }

      if (current.Value == '\n')
      {
        // Don't span across newlines
        break;
      }

      position = current.Remainder;
      current = position.ConsumeChar();
    }

    return Result.Empty<OracleToken>(start);
  }

  /// <summary>
  /// Tokenizes a word, checking for structural keywords first.
  /// </summary>
  private static Result<OracleToken> TokenizeWord(TextSpan start)
  {
    var position = start;
    var current = position.ConsumeChar();

    // Collect word characters (letters, digits, apostrophes, hyphens)
    while (
      current.HasValue
      && (char.IsLetterOrDigit(current.Value) || current.Value == '\'' || current.Value == '-')
    )
    {
      position = current.Remainder;
      current = position.ConsumeChar();
    }

    var wordSpan = start.Until(position);
    var word = wordSpan.ToStringValue();

    // Check for structural keywords (case-insensitive)
    foreach (var (text, token) in _structuralKeywords)
    {
      if (string.Equals(word, text, StringComparison.OrdinalIgnoreCase))
      {
        return Result.Value(token, start, position);
      }
    }

    // Check for word numbers
    if (_wordNumbers.ContainsKey(word))
    {
      return Result.Value(OracleToken.WordNumber, start, position);
    }

    // Default to generic word
    return Result.Value(OracleToken.Word, start, position);
  }
}
