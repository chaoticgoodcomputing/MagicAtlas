namespace MagicAST.Parsing.Parsers.Activated;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Shared utilities used across <see cref="IActivatedEffectRule"/> and
/// <see cref="IActivatedCostRule"/> implementations.
/// </summary>
internal static class ActivatedRuleHelpers
{
  /// <summary>
  /// Parses number words like "one", "two", "three" into integers.
  /// Returns null if no number word is found.
  /// </summary>
  public static int? ParseNumberWord(string text)
  {
    var lower = text.ToLowerInvariant();

    if (lower.Contains("two"))
      return 2;
    if (lower.Contains("three"))
      return 3;
    if (lower.Contains("four"))
      return 4;
    if (lower.Contains("five"))
      return 5;
    if (lower.Contains("six"))
      return 6;
    if (lower.Contains("seven"))
      return 7;
    if (lower.Contains("eight"))
      return 8;
    if (lower.Contains("nine"))
      return 9;
    if (lower.Contains("ten"))
      return 10;
    if (lower.Contains("one") || lower.Contains(" a ") || lower.Contains("an "))
      return 1;

    // Try to find a digit
    var digitMatch = Regex.Match(text, @"\b(\d+)\b");
    if (digitMatch.Success)
    {
      return int.Parse(digitMatch.Groups[1].Value);
    }

    return null;
  }

  /// <summary>
  /// Parses "sacrifice [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  public static (Quantity quantity, ObjectFilter? filter) ParseSacrificePattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    Quantity quantity;
    if (lower.Contains(" x "))
    {
      quantity = VariableQuantity.X;
    }
    else
    {
      var count = ParseNumberWord(text) ?? 1;
      quantity = LiteralQuantity.Of(count);
    }

