namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Extracts card attributes from a CardInputDTO.
/// Handles mana costs, colors, color identity, creature stats, and loyalty.
/// </summary>
public sealed partial class AttributeExtractor
{
  private readonly ManaCostParser _manaCostParser = new();

  /// <summary>
  /// Extracts all applicable attributes from a card input.
  /// </summary>
  /// <param name="input">The card input DTO.</param>
  /// <returns>A list of card attributes.</returns>
  public IReadOnlyList<CardAttribute> Extract(CardInputDTO input)
  {
    var attributes = new List<CardAttribute>();

    // Mana cost (most cards have this, lands typically don't)
    if (!string.IsNullOrWhiteSpace(input.ManaCost))
    {
      var parsedCost = _manaCostParser.Parse(input.ManaCost);
      attributes.Add(
        new ManaCostAttribute
        {
          Raw = input.ManaCost,
          Symbols = parsedCost.Symbols,
          // Variable mana costs (containing {X}/{Y}/{Z}) have no determinate
          // mana value outside the stack — the X portion is undefined until
          // the spell is cast (Rule 107.3). Surface IsVariable as the
          // canonical signal and suppress the partial-literal ManaValue so
          // downstream consumers don't treat it as the spell's total cost.
          ManaValue = parsedCost.IsVariable
            ? null
            : (parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null),
          IsVariable = parsedCost.IsVariable,
        }
      );
    }

    // Colors — always emit, even when empty. An empty Colors[] array is the
    // canonical representation of a colorless card (CR 105.1: colorlessness is
    // the absence of color, not a color value). Doctrine: present-with-empty
    // is the descriptive shape, not absent-attribute.
    attributes.Add(new ColorsAttribute { Colors = input.Colors ?? [] });

    // Color identity is DERIVED, not echoed from source data (CR 903.4): it is a
    // computed property — the colors of mana symbols printed in the card's mana
    // cost and rules text, across every face (CR 903.4d), reminder text ignored
    // (CR 903.4c) — not anything supplied by the source database. Derived from the
    // PRINTED text (not the decomposed AST) so a keyword's definitional mana
    // (Firebending's reminder-only {R}) is excluded while its printed cost symbols
    // (Cycling {U}) still count. Always WUBRG-ordered; empty for colorless cards.
    attributes.Add(new ColorIdentityAttribute { ColorIdentity = ColorIdentityDeriver.Derive(input) });

    // Creature stats (power/toughness)
    if (!string.IsNullOrWhiteSpace(input.Power) && !string.IsNullOrWhiteSpace(input.Toughness))
    {
      attributes.Add(
        new CreatureStatsAttribute
        {
          Power = ParsePowerToughness(input.Power),
          Toughness = ParsePowerToughness(input.Toughness),
        }
      );
    }

    // Planeswalker loyalty
    if (!string.IsNullOrWhiteSpace(input.Loyalty))
    {
      attributes.Add(ParseLoyalty(input.Loyalty));
    }

    // Card layout (for multi-faced cards)
    if (!string.IsNullOrWhiteSpace(input.Layout) && input.Layout != "normal")
    {
      attributes.Add(new LayoutAttribute { Layout = input.Layout });
    }

    // Additional costs encoded in oracle text.
    // "As an additional cost to cast this spell, sacrifice a [type]."
    // These lines are skipped by ClauseSplitter (no oracle ability emitted);
    // we surface them here as AdditionalCostsAttribute on the card.
    var additionalCostsAttr = TryExtractAdditionalCosts(input.OracleText);
    if (additionalCostsAttr is not null)
    {
      attributes.Add(additionalCostsAttr);
    }

    // Bestow's alternative casting cost (CR 702.103a: "you pay [cost] rather than
    // its mana cost" — an alternative cost). The keyword's static ability (the
    // becomes-Aura / enchant-creature mode) is emitted separately by
    // BestowKeyword's combinator into Oracle.Abilities; here we surface only the
    // cost on the card as an AlternativeCostsAttribute.
    var alternativeCostsAttr = TryExtractBestowCost(input.OracleText);
    if (alternativeCostsAttr is not null)
    {
      attributes.Add(alternativeCostsAttr);
    }

    // Borderpost cycle's alternative cost (CR 118.9: "You may [action] rather than pay
    // [this object]'s mana cost" is an alternative cost). The static ability functions
    // while the spell is on the stack (CR 604.5), so — like Bestow's cost above — it is
    // hosted on the card's cost attributes rather than surfaced as an oracle ability.
    var borderpostAlternativeCostAttr = TryExtractBorderpostAlternativeCost(input.OracleText);
    if (borderpostAlternativeCostAttr is not null)
    {
      attributes.Add(borderpostAlternativeCostAttr);
    }

    // Red "free spell" family's pitch alternative cost (Cave-In, Force of Will, ...).
    // CR 118.9: "You may [action] rather than pay [this object]'s mana cost" is an
    // alternative cost; CR 604.5: it functions while the spell is on the stack — so,
    // like Bestow's and the Borderpost cycle's costs above, it is hosted on the card's
    // cost attributes rather than surfaced as an oracle ability.
    var pitchAlternativeCostAttr = TryExtractPitchAlternativeCost(input.OracleText);
    if (pitchAlternativeCostAttr is not null)
    {
      attributes.Add(pitchAlternativeCostAttr);
    }

    // "Sacrifice a creature" alternative-cost family (Flare of Duplication, ...). CR
    // 118.9: "You may [action] rather than pay [this object]'s mana cost" is an
    // alternative cost; CR 604.5: it functions while the spell is on the stack — so,
    // like Bestow's, Borderpost's, and the Pitch family's costs above, it is hosted on
    // the card's cost attributes rather than surfaced as an oracle ability.
    var sacrificeAlternativeCostAttr = TryExtractSacrificeAlternativeCost(input.OracleText);
    if (sacrificeAlternativeCostAttr is not null)
    {
      attributes.Add(sacrificeAlternativeCostAttr);
    }

    // Commander-conditional free-cast family (Deadly Rollick, ...). CR 118.9: "You
    // may [action] rather than pay [this object's] mana cost" is an alternative
    // cost; CR 604.5: it functions while the spell is on the stack — so, like
    // Bestow's, Borderpost's, Pitch's, and the Sacrifice family's costs above, it
    // is hosted on the card's cost attributes rather than surfaced as an oracle
    // ability.
    var commanderFreeCastAlternativeCostAttr = TryExtractCommanderFreeCastAlternativeCost(
      input.OracleText
    );
    if (commanderFreeCastAlternativeCostAttr is not null)
    {
      attributes.Add(commanderFreeCastAlternativeCostAttr);
    }

    return attributes;
  }

