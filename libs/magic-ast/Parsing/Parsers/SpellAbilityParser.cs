namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
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
    var effects = TryParseEffects(clause.RawText);
    if (effects is null || effects.Count == 0)
    {
      return
      [
        _fallback.Parse(clause, classification, "Spell ability parser couldn't recognise effect"),
      ];
    }

    return
    [
      new SpellAbility { Effects = effects, AbilityWord = classification.AbilityWord },
    ];
  }

  /// <summary>
  /// Multi-effect dispatch: one spell line can carry several effects in the
  /// gold AST (e.g. Rookie Mistake's pair of <c>modifyPT</c>s under a single
  /// duration). We model that as a list returned from a single line of text;
  /// single-effect routes still return one-element lists.
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseEffects(string text)
  {
    // Effects that intrinsically split into multiple gold entries are matched
    // before the single-effect dispatch so the parser doesn't collapse them
    // into one composite by accident.
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var pair = TryParseModifyPTConjunctionEffectsList(trimmed);
    if (pair is not null)
    {
      return pair;
    }

    var single = TryParseEffect(text);
    if (single is null)
    {
      return null;
    }
    return [single];
  }

  /// <summary>
  /// Rookie-Mistake shape returning the gold's flat two-element list directly.
  /// Mirrors <see cref="TryParseModifyPTConjunctionEffect"/> but skips the
  /// composite wrapper, so SpellAbility.Effects matches the fixture's shape.
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseModifyPTConjunctionEffectsList(string text)
  {
    var m = Regex.Match(
      text,
      @"^Until\s+end\s+of\s+turn,\s*target\s+creature\s+gets\s+(?<p1>[+-]\d+)/(?<t1>[+-]\d+)\s+and\s+another\s+target\s+creature\s+gets\s+(?<p2>[+-]\d+)/(?<t2>[+-]\d+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var p1 = int.Parse(m.Groups["p1"].Value);
    var t1 = int.Parse(m.Groups["t1"].Value);
    var p2 = int.Parse(m.Groups["p2"].Value);
    var t2 = int.Parse(m.Groups["t2"].Value);

    var duration = new MagicAST.AST.Effects.UntilEndOfTurnDuration();
    return new List<Effect>
    {
      new MagicAST.AST.Effects.Modification.ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = LiteralQuantity.Of(p1),
        ToughnessModifier = LiteralQuantity.Of(t1),
        Duration = duration,
      },
      new MagicAST.AST.Effects.Modification.ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["another"],
          },
        },
        PowerModifier = LiteralQuantity.Of(p2),
        ToughnessModifier = LiteralQuantity.Of(t2),
        Duration = duration,
      },
    };
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

    effect = TryParseReturnFromGraveyardToHandEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseDestroyTargetSimpleEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseDiscardEachPlayerEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseDrawCardsSimpleEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseModifyPTConjunctionEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseReturnCommanderToHandEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    effect = TryParseCantBeCounteredEffect(trimmed);
    if (effect is not null)
    {
      return effect;
    }

    return null;
  }

  /// <summary>
  /// "This spell can't be countered." — encoded as a spell-level effect
  /// rather than a static. (See <see cref="AbilityClassifier"/> for the
  /// rationale: it's a property of the resolving spell, not of the
  /// permanent that comes off the stack.)
  /// </summary>
  private static MagicAST.AST.Effects.Keyword.CantBeCounteredEffect? TryParseCantBeCounteredEffect(
    string text
  )
  {
    if (
      !Regex.IsMatch(
        text,
        @"^This\s+spell\s+can'?t\s+be\s+countered$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return null;
    }
    return new MagicAST.AST.Effects.Keyword.CantBeCounteredEffect();
  }

  /// <summary>
  /// "Destroy target creature." — single-type destroy spell. Disambiguates
  /// from the disjunction shape above by requiring exactly one type token
  /// in the target descriptor.
  /// </summary>
  private static MagicAST.AST.Effects.ZoneChange.DestroyEffect? TryParseDestroyTargetSimpleEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Destroy\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    return new MagicAST.AST.Effects.ZoneChange.DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] },
      },
    };
  }

  /// <summary>
  /// "Each player [may] discard a card." — each-player discard fragment used
  /// in spell text (e.g. the Death of Gwen Stacy first effect on Spider-Man).
  /// </summary>
  private static MagicAST.AST.Effects.CardFlow.DiscardCardsEffect? TryParseDiscardEachPlayerEffect(string text)
  {
    var lower = text.ToLowerInvariant();
    if (!lower.StartsWith("each player"))
    {
      return null;
    }
    if (!Regex.IsMatch(lower, @"discard\s+a\s+card"))
    {
      return null;
    }
    var isOptional = lower.Contains("may discard");
    return new MagicAST.AST.Effects.CardFlow.DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
      Random = false,
      IsOptional = isOptional,
    };
  }

  /// <summary>
  /// Plain "Draw [N] card(s)." used at the spell-resolution level (sorceries
  /// like Read the Tides' modal options, or single-option spells).
  /// </summary>
  private static MagicAST.AST.Effects.CardFlow.DrawCardsEffect? TryParseDrawCardsSimpleEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Draw\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var raw = m.Groups["count"].Value.ToLowerInvariant();
    int n;
    if (raw == "a" || raw == "one")
    {
      n = 1;
    }
    else if (int.TryParse(raw, out var asDigit))
    {
      n = asDigit;
    }
    else
    {
      n = raw switch
      {
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ => 1,
      };
    }
    return new MagicAST.AST.Effects.CardFlow.DrawCardsEffect
    {
      Count = LiteralQuantity.Of(n),
      Player = ObjectReference.You(),
    };
  }

  /// <summary>
  /// Rookie-Mistake shape:
  /// "Until end of turn, target creature gets +0/+2 and another target creature gets -2/-0."
  /// Builds a single composite effect with two <see cref="ModifyPTEffect"/>s,
  /// each sharing the "until end of turn" <see cref="Duration"/>.
  /// </summary>
  private static MagicAST.AST.Effects.Core.CompositeEffect? TryParseModifyPTConjunctionEffect(string text)
  {
    var m = Regex.Match(
      text,
      @"^Until\s+end\s+of\s+turn,\s*target\s+creature\s+gets\s+(?<p1>[+-]\d+)/(?<t1>[+-]\d+)\s+and\s+another\s+target\s+creature\s+gets\s+(?<p2>[+-]\d+)/(?<t2>[+-]\d+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var p1 = int.Parse(m.Groups["p1"].Value);
    var t1 = int.Parse(m.Groups["t1"].Value);
    var p2 = int.Parse(m.Groups["p2"].Value);
    var t2 = int.Parse(m.Groups["t2"].Value);

    var duration = new MagicAST.AST.Effects.UntilEndOfTurnDuration();
    var effects = new List<Effect>
    {
      new MagicAST.AST.Effects.Modification.ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = LiteralQuantity.Of(p1),
        ToughnessModifier = LiteralQuantity.Of(t1),
        Duration = duration,
      },
      new MagicAST.AST.Effects.Modification.ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["another"],
          },
        },
        PowerModifier = LiteralQuantity.Of(p2),
        ToughnessModifier = LiteralQuantity.Of(t2),
        Duration = duration,
      },
    };

    // The fixture stores each modifyPT as a top-level entry in Effects[] —
    // wrap them in a composite so SpellAbility.Effects keeps its single-entry
    // shape but the gold's flat list comes through after expansion.
    return new MagicAST.AST.Effects.Core.CompositeEffect { Effects = effects };
  }

  /// <summary>
  /// "Put your commander into your hand from the command zone." — Road of Return's
  /// commander-recall option. Encodes the destination zone implicitly via the
  /// <see cref="ReturnToHandEffect"/> kind and the source zone explicitly on the
  /// filter (<see cref="Zone.CommandZone"/>). The target uses
  /// <see cref="ObjectReferenceKind.Designated"/> because oracle text refers to
  /// "your commander" by designation, not by a creature filter.
  /// </summary>
  private static MagicAST.AST.Effects.ZoneChange.ReturnToHandEffect? TryParseReturnCommanderToHandEffect(
    string text
  )
  {
    if (
      !Regex.IsMatch(
        text,
        @"^Put\s+your\s+commander\s+into\s+your\s+hand\s+from\s+the\s+command\s+zone$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return null;
    }
    return new MagicAST.AST.Effects.ZoneChange.ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          Characteristics = ["your commander"],
          Zone = Zone.CommandZone,
        },
      },
    };
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

    var (colors, isColorless) = MapColorWord(colorWord);

    return new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors,
      IsColorless = isColorless,
    };
  }

  /// <summary>
  /// Maps an oracle-text color word to either a colored-list or the
  /// <see cref="ObjectFilter.IsColorless"/> flag. Colorlessness is the
  /// absence of all colors (Rule 105.1: "Colorless is not a color"), so
  /// it lands on its own axis rather than as a value in <c>Colors</c>.
  /// Returns (null, null) when no color word is present.
  /// </summary>
  private static (IReadOnlyList<string>?, bool?) MapColorWord(string? colorWord)
  {
    if (string.IsNullOrWhiteSpace(colorWord))
    {
      return (null, null);
    }

    return colorWord.ToLowerInvariant() switch
    {
      "white" => (new[] { "W" }, null),
      "blue" => (new[] { "U" }, null),
      "black" => (new[] { "B" }, null),
      "red" => (new[] { "R" }, null),
      "green" => (new[] { "G" }, null),
      "colorless" => (null, true),
      _ => (null, null),
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