    // Parse filter
    ObjectFilter? filter = null;
    if (lower.Contains("another creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"], Characteristics = [Characteristic.Other("another")] };
    }
    else if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"], Characteristics = [Characteristic.Other("this permanent")] };
    }
    else if (lower.Contains("this artifact"))
    {
      filter = new ObjectFilter { CardTypes = ["artifact"], Characteristics = [Characteristic.Other("this permanent")] };
    }
    else if (lower.Contains("this enchantment"))
    {
      filter = new ObjectFilter { CardTypes = ["enchantment"], Characteristics = [Characteristic.Other("this permanent")] };
    }
    else if (lower.Contains("this land"))
    {
      filter = new ObjectFilter { CardTypes = ["land"], Characteristics = [Characteristic.Other("this permanent")] };
    }
    else if (lower.Contains(" land") || lower.EndsWith("land"))
    {
      // "Sacrifice a land" / "Sacrifice another land" — land is a card type (Rule 205.3a),
      // not a subtype. Must precede the creature/artifact branches to avoid misrouting via
      // the generic fallback regex which would emit Subtypes: ["land"] instead.
      filter = new ObjectFilter { CardTypes = ["land"] };
    }
    else if (Regex.IsMatch(lower, @"\btoken\b") && !lower.Contains("creature") && !lower.Contains("artifact"))
    {
      // "Sacrifice a token" — "token" is a characteristic predicate (Rule 111.7),
      // not a card type or subtype. Encodes as Characteristics: ["token"] to
      // match the gold convention and distinguish from typed-token costs like
      // "Sacrifice a creature token".
      filter = new ObjectFilter { Characteristics = [Characteristic.Other("token")] };
    }
    else if (lower.Contains("creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }
    else if (lower.Contains("artifact"))
    {
      filter = new ObjectFilter { CardTypes = ["artifact"] };
    }
    else
    {
      // Try to extract the type from the text.
      //
      // Shape 1: "Sacrifice [count-word] [Subtype]s" — e.g. "Sacrifice three Treasures".
      // The count word was already parsed into `quantity` above; here we just need
      // the subtype. We look for an explicit count-word + capitalized-subtype pair
      // BEFORE the generic article regex so "three" doesn't get captured as the type.
      var countSubtypeMatch = Regex.Match(
        text,
        @"(?:Sacrifice|sacrifice)\s+(?:one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[A-Z]\w+?)s?$",
        RegexOptions.None
      );
      if (countSubtypeMatch.Success)
      {
        var typeRaw = countSubtypeMatch.Groups["type"].Value;
        filter = new ObjectFilter { Subtypes = [typeRaw] };
      }
      else
      {
        // Shape 2: "Sacrifice [article] [type]"
        // Capture the optional article/count so we can distinguish self-reference
        // from creature-subtype: "Sacrifice Denethor" (no article → self) vs.
        // "Sacrifice a Saproling" (article "a" → creature subtype, Rule 205.3m).
        var match = Regex.Match(
          text,
          @"(?:Sacrifice|sacrifice) (?<article>a |an |X )?(?<type>\w+)",
          RegexOptions.IgnoreCase
        );
        if (match.Success)
        {
          var typeRaw = match.Groups["type"].Value;
          var type = typeRaw.ToLowerInvariant();
          var hasArticle = match.Groups["article"].Success && match.Groups["article"].Value.Trim() is "a" or "an";
          var wasPlural = type.EndsWith("s") && type != "this";
          // Handle plurals (e.g., "Squirrels" -> "Squirrel")
          if (wasPlural)
          {
            type = type[..^1];
          }
          // Capitalized SINGULAR without an article (e.g., "Sacrifice Denethor") —
          // the card refers to itself by name. Encode as a "this permanent"
          // self-reference on Characteristics rather than a literal Subtypes
          // entry, matching the gold convention for self-by-name cost references.
          //
          // Capitalized PLURAL (e.g., "Sacrifice X Squirrels") or capitalized with
          // an article (e.g., "Sacrifice a Saproling") is a creature subtype, not
          // a self-reference — oracle text capitalizes creature subtypes (Rule 205.3m).
          // Singularize and emit on Subtypes. Without this distinction the plural-subtype
          // case and the article-preceded case collapse onto the "this permanent"
          // self-ref shape.
          if (char.IsUpper(typeRaw[0]) && !wasPlural && !hasArticle)
          {
            filter = new ObjectFilter { Characteristics = [Characteristic.Other("this permanent")] };
          }
          else
          {
            // Title-case the subtype to match oracle-text capitalisation
            // convention. Subtype names are proper-noun-ish (Squirrel, Goblin,
            // Treasure, etc.) and the existing CreateTokenEffect emitters
            // already title-case their subtype output.
            if (char.IsUpper(typeRaw[0]) && type.Length > 0)
            {
              type = char.ToUpperInvariant(type[0]) + type[1..];
            }
            filter = new ObjectFilter { Subtypes = [type] };
          }
        }
      }
    }

    return (quantity, filter);
  }

  /// <summary>
  /// Parses "discard [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  public static (Quantity quantity, ObjectFilter filter) ParseDiscardPattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    var count = ParseNumberWord(text) ?? 1;
    var quantity = LiteralQuantity.Of(count);

    // Parse filter
    ObjectFilter filter;
    if (lower.Contains("legendary card"))
    {
      filter = new ObjectFilter { Supertypes = ["Legendary"], CardTypes = ["card"] };
    }
    else
    {
      filter = new ObjectFilter { CardTypes = ["card"] };
    }

    return (quantity, filter);
  }

  /// <summary>
  /// Maps a signed modifier token (e.g. "+1", "-2", "+X", "-X") onto a
  /// <see cref="Quantity"/>. Literals fold the sign into the
  /// <see cref="LiteralQuantity.Value"/>; the variable case keeps the variable
  /// name and (for "-X") wraps it in a <see cref="CalculatedQuantity"/> with
  /// <c>Operation = "negate"</c>.
  /// </summary>
  public static Quantity ParseSignedModifier(string token)
  {
    var sign = token[0]; // '+' or '-'
    var rest = token[1..];
    if (string.Equals(rest, "X", StringComparison.OrdinalIgnoreCase)
      || string.Equals(rest, "Y", StringComparison.OrdinalIgnoreCase)
      || string.Equals(rest, "Z", StringComparison.OrdinalIgnoreCase))
    {
      var variable = new VariableQuantity { Name = rest.ToUpperInvariant() };
      if (sign == '+')
      {
        return variable;
      }
      return new CalculatedQuantity
      {
        Expression = $"-{rest.ToUpperInvariant()}",
        BaseQuantity = variable,
        Operation = "negate",
      };
    }

    var magnitude = int.Parse(rest);
    return LiteralQuantity.Of(sign == '-' ? -magnitude : magnitude);
  }

  /// <summary>
  /// Wraps a granted-keyword name into a structured <see cref="StaticAbility"/>
  /// carrying the keyword's effect node. Returns null when the keyword has no
  /// modeled effect yet — caller treats that as a parser miss.
  /// </summary>
  public static Ability? BuildGrantedKeywordAbility(string keywordRaw)
  {
    var keyword = keywordRaw.Trim().ToLowerInvariant();

    // Multi-word keywords (first strike, double strike) use CombatDamageTimingEffect.
    // Single-word keywords use their dedicated effect type.
    return keyword switch
    {
      "lifelink" => new StaticAbility { KeywordSource = "Lifelink", Effects = [new LifelinkEffect()] },
      "haste" => new StaticAbility { KeywordSource = "Haste", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }] },
      "trample" => new StaticAbility { KeywordSource = "Trample", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }] },
      "vigilance" => new StaticAbility { KeywordSource = "Vigilance", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }] },
      "reach" => new StaticAbility { KeywordSource = "Reach", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }] },
      "indestructible" => new StaticAbility { KeywordSource = "Indestructible", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }] },
      "deathtouch" => new StaticAbility { KeywordSource = "Deathtouch", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }] },
      "hexproof" => new StaticAbility { KeywordSource = "Hexproof", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }] },
      "shroud" => new StaticAbility { KeywordSource = "Shroud", Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }] },
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
            },
          },
        ],
      },
      "menace" => new StaticAbility
      {
        KeywordSource = "Menace",
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
            MinimumBlockers = 2,
          },
        ],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      _ => null,
    };
  }
}
