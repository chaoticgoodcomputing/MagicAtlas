namespace MagicAST.Parsing;

using MagicAST.AST.References;

/// <summary>
/// The single shared qualifier→axis mapper. Folds raw oracle qualifier labels
/// ("nonland", "nonblack", "token", "tapped", "with a +1/+1 counter", "shares a
/// color", …) onto the structured axes of an <see cref="ObjectFilter"/>, routing
/// each label to its first-class home — <see cref="ObjectFilter.ExcludedCardTypes"/>,
/// <see cref="ObjectFilter.ExcludedSupertypes"/>, <see cref="ObjectFilter.ExcludedColors"/>,
/// <see cref="ObjectFilter.Colors"/>, <see cref="ObjectFilter.CardTypes"/>,
/// <see cref="ObjectFilter.IsToken"/>, <see cref="ObjectFilter.SharesColorWith"/> — or,
/// for state/counter predicates, a structured <see cref="Characteristic"/> variant
/// (<see cref="TappedStateCharacteristic"/>, <see cref="CounterCharacteristic"/>,
/// <see cref="CombatStateCharacteristic"/>). Anything not recognised falls back to the
/// <see cref="OtherCharacteristic"/> residual via <see cref="Characteristic.FromLabel"/>.
///
/// <para>
/// Extracted from the qualifier-handling that was duplicated across the spell /
/// static / triggered / activated rule helpers (each routed a raw label list through
/// <see cref="Characteristic.FromLabel"/>, which could only ever produce a
/// <see cref="Characteristic"/> and so could not reach the structured negation/colour
/// axes). Routing every call site through this one mapper collapses the scattered
/// per-rule touches into a single place and de-strings the type/colour/state/counter
/// residuals at once.
/// </para>
/// </summary>
public static class QualifierAxisMapper
{
  // Color name → single-letter code (WUBRG), used for both the bare-color and the
  // "non<color>" negation axes (CR 105.1).
  private static readonly IReadOnlyDictionary<string, string> _colorCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  // Card types (lowercase, singular) recognised as a bare type or a "non<type>" exclusion.
  private static readonly HashSet<string> _cardTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "artifact", "creature", "enchantment", "instant", "sorcery",
      "planeswalker", "land", "battle", "tribal", "permanent",
    };

  // Supertypes (PascalCase on emit) recognised as a "non<supertype>" exclusion
  // (e.g. "nonbasic" → ExcludedSupertypes:["Basic"], CR 205.4).
  private static readonly IReadOnlyDictionary<string, string> _supertypes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["legendary"] = "Legendary",
      ["snow"] = "Snow",
      ["basic"] = "Basic",
      ["world"] = "World",
      ["ongoing"] = "Ongoing",
    };

  /// <summary>
  /// Folds <paramref name="labels"/> onto <paramref name="filter"/>, routing each to its
  /// structured axis (or, for state/counter predicates, a structured
  /// <see cref="Characteristic"/>); unrecognised labels become an
  /// <see cref="OtherCharacteristic"/> residual. Returns a new filter; the input is
  /// unmodified. Labels are matched case-insensitively. Pass <see langword="null"/> or an
  /// empty sequence to get <paramref name="filter"/> back unchanged.
  /// </summary>
  public static ObjectFilter Apply(ObjectFilter filter, IEnumerable<string>? labels)
  {
    if (labels is null)
      return filter;

    var excludedCardTypes = filter.ExcludedCardTypes?.ToList();
    var excludedSupertypes = filter.ExcludedSupertypes?.ToList();
    var excludedColors = filter.ExcludedColors?.ToList();
    var colors = filter.Colors?.ToList();
    var cardTypes = filter.CardTypes?.ToList();
    bool? isToken = filter.IsToken;
    ObjectReference? sharesColorWith = filter.SharesColorWith;
    var characteristics = filter.Characteristics?.ToList();

    foreach (var raw in labels)
    {
      var label = raw.Trim().ToLowerInvariant();
      if (label.Length == 0)
        continue;

      // "non<X>" negation: colour → ExcludedColors; supertype → ExcludedSupertypes;
      // token → IsToken=false; card type → ExcludedCardTypes.
      if (label.StartsWith("non", StringComparison.Ordinal) && label.Length > 3)
      {
        var rest = label[3..].TrimStart('-');
        if (_colorCode.TryGetValue(rest, out var negColor))
        {
          (excludedColors ??= []).Add(negColor);
          continue;
        }
        if (_supertypes.TryGetValue(rest, out var negSuper))
        {
          (excludedSupertypes ??= []).Add(negSuper);
          continue;
        }
        if (rest is "token")
        {
          isToken = false;
          continue;
        }
        if (_cardTypes.Contains(rest))
        {
          (excludedCardTypes ??= []).Add(rest);
          continue;
        }
        // Unknown negation → residual.
        (characteristics ??= []).Add(Characteristic.FromLabel(raw));
        continue;
      }

      // Bare colour → Colors.
      if (_colorCode.TryGetValue(label, out var color))
      {
        (colors ??= []).Add(color);
        continue;
      }

      // "shares a color [with it]" → relational SharesColorWith axis against the source (Self).
      if (label is "shares a color" or "shares a color with it")
      {
        sharesColorWith = ObjectReference.Self();
        continue;
      }

      // "token" → IsToken=true (CR 111; not a card type).
      if (label is "token")
      {
        isToken = true;
        continue;
      }

      // Bare card type → CardTypes.
      if (_cardTypes.Contains(label))
      {
        (cardTypes ??= []).Add(label);
        continue;
      }

      // State / counter / keyword predicate, or residual fallback — Characteristic.FromLabel
      // structures tapped/untapped/attacking/with a +1/+1 counter/keywords and leaves the
      // rest as OtherCharacteristic.
      (characteristics ??= []).Add(Characteristic.FromLabel(raw));
    }

    return filter with
    {
      ExcludedCardTypes = excludedCardTypes,
      ExcludedSupertypes = excludedSupertypes,
      ExcludedColors = excludedColors,
      Colors = colors,
      CardTypes = cardTypes,
      IsToken = isToken,
      SharesColorWith = sharesColorWith,
      Characteristics = characteristics is { Count: > 0 } ? characteristics : null,
    };
  }
}
