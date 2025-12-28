namespace MagicAST.Parsing;

using MagicAST.AST.Abilities;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Classification result for an oracle clause.
/// </summary>
public sealed record ClauseClassification
{
  /// <summary>
  /// The classified ability kind.
  /// </summary>
  public required AbilityKind Kind { get; init; }

  /// <summary>
  /// Confidence level of the classification (0.0 to 1.0).
  /// Higher values indicate stronger pattern matches.
  /// </summary>
  public required double Confidence { get; init; }

  /// <summary>
  /// Optional ability word detected (e.g., "Landfall", "Revolt").
  /// </summary>
  public string? AbilityWord { get; init; }

  /// <summary>
  /// For loyalty abilities, the loyalty cost (+N, -N, or 0).
  /// </summary>
  public int? LoyaltyCost { get; init; }
}

/// <summary>
/// Classifies oracle text clauses into ability types based on structural patterns.
/// This pre-classification enables routing to specialized parsers.
/// </summary>
public sealed class AbilityClassifier
{
  /// <summary>
  /// Known ability words that precede em-dashes.
  /// These have no rules meaning but help identify ability patterns.
  /// </summary>
  private static readonly HashSet<string> _abilityWords =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Triggered ability words
      "Landfall",
      "Revolt",
      "Enrage",
      "Delirium",
      "Threshold",
      "Hellbent",
      "Metalcraft",
      "Morbid",
      "Raid",
      "Spell mastery",
      "Ferocious",
      "Formidable",
      "Undergrowth",
      "Addendum",
      "Spectacle",
      "Constellation",
      "Heroic",
      "Inspired",
      "Battalion",
      "Bloodrush",
      "Evolve",
      "Extort",
      "Overload",
      "Populate",
      "Scavenge",
      "Detain",
      "Unleash",
      "Radiance",
      // Static/conditional ability words
      "Kinship",
      "Domain",
      "Converge",
      "Sweep",
      "Grandeur",
      "Channel",
      "Bloodthirst",
      "Imprint",
      "Join forces",
      "Tempting offer",
      "Will of the council",
      "Council's dilemma",
      "Parley",
      "Lieutenant",
      "Eminence",
      "Alliance",
      "Coven",
      "Pack tactics",
      "Magecraft",
      // Adventure
      "Adventure",
    };

  /// <summary>
  /// Classifies a clause into an ability kind.
  /// </summary>
  /// <param name="clause">The clause to classify.</param>
  /// <returns>The classification result.</returns>
  public ClauseClassification Classify(OracleClause clause)
  {
    var tokens = clause.Tokens.ToList();

    // Empty clause
    if (tokens.Count == 0)
    {
      return new ClauseClassification { Kind = AbilityKind.Unparsed, Confidence = 1.0 };
    }

    // Check for ability word pattern: "Word —"
    var abilityWord = TryExtractAbilityWord(clause);

    // Check for loyalty ability: +N: or −N: or 0:
    var loyaltyClassification = TryClassifyAsLoyalty(tokens);
    if (loyaltyClassification != null)
    {
      return loyaltyClassification with { AbilityWord = abilityWord };
    }

    // Check for triggered ability: When/Whenever/At
    if (StartsWithTriggerTiming(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Triggered,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // Check for activated ability: {cost}: or word:
    if (IsActivatedAbilityPattern(tokens, clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // Check for replacement effect: would ... instead
    if (ContainsReplacementPattern(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static, // Replacement effects are a type of static ability
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // Check for modal ability: Choose
    if (StartsWithChoose(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Modal,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // Check for single keyword (may need expansion)
    if (IsSingleKeyword(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static, // Keywords are typically static abilities
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // Default to static ability for declarative statements
    return new ClauseClassification
    {
      Kind = AbilityKind.Static,
      Confidence = 0.50,
      AbilityWord = abilityWord,
    };
  }

  /// <summary>
  /// Tries to extract an ability word from the clause.
  /// Ability words appear before an em-dash.
  /// </summary>
  private static string? TryExtractAbilityWord(OracleClause clause)
  {
    var emDashIndex = clause.RawText.IndexOf('\u2014');
    if (emDashIndex <= 0)
    {
      return null;
    }

    var prefix = clause.RawText[..emDashIndex].Trim();
    if (_abilityWords.Contains(prefix))
    {
      return prefix;
    }

    return null;
  }

  /// <summary>
  /// Tries to classify a clause as a loyalty ability.
  /// </summary>
  private static ClauseClassification? TryClassifyAsLoyalty(List<Token<OracleToken>> tokens)
  {
    if (tokens.Count < 2)
    {
      return null;
    }

    var first = tokens[0];

    // +N: pattern
    if (first.Kind == OracleToken.LoyaltyUp)
    {
      var loyaltyCost = ParseLoyaltyCost(first.ToStringValue(), positive: true);
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.98,
        LoyaltyCost = loyaltyCost,
      };
    }

    // −N: pattern
    if (first.Kind == OracleToken.LoyaltyDown)
    {
      var loyaltyCost = ParseLoyaltyCost(first.ToStringValue(), positive: false);
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.98,
        LoyaltyCost = loyaltyCost,
      };
    }

    // 0: pattern (number 0 followed by colon)
    if (first.Kind == OracleToken.Number && first.ToStringValue() == "0")
    {
      if (tokens.Count > 1 && tokens[1].Kind == OracleToken.Colon)
      {
        return new ClauseClassification
        {
          Kind = AbilityKind.Activated,
          Confidence = 0.98,
          LoyaltyCost = 0,
        };
      }
    }

    return null;
  }

  /// <summary>
  /// Parses a loyalty cost from the token value.
  /// </summary>
  private static int? ParseLoyaltyCost(string value, bool positive)
  {
    // Remove + or − prefix
    var numStr = value.TrimStart('+', '\u2212', '-');
    if (int.TryParse(numStr, out var cost))
    {
      return positive ? cost : -cost;
    }

    return null;
  }

  /// <summary>
  /// Checks if the clause starts with a trigger timing word.
  /// </summary>
  private static bool StartsWithTriggerTiming(List<Token<OracleToken>> tokens)
  {
    if (tokens.Count == 0)
    {
      return false;
    }

    var first = tokens[0].Kind;
    return first == OracleToken.When || first == OracleToken.Whenever || first == OracleToken.At;
  }

  /// <summary>
  /// Checks if the clause matches an activated ability pattern.
  /// Pattern: {cost}: or word: (but not "When:" etc.)
  /// </summary>
  private static bool IsActivatedAbilityPattern(List<Token<OracleToken>> tokens, string rawText)
  {
    // Look for colon in the raw text
    var colonIndex = rawText.IndexOf(':');
    if (colonIndex < 0)
    {
      return false;
    }

    // Check for mana/tap symbols before colon
    var hasCostToken = false;
    for (var i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];

      if (token.Kind == OracleToken.Colon)
      {
        // Found colon - check if there were any cost-like tokens before it
        // or if this is an activation keyword pattern
        if (i > 0)
        {
          return hasCostToken || IsActivationKeyword(tokens, i);
        }

        return false;
      }

      // Track if we've seen any cost tokens
      if (IsCostToken(token.Kind))
      {
        hasCostToken = true;
      }

      // If we hit a trigger timing word, this isn't an activated ability
      if (
        token.Kind == OracleToken.When
        || token.Kind == OracleToken.Whenever
        || token.Kind == OracleToken.At
      )
      {
        return false;
      }
    }

    return false;
  }

  /// <summary>
  /// Checks if a token kind represents a cost component.
  /// </summary>
  private static bool IsCostToken(OracleToken kind)
  {
    return kind == OracleToken.TapSymbol
      || kind == OracleToken.UntapSymbol
      || kind == OracleToken.GenericMana
      || kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana
      || kind == OracleToken.ColorlessMana
      || kind == OracleToken.HybridMana
      || kind == OracleToken.PhyrexianMana
      || kind == OracleToken.VariableMana
      || kind == OracleToken.EnergySymbol;
  }

  /// <summary>
  /// Checks if the tokens before the colon represent an activation keyword
  /// (e.g., "Cycling", "Equip", "Crew").
  /// </summary>
  private static bool IsActivationKeyword(List<Token<OracleToken>> tokens, int colonIndex)
  {
    // Common activation keywords that appear before colon without mana symbols
    var activationKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Equip",
      "Cycling",
      "Crew",
      "Fortify",
      "Reconfigure",
      "Ninjutsu",
      "Outlast",
      "Megamorph",
      "Morph",
      "Unearth",
      "Suspend",
      "Transfigure",
      "Transmute",
      "Boast",
      "Channel",
    };

    // Look for word token immediately before colon
    if (colonIndex > 0 && tokens[colonIndex - 1].Kind == OracleToken.Word)
    {
      var word = tokens[colonIndex - 1].ToStringValue();
      return activationKeywords.Contains(word);
    }

    return false;
  }

  /// <summary>
  /// Checks if the clause contains a replacement effect pattern.
  /// </summary>
  private static bool ContainsReplacementPattern(List<Token<OracleToken>> tokens)
  {
    var hasWould = false;
    var hasInstead = false;

    foreach (var token in tokens)
    {
      if (token.Kind == OracleToken.Would)
      {
        hasWould = true;
      }
      else if (token.Kind == OracleToken.Instead)
      {
        hasInstead = true;
      }
    }

    return hasWould && hasInstead;
  }

  /// <summary>
  /// Checks if the clause starts with "Choose".
  /// </summary>
  private static bool StartsWithChoose(List<Token<OracleToken>> tokens)
  {
    return tokens.Count > 0 && tokens[0].Kind == OracleToken.Choose;
  }

  /// <summary>
  /// Checks if the clause is a single keyword ability.
  /// </summary>
  private static bool IsSingleKeyword(List<Token<OracleToken>> tokens)
  {
    // Filter out reminder text and punctuation
    var significantTokens = 0;
    foreach (var token in tokens)
    {
      if (
        token.Kind == OracleToken.Word
        || token.Kind == OracleToken.Number
        || token.Kind == OracleToken.GenericMana
        || IsColoredManaToken(token.Kind)
      )
      {
        significantTokens++;
      }
      else if (
        token.Kind != OracleToken.ReminderText
        && token.Kind != OracleToken.Period
        && token.Kind != OracleToken.Comma
      )
      {
        // Has other structural tokens, not a simple keyword
        return false;
      }
    }

    // Single word (possibly with number parameter like "Absorb 2")
    return significantTokens <= 2;
  }

  /// <summary>
  /// Checks if a token is a colored mana token.
  /// </summary>
  private static bool IsColoredManaToken(OracleToken kind)
  {
    return kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana;
  }
}
