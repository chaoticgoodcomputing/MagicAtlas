namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Parses spell abilities — the resolution-time instructions of an instant or
/// sorcery (Rule 113.3a). Classified into <see cref="AbilityKind.Spell"/> by
/// <see cref="AbilityClassifier"/> and dispatched through
/// <see cref="AbilityParserRegistry"/>.
/// </summary>
/// <remarks>
/// Mirrors the shape of <see cref="ActivatedAbilityParser"/>: a chain of
/// <c>TryParse*Effect</c> rules. Each rule recognises a single oracle-text
/// shape and constructs the corresponding <see cref="Effect"/> AST node.
/// Falls through to <see cref="FallbackParser"/> when no rule matches.
/// </remarks>
[OracleAbilityParser(AbilityKind.Spell)]
public sealed class SpellAbilityParser : IAbilityParser
{
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var effect = TryParseEffect(clause.RawText);
    if (effect is null)
    {
      return
      [
        _fallback.Parse(clause, classification, "Spell ability parser couldn't recognise effect"),
      ];
    }

    return
    [
      new SpellAbility { Effects = [effect], AbilityWord = classification.AbilityWord },
    ];
  }

  /// <summary>
  /// Routes through the recognised effect shapes in priority order.
  /// </summary>
  private static Effect? TryParseEffect(string text)
  {
    var trimmed = text.Trim().TrimEnd('.').Trim();

    Effect? effect = TryParseCounterSpellEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseDestroyTargetTypeDisjunctionEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    return TryParseReturnFromGraveyardToHandEffect(trimmed);
  }

  /// <summary>
  /// "Counter target spell." / "Counter target sorcery spell." /
  /// "Counter target instant spell." / "Counter target colorless spell." /
  /// "Counter target [color] spell." (Rule 701.6, with the targeted spell's
  /// color restriction modeled as a structural <see cref="ObjectFilter.Colors"/>
  /// filter on the target reference.)
  /// </summary>
  private static CounterSpellEffect? TryParseCounterSpellEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Counter\s+target\s+(?<filter>(?<color>colorless|white|blue|black|red|green)?\s*(?<type>instant|sorcery|creature|noncreature)?\s*spell(\s+with\s+converted\s+mana\s+cost.*)?)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var filter = BuildSpellFilter(
      m.Groups["filter"].Value,
      m.Groups["color"].Success ? m.Groups["color"].Value : null
    );
    return new CounterSpellEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
    };
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> for "target [color] [card-type] spell"
  /// where each qualifier is optional. The card-type qualifier (instant /
  /// sorcery / creature / noncreature) lands on <see cref="ObjectFilter.Characteristics"/>;
  /// the color qualifier lands on <see cref="ObjectFilter.Colors"/> as the
  /// canonical letter code (W/U/B/R/G/C) so color filtering remains a
  /// structured axis rather than free-text.
  /// </summary>
  private static ObjectFilter BuildSpellFilter(string filterText, string? colorWord)
  {
    var characteristics = new List<string>();
    var cardTypes = new List<string> { "spell" };
    foreach (var word in new[] { "instant", "sorcery", "creature", "noncreature" })
    {
      if (
        filterText.Contains(word, StringComparison.OrdinalIgnoreCase)
        && !filterText.Contains("non" + word, StringComparison.OrdinalIgnoreCase)
      )
      {
        characteristics.Add(word);
      }
    }
    if (filterText.Contains("noncreature", StringComparison.OrdinalIgnoreCase))
    {
      characteristics.Add("noncreature");
    }

    var colors = MapColorWord(colorWord);

    return new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors,
    };
  }

  /// <summary>
  /// Maps an oracle-text color word (e.g. "colorless", "blue") to the
  /// canonical single-letter color list used on <see cref="ObjectFilter.Colors"/>.
  /// Returns null when no color word is present so the filter doesn't
  /// over-specify.
  /// </summary>
  private static IReadOnlyList<string>? MapColorWord(string? colorWord)
  {
    if (string.IsNullOrWhiteSpace(colorWord))
    {
      return null;
    }

    return colorWord.ToLowerInvariant() switch
    {
      "white" => ["W"],
      "blue" => ["U"],
      "black" => ["B"],
      "red" => ["R"],
      "green" => ["G"],
      "colorless" => ["C"],
      _ => null,
    };
  }

  /// <summary>
  /// "Destroy target [type1] or [type2]." / "Destroy target [type1], [type2], or [type3]."
  /// Emits a <see cref="DestroyEffect"/> whose target carries an
  /// <see cref="ObjectFilter"/> with <see cref="ObjectFilter.CardTypes"/> set to the
  /// union of the listed card types (existing convention: a multi-element
  /// <c>CardTypes</c> list is interpreted as a disjunction — see e.g. the destroy
  /// rule on Demolish, "Destroy target artifact or land").
  /// </summary>
  /// <remarks>
  /// Single-type destroy ("Destroy target creature") is intentionally not handled
  /// here. Plain destroy is covered by other shapes (saga chapters, triggered
  /// abilities) and flipping its parser status would cascade across fixtures that
  /// currently expect <c>unparsed</c>.
  /// </remarks>
  private static DestroyEffect? TryParseDestroyTargetTypeDisjunctionEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Destroy\s+target\s+(?<types>[a-z]+(?:\s*,\s*[a-z]+)*\s+or\s+[a-z]+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var cardTypes = SplitTypeDisjunction(m.Groups["types"].Value);
    if (cardTypes.Count < 2)
    {
      return null;
    }

    return new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
  }

  /// <summary>
  /// Splits a "[type1], [type2], or [typeN]" or "[type1] or [type2]" phrase into
  /// the underlying card-type tokens, lowercased, in source order, with duplicates
  /// removed (preserving first occurrence).
  /// </summary>
  private static List<string> SplitTypeDisjunction(string phrase)
  {
    // Normalise "X, Y, or Z" / "X or Y" into a flat list.
    var withoutOr = Regex.Replace(
      phrase,
      @"\s*,?\s+or\s+",
      ",",
      RegexOptions.IgnoreCase
    );
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var result = new List<string>();
    foreach (var raw in withoutOr.Split(','))
    {
      var token = raw.Trim().ToLowerInvariant();
      if (token.Length == 0)
      {
        continue;
      }
      if (seen.Add(token))
      {
        result.Add(token);
      }
    }
    return result;
  }

  /// <summary>
  /// "Return target [filter] from your graveyard to your hand."
  /// </summary>
  /// <remarks>
  /// Encodes the graveyard zone on the <see cref="ObjectFilter.Zone"/> field so the
  /// canonical <see cref="ReturnToHandEffect"/> shape doesn't need a separate
  /// source-zone slot.
  /// </remarks>
  private static ReturnToHandEffect? TryParseReturnFromGraveyardToHandEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Return\s+target\s+(?<filter>permanent|creature|artifact|enchantment|land|card|nonland\s+permanent)\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var filterText = m.Groups["filter"].Value.ToLowerInvariant();
    var cardTypes = filterText switch
    {
      "permanent" or "nonland permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      "card" => new List<string> { "card" },
      _ => new List<string> { "card" },
    };
    var characteristics =
      filterText == "nonland permanent" ? new List<string> { "nonland" } : null;

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Characteristics = characteristics,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
  }
}
