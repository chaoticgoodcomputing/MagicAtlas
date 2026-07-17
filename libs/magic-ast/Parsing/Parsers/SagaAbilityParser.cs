namespace MagicAST.Parsing.Parsers;

using MagicAST.AST.Abilities;

/// <summary>
/// Parses Saga chapter structures (Rule 715). Consumes a saga-preamble clause
/// (one that <see cref="ClauseSplitter"/> has pre-grouped with its chapter
/// bodies on <c>SagaChapters</c>), then dispatches each chapter body through
/// the registry so individual chapters land as whatever ability shape best
/// matches their text.
/// </summary>
[OracleAbilityParser(AbilityKind.Saga)]
public sealed class SagaAbilityParser : IAbilityParser
{
  private readonly AbilityClassifier _classifier = new();
  private readonly AbilityParserRegistry _registry = new();
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    if (clause.SagaChapters is null || clause.SagaChapters.Count == 0)
    {
      // Defensive: classifier should only route us pre-grouped clauses.
      return
      [
        _fallback.Parse(
          clause,
          classification,
          "Saga parser invoked without chapters",
          lastAttemptedRule: "SagaAbilityParser.Parse",
          failurePosition: clause.SourceSpan.Start
        ),
      ];
    }

    var chapters = new List<SagaChapter>(clause.SagaChapters.Count);
    foreach (var chapterClause in clause.SagaChapters)
    {
      var bodyClassification = _classifier.Classify(chapterClause);
      var bodyAbilities = _registry.GetParser(bodyClassification.Kind).Parse(chapterClause, bodyClassification);

      // Each chapter has a single body. If the dispatched parser returns
      // multiple abilities (rare; e.g. comma-separated keywords on a chapter
      // body), wrap them in the first as the canonical body and surface the
      // rest as an UnparsedAbility — a follow-up.
      var body =
        bodyAbilities.Count > 0
          ? bodyAbilities[0]
          : _fallback.Parse(
              chapterClause,
              bodyClassification,
              "Saga chapter body produced no abilities",
              lastAttemptedRule: "SagaAbilityParser.Parse",
              failurePosition: chapterClause.SourceSpan.Start
            );

      chapters.Add(
        new SagaChapter
        {
          Numbers = chapterClause.ChapterNumbers ?? Array.Empty<int>(),
          // Per-chapter provenance: the dispatched body parser attributes spans within the chapter, but
          // the body ability itself is only span-stamped for TOP-LEVEL clauses in OracleParser. Carry the
          // chapter clause's own span so a chapter's ports trace to the chapter text, not line 0 (the
          // OracleParser recursion then fills OracleLineIndex). A Saga chapter fires on a lore counter
          // (CR 714) — its ports are a counter/proliferate interaction surface once Saga bodies project.
          Body = body with { SourceSpan = body.SourceSpan ?? chapterClause.SourceSpan },
        }
      );
    }

    return [new SagaAbility { Chapters = chapters }];
  }
}