  /// <summary>
  /// Extracts attributes from a card face DTO.
  /// </summary>
  /// <param name="face">The card face DTO.</param>
  /// <returns>A list of card attributes.</returns>
  public IReadOnlyList<CardAttribute> ExtractFromFace(CardFaceDTO face)
  {
    var attributes = new List<CardAttribute>();

    // Mana cost
    if (!string.IsNullOrWhiteSpace(face.ManaCost))
    {
      var parsedCost = _manaCostParser.Parse(face.ManaCost);
      attributes.Add(
        new ManaCostAttribute
        {
          Raw = face.ManaCost,
          Symbols = parsedCost.Symbols,
          // Variable mana costs (containing {X}/{Y}/{Z}) have no determinate
          // mana value outside the stack — the X portion is undefined until
          // the spell is cast (Rule 107.3). Surface IsVariable as the
          // canonical signal and suppress the partial-literal ManaValue so
          // downstream consumers don't treat it as the spell's total cost.
          ManaValue = parsedCost.IsVariable
            ? null
            : (parsedCost.ManaValue > 0 ? parsedCost.ManaValue : null),
          IsVariable = parsedCost.IsVariable,
        }
      );
    }

    // Colors
    if (face.Colors is { Count: > 0 })
    {
      attributes.Add(new ColorsAttribute { Colors = face.Colors });
    }

    // Creature stats
    if (!string.IsNullOrWhiteSpace(face.Power) && !string.IsNullOrWhiteSpace(face.Toughness))
    {
      attributes.Add(
        new CreatureStatsAttribute
        {
          Power = ParsePowerToughness(face.Power),
          Toughness = ParsePowerToughness(face.Toughness),
        }
      );
    }

    // Planeswalker loyalty
    if (!string.IsNullOrWhiteSpace(face.Loyalty))
    {
      attributes.Add(ParseLoyalty(face.Loyalty));
    }

    return attributes;
  }

