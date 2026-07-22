namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 992)]
public sealed class EnchantRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Strip trailing reminder text so "Enchant creature (Target a creature as you
    // cast this. This card enters attached to that creature.)" reduces to
    // "Enchant creature" before the descriptor match.
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var match = Regex.Match(
      rawText,
      @"^\s*Enchant\s+(?<descriptor>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var descriptor = match.Groups["descriptor"].Value.Trim();
    if (descriptor.Length == 0)
    {
      return null;
    }

    var filter = BuildEnchantFilter(descriptor);
    if (filter is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        KeywordSource = KeywordAbility.Enchant,
        Effects = [new MagicAST.AST.Effects.Combat.EnchantRestrictionEffect
        {
          LegalTargets = filter,
        }],
      },
    ];
  }

  private static ObjectFilter? BuildEnchantFilter(string descriptor)
  {
    // Strip leading "a "/"an " articles that appear in some printings.
    // `descriptor` is the original-case phrase from the oracle text (not lowercased);
    // we lowercase only for comparisons so OtherCharacteristic can preserve oracle casing.
    var d = Regex.Replace(descriptor, @"^(?:a|an)\s+", "", RegexOptions.IgnoreCase).Trim();

    ControllerFilter? controller = null;
    if (d.EndsWith(" you control", StringComparison.OrdinalIgnoreCase))
    {
      controller = ControllerFilter.You;
      d = d[..^" you control".Length].Trim();
    }
    else if (d.EndsWith(" an opponent controls", StringComparison.OrdinalIgnoreCase))
    {
      controller = ControllerFilter.Opponent;
      d = d[..^" an opponent controls".Length].Trim();
    }

    // "Enchant player" (CR 702.5) — a player is not an object/card type
    // (CR 109 vs CR 102), so the legal-target descriptor lands on the
    // EntityType axis rather than CardTypes.
    if (d.Equals("player", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter { EntityType = "player", Controller = controller };
    }

    // Simple-noun shape: "creature", "land", "permanent", "artifact", "enchantment".
    var simpleTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "creature", "land", "permanent", "artifact", "enchantment", "planeswalker",
    };
    if (simpleTypes.Contains(d))
    {
      return new ObjectFilter { CardTypes = [d.ToLowerInvariant()], Controller = controller };
    }

    // Color-exclusion shape: "non<color> <type>" (e.g. "nonblack creature", Armor of
    // Thorns) — CR 105.2c: the "non" prefix negates a color characteristic. Routed
    // through the shared QualifierAxisMapper so the "non<color>" → ExcludedColors
    // handling stays consistent with how the same negation is folded elsewhere
    // (e.g. DestroyTargetNonblackCreatureEffectRule's ExcludedColors=["B"]).
    var nonColorMatch = Regex.Match(
      d,
      @"^non(?<color>white|blue|black|red|green)\s+(?<type>creature|land|permanent|artifact|enchantment|planeswalker)$",
      RegexOptions.IgnoreCase
    );
    if (nonColorMatch.Success)
    {
      var typeName = nonColorMatch.Groups["type"].Value.ToLowerInvariant();
      var qualifier = "non" + nonColorMatch.Groups["color"].Value.ToLowerInvariant();
      var baseFilter = new ObjectFilter { CardTypes = [typeName], Controller = controller };
      return QualifierAxisMapper.Apply(baseFilter, [qualifier]);
    }

    // Card-type-exclusion shape: "non<type> <type>" (e.g. "nonland permanent",
    // Detention Vortex) — CR 702.5a: the enchant ability restricts what the Aura
    // can enchant; "non<type>" negates a card-TYPE characteristic (CR 109 vs the
    // color negation above). Routed to ExcludedCardTypes, parallel to the "a
    // nonland card" -> CardTypes=["card"] + ExcludedCardTypes=["land"] convention
    // used throughout the codebase (see ObjectFilter.ExcludedCardTypes doc).
    var nonTypeMatch = Regex.Match(
      d,
      @"^non(?<excludedType>creature|land|artifact|enchantment|planeswalker)\s+(?<type>creature|land|permanent|artifact|enchantment|planeswalker)$",
      RegexOptions.IgnoreCase
    );
    if (nonTypeMatch.Success)
    {
      var excludedType = nonTypeMatch.Groups["excludedType"].Value.ToLowerInvariant();
      var typeName = nonTypeMatch.Groups["type"].Value.ToLowerInvariant();
      return new ObjectFilter
      {
        CardTypes = [typeName],
        ExcludedCardTypes = [excludedType],
        Controller = controller,
      };
    }

    // Disjunctive type shape: "typeA or typeB" (e.g. "artifact or creature").
    // Both halves must be recognised simple card types for the structured encoding.
    var orMatch = Regex.Match(d, @"^(?<a>[A-Za-z]+)\s+or\s+(?<b>[A-Za-z]+)$", RegexOptions.IgnoreCase);
    if (orMatch.Success)
    {
      var typeA = orMatch.Groups["a"].Value;
      var typeB = orMatch.Groups["b"].Value;
      if (simpleTypes.Contains(typeA) && simpleTypes.Contains(typeB))
      {
        return new ObjectFilter
        {
          CardTypes = [typeA.ToLowerInvariant(), typeB.ToLowerInvariant()],
          Controller = controller,
        };
      }

      // Mixed card-type / subtype disjunction: "creature or Vehicle", "creature or Equipment",
      // etc. — one half is a recognised card-type noun, the other is a subtype (not a simple
      // card-type noun). Structured on the cross-axis disjunction field
      // <see cref="ObjectFilter.AnyOf"/> (CardTypes ∨ Subtypes), consistent with the
      // "creature or Vehicle card" encoding in BroodheartEngine.
      if (simpleTypes.Contains(typeA) || simpleTypes.Contains(typeB))
      {
        var anyOf = TypeDisjunctionParser.TryParse(d);
        if (anyOf is not null)
        {
          return new ObjectFilter { AnyOf = anyOf, Controller = controller };
        }
      }
    }

    // Color-disjunctive shape: "colorA or colorB creature"
    // (e.g. "red or green creature", "white or blue creature").
    // Rule 105: multiple entries in Colors[] encode a disjunction — the filter
    // matches any creature that has at least one of the listed colors.
    var colorOrMatch = Regex.Match(
      d,
      @"^(?<colorA>white|blue|black|red|green)\s+or\s+(?<colorB>white|blue|black|red|green)\s+creature$",
      RegexOptions.IgnoreCase
    );
    if (colorOrMatch.Success)
    {
      var colorA = MapColorNameToSymbol(colorOrMatch.Groups["colorA"].Value.ToLowerInvariant());
      var colorB = MapColorNameToSymbol(colorOrMatch.Groups["colorB"].Value.ToLowerInvariant());
      if (colorA is not null && colorB is not null)
      {
        return new ObjectFilter
        {
          CardTypes = ["creature"],
          Colors = [colorA, colorB],
          Controller = controller,
        };
      }
    }

    // Zone-qualified shape: "creature card in a graveyard" (Animate Dead CR 702.5 /
    // CR 303.4 — Auras that enchant graveyard cards). "creature card" is a creature
    // card (non-token, in the graveyard zone). The zone qualifier lands on
    // ObjectFilter.Zone; the card type remains "creature" (CR 109.1 — a "card" in
    // oracle text refers to any card, including creature cards in a graveyard zone).
    // CR 702.5a: the enchant ability restricts what the Aura can enchant.
    var zoneMatch = Regex.Match(
      d,
      @"^(?<type>creature|artifact|enchantment|permanent|land|card)\s+card\s+in\s+(?:a|an|the)\s+(?<zone>graveyard|exile|hand|library)$",
      RegexOptions.IgnoreCase
    );
    if (zoneMatch.Success)
    {
      var cardType = zoneMatch.Groups["type"].Value.ToLowerInvariant();
      var zoneName = zoneMatch.Groups["zone"].Value.ToLowerInvariant();
      var zone = zoneName switch
      {
        "graveyard" => Zone.Graveyard,
        "exile"     => Zone.Exile,
        "hand"      => Zone.Hand,
        "library"   => Zone.Library,
        _           => (Zone?)null,
      };
      if (zone is not null)
      {
        return new ObjectFilter
        {
          CardTypes = [cardType],
          Zone = zone.Value,
          Controller = controller,
        };
      }
    }

    return null;
  }

  private static string? MapColorNameToSymbol(string colorName) => colorName switch
  {
    "white" => "W",
    "blue"  => "U",
    "black" => "B",
    "red"   => "R",
    "green" => "G",
    _       => null,
  };
}
