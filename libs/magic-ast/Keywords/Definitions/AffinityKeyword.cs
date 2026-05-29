namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Affinity for [text]: This spell costs {1} less to cast for each [text] you control.
/// Rule 702.41. The parameter is a free-text type/subtype label (e.g., "artifacts",
/// "Cats", "Plains", "historic permanents"). MAST captures it as a structured
/// <see cref="ObjectFilter"/> on the cost-reduction's per-object axis.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Affinity</c>
/// (including its inlined <c>BuildAffinityFilter</c> helper); the <see cref="Combinator"/>
/// is the verbatim former <c>OracleParsers.Affinity</c>, with the expansion inlined to
/// avoid reaching back into <c>KeywordDefinitions</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class AffinityKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
    new()
    {
      Name = "Affinity",
      RuleReference = "702.41",
      Category = KeywordCategory.CostModifier,
      HasParameter = true,
      ParameterType = KeywordParameterType.CardType,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = $"Affinity for {parameter?.Trim()}",
        Effects = [new CostReductionEffect
        {
          Amount = LiteralQuantity.Of(1),
          PerObject = BuildAffinityFilter(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Affinity")
    from forKw in Keyword("for")
    from typeWords in Token.EqualTo(OracleToken.Word).AtLeastOnce()
    from reminder in OptionalReminder
    let parameter = string.Join(" ", typeWords.Select(t => t.ToStringValue()))
    select (StaticAbility)new StaticAbility
    {
      KeywordSource = $"Affinity for {parameter}",
      Effects = [new CostReductionEffect
      {
        Amount = LiteralQuantity.Of(1),
        PerObject = BuildAffinityFilter(parameter),
      }],
    } with
    {
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Maps the literal "Affinity for X" parameter text to a structured
  /// <see cref="ObjectFilter"/> for the per-object cost-reduction axis. Rule 702.41.
  /// Inlined from the former <c>KeywordDefinitions.BuildAffinityFilter</c>.
  /// </summary>
  private static ObjectFilter BuildAffinityFilter(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Affinity requires a type parameter.", nameof(parameter));
    }

    var raw = parameter.Trim();

    // Card-type plurals: lowercase in oracle text. Singularize via trailing-s strip
    // (with the sorceries→sorcery special case mirroring Protection).
    var cardTypes = new[]
    {
      "artifacts",
      "creatures",
      "enchantments",
      "instants",
      "sorceries",
      "lands",
      "planeswalkers",
      "battles",
    };
    if (cardTypes.Contains(raw))
    {
      var singular = raw.TrimEnd('s');
      if (singular == "sorcerie")
      {
        singular = "sorcery";
      }
      else if (singular == "batt") // "battles" → "battle"
      {
        singular = "battle";
      }
      else if (singular == "land")
      {
        // "lands" already strips to "land"; keep as-is.
      }

      return new ObjectFilter
      {
        CardTypes = [singular],
        Controller = ControllerFilter.You,
      };
    }

    // Basic-land subtype labels. "Plains" is its own plural; the others
    // pluralize regularly. Match either form, normalize to the singular
    // subtype as it appears on a basic land's type line.
    var basicLandPlural = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["Plains"] = "Plains",
      ["Islands"] = "Island",
      ["Swamps"] = "Swamp",
      ["Mountains"] = "Mountain",
      ["Forests"] = "Forest",
    };
    if (basicLandPlural.TryGetValue(raw, out var basicSubtype))
    {
      return new ObjectFilter
      {
        Subtypes = [basicSubtype],
        Controller = ControllerFilter.You,
      };
    }

    // Capitalized plural subtype labels (creature/artifact/land subtypes other
    // than basics): "Cats", "Humans", "Frogs", "Equipment", "Gates", "Towns", ...
    // Heuristic: starts with a capital letter; singularize by trailing-s strip
    // (irregular plurals are out of scope — none in the current corpus's
    // single-word Affinity surface).
    if (char.IsUpper(raw[0]) && !raw.Contains(' '))
    {
      var singular = raw.EndsWith("s") ? raw[..^1] : raw;
      return new ObjectFilter
      {
        Subtypes = [singular],
        Controller = ControllerFilter.You,
      };
    }

    // Fallback: preserve the raw text as a free-form characteristic. Multi-word
    // ("historic permanents", "snow lands", "artifact creatures") and unknown
    // shapes land here. Surfaces such cards for follow-up parsing rather than
    // silently mis-routing them through a singular card-type or subtype branch.
    return new ObjectFilter
    {
      Characteristics = [Characteristic.FromLabel(raw.ToLowerInvariant())],
      Controller = ControllerFilter.You,
    };
  }
}
