namespace MagicAST.Parsing.Parsers;

using MagicAST;
using MagicAST.AST.Abilities;

/// <summary>
/// Parses Level Up clusters (Rule 711). Consumes a level-up cost clause
/// (one that <see cref="ClauseSplitter"/> has pre-grouped with its LEVEL
/// stanzas on <c>LevelUpStanzas</c>), then constructs the cost activated
/// ability + the stanza records.
/// </summary>
[OracleAbilityParser(AbilityKind.LevelUp)]
public sealed class LevelUpAbilityParser : IAbilityParser
{
  private readonly ActivatedAbilityParser _activatedParser = new();
  private readonly AbilityClassifier _classifier = new();
  private readonly AbilityParserRegistry _registry = new();
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    if (clause.LevelUpStanzas is null || clause.LevelUpStanzas.Count == 0)
    {
      return [_fallback.Parse(clause, classification, "LevelUp parser invoked without stanzas")];
    }

    // The cost paragraph is "Level up {N} ({N}: Put a level counter on this. Level up only as a sorcery.)".
    // It reads as an activated ability whose effect is the explicit reminder text.
    var costAbility = ParseLevelUpCost(clause, classification);

    var stanzas = new List<LevelStanza>(clause.LevelUpStanzas.Count);
    foreach (var stanzaClause in clause.LevelUpStanzas)
    {
      var power = ParsePowerToughnessText(stanzaClause.PowerText);
      var toughness = ParsePowerToughnessText(stanzaClause.ToughnessText);

      var innerAbilities = new List<Ability>(stanzaClause.InnerAbilityClauses.Count);
      foreach (var inner in stanzaClause.InnerAbilityClauses)
      {
        var innerClassification = _classifier.Classify(inner);
        var parsed = _registry.GetParser(innerClassification.Kind).Parse(inner, innerClassification);
        innerAbilities.AddRange(parsed);
      }

      stanzas.Add(
        new LevelStanza
        {
          MinLevel = stanzaClause.MinLevel,
          MaxLevel = stanzaClause.MaxLevel,
          Power = power,
          Toughness = toughness,
          Abilities = innerAbilities,
        }
      );
    }

    return
    [
      new LevelUpAbility { LevelUpCost = costAbility, Stanzas = stanzas },
    ];
  }

  /// <summary>
  /// Parses the "Level up {cost}" cost paragraph. The text reads as an
  /// activated ability — we synthesise a cost clause for it (the
  /// parenthetical reminder text describes the actual put-counter effect).
  /// </summary>
  /// <remarks>
  /// First cut: we use a stub <see cref="ActivatedAbility"/> with empty
  /// costs/effects so the structural shape lands. Teaching the activated
  /// parser to extract the cost from "Level up {N}" syntax is regular
  /// effect-parser work and is deferred to a follow-up.
  /// </remarks>
  private ActivatedAbility ParseLevelUpCost(OracleClause clause, ClauseClassification classification)
  {
    return new ActivatedAbility
    {
      Costs = [],
      Effects = [],
      IsManaAbility = false,
      AbilityWord = classification.AbilityWord,
      KeywordSource = "Level up",
    };
  }

  /// <summary>
  /// Converts a raw P/T string to a <see cref="PowerToughnessValue"/>.
  /// Most leveler stanzas use fixed integer values; we fall back to a
  /// fixed representation for unknown shapes.
  /// </summary>
  private static PowerToughnessValue ParsePowerToughnessText(string raw)
  {
    if (int.TryParse(raw, out var value))
    {
      return new FixedPTValue { Raw = raw, Value = value };
    }
    // Variable / derived P/T cases — for now, surface as variable so the
    // schema accepts it. Refining is per-effect work.
    return new VariablePTValue { Raw = raw };
  }
}