  // "As an additional cost to cast this spell, sacrifice a <type>."
  // or "sacrifice <N> <type>s" — currently only "sacrifice a" (quantity=1) is
  // handled, which covers the entire family-B corpus.
  [GeneratedRegex(
    @"^As an additional cost to cast this spell,\s+sacrifice\s+a\s+(?<type>[a-z]+)\.",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex AdditionalSacrificePrefix();

  // "As an additional cost to cast this spell, sacrifice a[n] <type> or [a[n]] <type>."
  // A type-disjunction sacrifice cost: the sacrificed permanent may be either card type
  // (CR 118.8 — additional costs; the "or" between two card types is a disjunctive filter,
  // modelled as CardTypes=[t1, t2], matching the "an artifact or creature" encoding on
  // Panharmonicon). The trailing "." is anchored immediately after the second type so this
  // does NOT capture the distinct alternative-cost shapes ("sacrifice a creature or discard a
  // card." / "... or pay {3}.") — those have text between the second word and the period and
  // stay unrecognised here (left for a future batch), exactly as before this branch was added.
  [GeneratedRegex(
    @"^As an additional cost to cast this spell,\s+sacrifice\s+an?\s+(?<t1>[a-z]+)\s+or\s+(?:an?\s+)?(?<t2>[a-z]+)\.",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex AdditionalSacrificeDisjunctionPrefix();

  /// <summary>
  /// Scans oracle text for "As an additional cost to cast this spell, sacrifice a [type]."
  /// prefix lines and returns an AdditionalCostsAttribute when found, or null when absent.
  /// Handles the sacrifice-a-permanent family; other cost shapes (discard, pay life) are
  /// not yet recognised here and are left unparsed until a future batch extends this method.
  /// </summary>
  private AdditionalCostsAttribute? TryExtractAdditionalCosts(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    // Oracle text is newline-separated; additional-cost lines are always the first line.
    var firstLine = oracleText.Split('\n')[0].Trim();

    var sacrificeMatch = AdditionalSacrificePrefix().Match(firstLine);
    if (sacrificeMatch.Success)
    {
      var cardType = sacrificeMatch.Groups["type"].Value.ToLowerInvariant();
      var sacrificeCost = new AdditionalCost
      {
        Cost = new SacrificeCost
        {
          Filter = new ObjectFilter { CardTypes = [cardType] },
          Quantity = LiteralQuantity.Of(1),
        },
        SourceSpan = new TextSpan(0, firstLine.Length),
      };

      return new AdditionalCostsAttribute { Costs = [sacrificeCost] };
    }

    // Type-disjunction sacrifice ("sacrifice an artifact or creature.") — one permanent that
    // is either card type. Checked after the single-type branch above (which requires the
    // period to fall right after one type word, so it never matches the "X or Y" form).
    var disjunctionMatch = AdditionalSacrificeDisjunctionPrefix().Match(firstLine);
    if (disjunctionMatch.Success)
    {
      var t1 = disjunctionMatch.Groups["t1"].Value.ToLowerInvariant();
      var t2 = disjunctionMatch.Groups["t2"].Value.ToLowerInvariant();
      var disjunctionCost = new AdditionalCost
      {
        Cost = new SacrificeCost
        {
          Filter = new ObjectFilter { CardTypes = [t1, t2] },
          Quantity = LiteralQuantity.Of(1),
        },
        SourceSpan = new TextSpan(0, firstLine.Length),
      };

      return new AdditionalCostsAttribute { Costs = [disjunctionCost] };
    }

    // Kicker (CR 702.33) is NOT extracted here. It is a static ability — its combinator
    // (KickerKeyword) emits a StaticAbility{KeywordSource:"Kicker", AdditionalCastCostEffect}
    // into Oracle.Abilities, carrying the keyword identity the anonymous
    // AdditionalCostsAttribute would lose. Surfacing it here too would double-count the cost.

    return null;
  }

  // "Bestow {cost}" — the keyword line is always the card's first paragraph.
  [GeneratedRegex(
    @"^Bestow\s+(?<cost>(?:\{[^}]+\})+)",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex BestowPrefix();

  /// <summary>
  /// Scans oracle text for a "Bestow {cost}" line and returns an
  /// <see cref="AlternativeCostsAttribute"/> carrying the bestow casting cost, or
  /// null when absent. CR 702.103a: "you pay [cost] rather than its mana cost" —
  /// the bestow cost is an alternative cost, so it is hosted on the card's cost
  /// attributes rather than in an oracle ability. The SourceSpan is zero-width
  /// (Start 0, Length 0): the cost is synthesised from the keyword expansion and
  /// carries no meaningful source offset.
  /// </summary>
  private AlternativeCostsAttribute? TryExtractBestowCost(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    var firstLine = oracleText.Split('\n')[0].Trim();
    var match = BestowPrefix().Match(firstLine);
    if (!match.Success)
    {
      return null;
    }

    var parsed = _manaCostParser.Parse(match.Groups["cost"].Value);

    var cost = new AlternativeCost
    {
      Cost = new ManaCost { Symbols = parsed.Symbols },
      SourceSpan = new TextSpan(0, 0),
    };

    return new AlternativeCostsAttribute { Costs = [cost] };
  }

  // "You may pay {cost} and return a basic land you control to its owner's hand rather
  // than pay this spell's mana cost." — the Borderpost cycle's alternative cost. Kept
  // narrow to this specific sentence (not a generalised "rather than pay this spell's
  // mana cost" matcher) so it does not swallow other keyword-driven alternative costs
  // handled elsewhere (e.g. Bestow, above).
  [GeneratedRegex(
    @"^You may pay (?<cost>(?:\{[^}]+\})+) and return a basic land you control to its owner's hand rather than pay this spell's mana cost\.?$",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex BorderpostAlternativeCostPrefix();

  /// <summary>
  /// Scans oracle text for the Borderpost cycle's "You may pay {1} and return a basic
  /// land you control to its owner's hand rather than pay this spell's mana cost." line
  /// and returns an <see cref="AlternativeCostsAttribute"/> carrying the composite cost
  /// (mana + return-a-basic-land), or null when absent.
  ///
  /// <para>
  /// CR 118.9: "Some spells have alternative costs. ... Alternative costs are usually
  /// phrased, 'You may [action] rather than pay [this object's] mana cost,' ..." CR
  /// 604.5: "... abilities that say ... 'You may pay [cost] rather than pay [this
  /// object]'s mana cost' ... work while a spell is on the stack." The line is
  /// therefore a card-level cost attribute, not an oracle ability — mirroring
  /// <see cref="TryExtractBestowCost"/> above. Unlike Bestow's synthesised zero-width
  /// span, this cost is parsed directly from prose, so its SourceSpan covers the line
  /// (matching <see cref="TryExtractAdditionalCosts"/>'s convention).
  /// </para>
  /// </summary>
  private AlternativeCostsAttribute? TryExtractBorderpostAlternativeCost(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    var firstLine = oracleText.Split('\n')[0].Trim();
    var match = BorderpostAlternativeCostPrefix().Match(firstLine);
    if (!match.Success)
    {
      return null;
    }

    var parsed = _manaCostParser.Parse(match.Groups["cost"].Value);

    var cost = new AlternativeCost
    {
      Cost = new CompositeCost
      {
        Costs =
        [
          new ManaCost { Symbols = parsed.Symbols },
          new ReturnToHandCost
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Any,
              Filter = new ObjectFilter
              {
                CardTypes = ["land"],
                Supertypes = ["Basic"],
                Controller = ControllerFilter.You,
              },
            },
          },
        ],
      },
      SourceSpan = new TextSpan(0, firstLine.Length),
    };

    return new AlternativeCostsAttribute { Costs = [cost] };
  }

  // "You may exile a [color] card from your hand rather than pay this spell's mana
  // cost." — the red "free spell" family's pitch alternative cost (Cave-In, Force of
  // Will, Misdirection, ...). Kept narrow to this specific sentence so it does not
  // swallow other "rather than pay this spell's mana cost" costs handled elsewhere
  // (Bestow, Borderpost).
  [GeneratedRegex(
    @"^You may exile a (?<color>white|blue|black|red|green) card from your hand rather than pay this spell's mana cost\.?$",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex PitchAlternativeCostPrefix();

  /// <summary>
  /// Scans oracle text for the red "free spell" family's "You may exile a [color] card
  /// from your hand rather than pay this spell's mana cost." line and returns an
  /// <see cref="AlternativeCostsAttribute"/> carrying the exile cost, or null when
  /// absent.
  ///
  /// <para>
  /// CR 118.9: "Some spells have alternative costs. ... Alternative costs are usually
  /// phrased, 'You may [action] rather than pay [this object's] mana cost,' ..." CR
  /// 118.9a: "Only one alternative cost can be applied to any one spell as it's being
  /// cast." CR 604.5: "... abilities that say ... 'You may pay [cost] rather than pay
  /// [this object]'s mana cost' ... work while a spell is on the stack." The line is
  /// therefore a card-level cost attribute, not an oracle ability — mirroring
  /// <see cref="TryExtractBorderpostAlternativeCost"/> above.
  /// </para>
  /// </summary>
  private AlternativeCostsAttribute? TryExtractPitchAlternativeCost(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    var firstLine = oracleText.Split('\n')[0].Trim();
    var match = PitchAlternativeCostPrefix().Match(firstLine);
    if (!match.Success)
    {
      return null;
    }

    var colorCode = match.Groups["color"].Value.ToLowerInvariant() switch
    {
      "white" => "W",
      "blue" => "U",
      "black" => "B",
      "red" => "R",
      "green" => "G",
      _ => throw new InvalidOperationException("Unreachable: regex only matches the five color words."),
    };

    var cost = new AlternativeCost
    {
      Cost = new ExileCost
      {
        Filter = new ObjectFilter { CardTypes = ["card"], Colors = [colorCode] },
        Quantity = LiteralQuantity.Of(1),
        FromZone = Zone.Hand,
      },
      SourceSpan = new TextSpan(0, firstLine.Length),
    };

    return new AlternativeCostsAttribute { Costs = [cost] };
  }

  // "You may sacrifice a nontoken [color] creature rather than pay this spell's mana
  // cost." — the "sacrifice a creature" alternative-cost family (Flare of Duplication,
  // ...). Kept narrow to this specific sentence so it does not swallow other "rather
  // than pay this spell's mana cost" costs handled elsewhere (Bestow, Borderpost, Pitch).
  [GeneratedRegex(
    @"^You may sacrifice a nontoken (?<color>white|blue|black|red|green) creature rather than pay this spell's mana cost\.?$",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex SacrificeAlternativeCostPrefix();

  /// <summary>
  /// Scans oracle text for the "sacrifice a creature" alternative-cost family's "You
  /// may sacrifice a nontoken [color] creature rather than pay this spell's mana cost."
  /// line and returns an <see cref="AlternativeCostsAttribute"/> carrying the sacrifice
  /// cost, or null when absent.
  ///
  /// <para>
  /// CR 118.9: "Some spells have alternative costs. An alternative cost is a cost
  /// listed in a spell's text, or applied to it from another effect, that its
  /// controller may pay rather than paying the spell's mana cost. Alternative costs
  /// are usually phrased, 'You may [action] rather than pay [this object's] mana
  /// cost,' ..." CR 604.5: "... abilities that say ... 'You may pay [cost] rather than
  /// pay [this object]'s mana cost' ... work while a spell is on the stack." The line
  /// is therefore a card-level cost attribute, not an oracle ability — mirroring
  /// <see cref="TryExtractPitchAlternativeCost"/> above. The "nontoken" qualifier is
  /// recorded on the filter's <c>IsToken</c> axis (CR 111) rather than dropped.
  /// </para>
  /// </summary>
  private AlternativeCostsAttribute? TryExtractSacrificeAlternativeCost(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    var firstLine = oracleText.Split('\n')[0].Trim();
    var match = SacrificeAlternativeCostPrefix().Match(firstLine);
    if (!match.Success)
    {
      return null;
    }

    var colorCode = match.Groups["color"].Value.ToLowerInvariant() switch
    {
      "white" => "W",
      "blue" => "U",
      "black" => "B",
      "red" => "R",
      "green" => "G",
      _ => throw new InvalidOperationException("Unreachable: regex only matches the five color words."),
    };

    var cost = new AlternativeCost
    {
      Cost = new SacrificeCost
      {
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Colors = [colorCode],
          IsToken = false,
        },
        Quantity = LiteralQuantity.Of(1),
      },
      SourceSpan = new TextSpan(0, firstLine.Length),
    };

    return new AlternativeCostsAttribute { Costs = [cost] };
  }

  // "If you control a commander, you may cast this spell without paying its mana
  // cost." — the commander-conditional free-cast family (Deadly Rollick, ...).
  // Anchored (^…$, whole line) so it does not swallow other alternative-cost
  // lines handled elsewhere (Bestow, Borderpost, Pitch, Sacrifice).
  [GeneratedRegex(
    @"^If you control a commander, you may cast this spell without paying its mana cost\.?$",
    RegexOptions.IgnoreCase
  )]
  private static partial Regex CommanderFreeCastAlternativeCostPrefix();

  /// <summary>
  /// Scans oracle text for the commander-conditional free-cast family's "If you
  /// control a commander, you may cast this spell without paying its mana cost."
  /// line and returns an <see cref="AlternativeCostsAttribute"/> carrying a
  /// zero-symbol <see cref="ManaCost"/> gated on a <see cref="CountCondition"/>,
  /// or null when absent.
  ///
  /// <para>
  /// CR 118.9 (verbatim): "Some spells have alternative costs. ... Alternative
  /// costs are usually phrased, ... 'You may cast [this object] without paying
  /// its mana cost.'" CR 903 (Commander format) supplies the gating fact "you
  /// control a commander" — a board-state count, not a game action — so it is
  /// recorded as a <see cref="CountCondition"/> on
  /// <see cref="AlternativeCost.Condition"/> (the field this record type carries
  /// for exactly this purpose) rather than folded into the cost itself. The
  /// filter shape (Commander-supertype creature, controller You, count ≥ 1)
  /// mirrors the one <c>ModalAbilityParser</c> builds for the sibling "If you
  /// control a commander as you cast this spell, you may choose both instead."
  /// mode-expansion condition. The alternative <see cref="Cost"/> is a
  /// zero-symbol <see cref="ManaCost"/> — "without paying its mana cost" reads as
  /// paying no mana at all, distinct from an explicit "{0}" cost (Rooftop Storm's
  /// "You may pay {0} rather than pay the mana cost", handled by the
  /// <c>GrantAlternativeCostRule</c> static rule, which targets OTHER spells
  /// rather than this card's own casting cost).
  /// </para>
  /// </summary>
  private AlternativeCostsAttribute? TryExtractCommanderFreeCastAlternativeCost(string? oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return null;
    }

    var firstLine = oracleText.Split('\n')[0].Trim();
    var match = CommanderFreeCastAlternativeCostPrefix().Match(firstLine);
    if (!match.Success)
    {
      return null;
    }

    var cost = new AlternativeCost
    {
      Cost = new ManaCost { Symbols = [] },
      Condition = new CountCondition
      {
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Supertypes = ["Commander"],
          Controller = ControllerFilter.You,
        },
        Count = new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = 1 },
      },
      SourceSpan = new TextSpan(0, firstLine.Length),
    };

    return new AlternativeCostsAttribute { Costs = [cost] };
  }

  /// <summary>
  /// Parses a power or toughness value into the appropriate AST node.
  /// Handles: "3", "*", "1+*", "*+1", etc.
  /// </summary>
  private static PowerToughnessValue ParsePowerToughness(string value)
  {
    var trimmed = value.Trim();

    // Pure numeric
    if (int.TryParse(trimmed, out var numericValue))
    {
      return new FixedPTValue { Raw = value, Value = numericValue };
    }

    // Pure variable (just "*")
    if (trimmed == "*")
    {
      return new VariablePTValue { Raw = value };
    }

    // Derived value like "1+*" or "*+1"
    if (trimmed.Contains('+') || trimmed.Contains('-'))
    {
      // Try to extract the base value
      var numericPart = trimmed.Replace("*", "").Replace("+", "").Replace("-", "").Trim();
      if (int.TryParse(numericPart, out var baseValue))
      {
        return new DerivedPTValue { Raw = value, BaseValue = baseValue };
      }
    }

    // Default to variable if we can't parse it
    return new VariablePTValue { Raw = value };
  }

  /// <summary>
  /// Parses a loyalty value into a LoyaltyAttribute.
  /// </summary>
  private static LoyaltyAttribute ParseLoyalty(string value)
  {
    var trimmed = value.Trim();

    // Variable loyalty (X)
    if (trimmed.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      return new LoyaltyAttribute
      {
        Raw = value,
        StartingLoyalty = null,
        IsVariable = true,
      };
    }

    // Numeric loyalty
    if (int.TryParse(trimmed, out var numericValue))
    {
      return new LoyaltyAttribute
      {
        Raw = value,
        StartingLoyalty = numericValue,
        IsVariable = false,
      };
    }

    // Unknown format
    return new LoyaltyAttribute
    {
      Raw = value,
      StartingLoyalty = null,
      IsVariable = false,
    };
  }
}
