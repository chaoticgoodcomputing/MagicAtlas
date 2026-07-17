namespace MagicAST.Parsing.Parsers;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;

/// <summary>
/// Parses Class clusters (CR 716, "Class Cards"; AFR). Consumes a Class cluster
/// head clause (one that <see cref="ClauseSplitter"/> has pre-grouped with its
/// base abilities on <c>ClassBaseAbilities</c> and its level bars on
/// <c>ClassLevels</c>), then dispatches each base/granted ability body through
/// the registry and converts each level bar's raw cost text into a
/// <see cref="Cost"/>.
/// </summary>
[OracleAbilityParser(AbilityKind.Class)]
public sealed class ClassAbilityParser : IAbilityParser
{
  private readonly AbilityClassifier _classifier = new();
  private readonly AbilityParserRegistry _registry = new();
  private readonly ManaCostParser _manaCostParser = new();
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    if (clause.ClassLevels is null || clause.ClassLevels.Count == 0)
    {
      // Defensive: classifier should only route us pre-grouped clusters.
      return
      [
        _fallback.Parse(
          clause,
          classification,
          "Class parser invoked without level bars",
          lastAttemptedRule: "ClassAbilityParser.Parse",
          failurePosition: clause.SourceSpan.Start
        ),
      ];
    }

    var baseAbilities = DispatchAll(clause.ClassBaseAbilities);

    var levels = new List<ClassLevel>(clause.ClassLevels.Count);
    foreach (var levelClause in clause.ClassLevels)
    {
      levels.Add(
        new ClassLevel
        {
          Level = levelClause.Level,
          Cost = ParseCost(levelClause.CostText),
          Abilities = DispatchAll(levelClause.AbilityClauses),
        }
      );
    }

    return [new ClassAbility { BaseAbilities = baseAbilities, Levels = levels }];
  }

  /// <summary>
  /// Dispatches each body sub-clause through the registry (classify → parse),
  /// flattening the produced abilities into one list in source order.
  /// </summary>
  private IReadOnlyList<Ability> DispatchAll(IReadOnlyList<OracleClause>? clauses)
  {
    if (clauses is null || clauses.Count == 0)
    {
      return [];
    }
    var result = new List<Ability>(clauses.Count);
    foreach (var body in clauses)
    {
      var bodyClassification = _classifier.Classify(body);
      // Per-body provenance: carry each base/level body clause's span onto its ability, so a Class
      // level's ports trace to that level's text, not line 0 (OracleParser only span-stamps top-level
      // clauses; its recursion fills OracleLineIndex from this span).
      foreach (var ability in _registry.GetParser(bodyClassification.Kind).Parse(body, bodyClassification))
        result.Add(ability with { SourceSpan = ability.SourceSpan ?? body.SourceSpan });
    }
    return result;
  }

  /// <summary>
  /// Converts the raw level-bar cost text (e.g. <c>"{1}{R}"</c>) into a
  /// <see cref="ManaCost"/>. Class level bars are mana-only in the AFR cycle
  /// (CR 716.2); the structured symbols come from the shared
  /// <see cref="ManaCostParser"/>.
  /// </summary>
  private Cost ParseCost(string costText)
  {
    var parsed = _manaCostParser.Parse(costText);
    return new ManaCost { Symbols = parsed.Symbols };
  }
}
