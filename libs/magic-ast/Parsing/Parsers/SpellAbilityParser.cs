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
    return TryParseReturnFromGraveyardToHandEffect(trimmed);
  }

  /// <summary>
  /// "Counter target spell." / "Counter target sorcery spell." /
  /// "Counter target instant spell."
  /// </summary>
  private static CounterSpellEffect? TryParseCounterSpellEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Counter\s+target\s+(?<filter>(instant|sorcery|creature|noncreature)?\s*spell(\s+with\s+converted\s+mana\s+cost.*)?)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var filter = BuildSpellFilter(m.Groups["filter"].Value);
    return new CounterSpellEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
    };
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> for "target X spell" where X is an
  /// optional card type qualifier (instant / sorcery / creature / noncreature).
  /// </summary>
  private static ObjectFilter BuildSpellFilter(string filterText)
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

    return new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
    };
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
