namespace MagicAST.Tests.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Analysis;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// The operator property fleet (rollout C4): exercise <c>Intersects</c> / <c>Subsumes</c> over the
/// <b>whole hand-parsed corpus</b> rather than hand-picked fixtures, and assert the
/// <b>metamorphic / algebraic invariants</b> that must hold universally. At corpus scale there is no
/// per-case ground-truth oracle (that is the interaction-judge's job, B6) — so these properties are
/// chosen to catch an <em>unsound</em> verdict without one: a violation is a provable internal
/// contradiction (a false "impossible" or false "reliable"), not a judgement call. This is the
/// zero-false-positive bar of ADR-0008 enforced at scale.
///
/// The corpus is the committed gold (<c>HandParsedCards/</c>, ~1k filters). Filters are deduped by
/// their semantic shape (ignoring <see cref="ObjectFilter.SourceSpan"/>, which the operators don't
/// read) so the pairwise sweep stays cheap and deterministic.
/// </summary>
[TestFixture]
public class OperatorPropertyTests
{
  private static readonly TypeOntology _ontology = LoadOntology();
  private static readonly IReadOnlyList<ObjectFilter> _filters = HarvestDistinctFilters();

  private static TypeOntology LoadOntology()
  {
    var path = TestData.OntologyPath;
    return JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(path))
      ?? throw new InvalidOperationException($"Failed to deserialize TypeOntology from {path}");
  }

  /// <summary>Every distinct <see cref="ObjectFilter"/> in the gold corpus, keyed by semantic shape.</summary>
  private static IReadOnlyList<ObjectFilter> HarvestDistinctFilters()
  {
    var bySemantics = new Dictionary<string, ObjectFilter>(StringComparer.Ordinal);
    foreach (var testCase in HandParsedTestCaseLoader.GetAllTestCases())
    {
      IReadOnlyList<ObjectFilter> filters;
      try
      {
        filters = ObjectFilterCollector.Collect(testCase.GetOutput());
      }
      catch
      {
        continue; // a fixture that won't deserialize is not the operator's concern
      }
      foreach (var f in filters)
      {
        // The operators ignore SourceSpan (it is provenance, not a matching axis), so two filters
        // that differ only in source position are the same input to them — dedup on the rest.
        var key = JsonSerializer.Serialize(f with { SourceSpan = null }, MagicASTJsonOptions.Strict);
        bySemantics.TryAdd(key, f);
      }
    }
    return bySemantics.Values.ToList();
  }

  [Test]
  public void Harvested_a_nontrivial_corpus_of_distinct_filters()
  {
    // Guard the guard: if the harvest silently yields nothing, every property below passes vacuously.
    Assert.That(_filters.Count, Is.GreaterThan(100), "expected a substantial corpus of filters");
    TestContext.WriteLine($"distinct corpus filters: {_filters.Count}");
  }

  [Test]
  public void Intersects_is_symmetric_over_the_corpus()
  {
    var violations = new List<string>();
    for (var i = 0; i < _filters.Count; i++)
    for (var j = i + 1; j < _filters.Count; j++)
    {
      var ab = ObjectFilterRelations.Intersects(_filters[i], _filters[j], _ontology).Relation;
      var ba = ObjectFilterRelations.Intersects(_filters[j], _filters[i], _ontology).Relation;
      if (ab != ba)
        violations.Add($"pair ({i},{j}): Intersects(a,b)={ab} but Intersects(b,a)={ba}");
    }
    Assert.That(violations, Is.Empty, string.Join("\n", violations.Take(10)));
  }

  [Test]
  public void Subsumes_is_reflexive_over_the_corpus()
  {
    var falseNo = new List<string>();
    var notProven = new List<string>();
    for (var i = 0; i < _filters.Count; i++)
    {
      var self = ObjectFilterRelations.Subsumes(_filters[i], _filters[i], _ontology).Value;
      // Universal soundness: the operator must NEVER prove a filter fails to subsume itself.
      if (self == Trilean.No)
        falseNo.Add($"filter {i}: Subsumes(f,f)=No");
      // Full reflexivity holds wherever no relational axis floors the verdict to Unknown.
      else if (self != Trilean.Yes && !IsRelational(_filters[i]))
        notProven.Add($"filter {i}: Subsumes(f,f)={self}");
    }
    Assert.That(falseNo, Is.Empty, "Subsumes proved f ⊄ f: " + string.Join("\n", falseNo.Take(10)));
    Assert.That(
      notProven,
      Is.Empty,
      "Subsumes failed to prove f ⊆ f without a relational floor: " + string.Join("\n", notProven.Take(10))
    );
  }

  [Test]
  public void Subsumes_yes_implies_intersects_overlaps_for_inhabited_filters()
  {
    // The crown-jewel soundness link, checked over the whole corpus distribution: if a ⊆ b
    // (Subsumes=Yes) and a is inhabited, then a and b share a's objects, so they cannot be Disjoint.
    // A violation is a *provable contradiction* — the operator asserting both "every a is a b" and
    // "no object is both a and b" with a inhabited — i.e. a false Yes or a false Disjoint. This is
    // exactly the zero-false-positive bar (ADR-0008), enforced with no per-case oracle.
    var violations = new List<string>();
    for (var i = 0; i < _filters.Count; i++)
    {
      var a = _filters[i];
      // "Inhabited" per the operator's own vacuity verdict — it reads a self-Disjoint filter as
      // unsatisfiable, and a vacuously-empty a may subsume anything without overlapping it.
      if (ObjectFilterRelations.Intersects(a, a, _ontology).Relation == FilterRelation.Disjoint)
        continue;
      for (var j = 0; j < _filters.Count; j++)
      {
        if (ObjectFilterRelations.Subsumes(a, _filters[j], _ontology).Value != Trilean.Yes)
          continue;
        if (ObjectFilterRelations.Intersects(a, _filters[j], _ontology).Relation == FilterRelation.Disjoint)
          violations.Add($"filter {i} ⊆ filter {j}, yet Intersects(a,b)=Disjoint");
      }
    }
    Assert.That(violations, Is.Empty, string.Join("\n", violations.Take(10)));
  }

  /// <summary>
  /// A filter whose verdict the operators sound-floor to <c>Unknown</c> — so reflexivity gives
  /// <c>Unknown</c>, not <c>Yes</c>. Two families: the Phase-3 relational axes (referent decided at
  /// runtime / off-object), and a runtime-chosen controller/owner (<c>Target</c>/<c>ThatPlayer</c>/
  /// <c>EnchantedPlayer</c>) — the operator can't know <em>which</em> player that resolves to, even
  /// against itself, so it stays sound by flooring. Both are sound (Unknown, never a false No/Yes).
  /// </summary>
  private static bool IsRelational(ObjectFilter f) =>
    f.ExiledWith is not null
    || f.SharesColorWith is not null
    || f.AttachedTo is not null
    || f.ChosenCharacteristic is not null
    || f.History is not null
    || f.ExcludeSelf == true
    || f.Characteristics is not null
    || IsRuntimeChosen(f.Controller)
    || IsRuntimeChosen(f.Owner)
    // A power/toughness/mana-value Comparison whose bound is RUNTIME-RELATIVE (RelativeTo set,
    // e.g. "mana value N or less, where N is [It]'s power" — Kodama of the East Tree) floors to
    // Unknown for f⊆f, so it is a relational axis exempt from provable reflexivity, like the above.
    || IsRelativeComparison(f.PowerComparison)
    || IsRelativeComparison(f.ToughnessComparison)
    || IsRelativeComparison(f.ManaValueComparison);

  /// <summary>True for controllers whose referent is chosen at runtime (not the determinable You/Opponent/Any).</summary>
  private static bool IsRuntimeChosen(ControllerFilter? c) =>
    c is ControllerFilter.Target or ControllerFilter.ThatPlayer or ControllerFilter.EnchantedPlayer;

  /// <summary>True for a Comparison whose operand is decided at runtime (RelativeTo set).</summary>
  private static bool IsRelativeComparison(Comparison? c) => c is { RelativeTo: not null };
}
