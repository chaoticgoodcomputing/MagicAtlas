namespace MagicAST.Tests.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Analysis;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Conformance suite for the <c>ObjectFilter</c> relation operators (operator spec / ADR-0008).
/// Each fixture pairs two filters with the <see cref="FilterRelation"/> <c>Intersects</c> must
/// produce, judged against CR type rules. The <see cref="TypeOntology"/> is the vendored
/// <c>type-ontology.json</c> the <c>mtg-rules</c> project publishes — the same artifact a
/// downstream consumer binds to. Symmetry is asserted on every case (operator-spec property).
/// </summary>
[TestFixture]
public class ObjectFilterRelationTests
{
  private static readonly TypeOntology _ontology = LoadOntology();

  private static TypeOntology LoadOntology()
  {
    var path = TestData.OntologyPath;
    return JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(path))
      ?? throw new InvalidOperationException($"Failed to deserialize TypeOntology from {path}");
  }

  [TestCaseSource(
    typeof(FilterRelationTestCaseLoader),
    nameof(FilterRelationTestCaseLoader.GetTestCaseData)
  )]
  public void Intersects_ProducesExpectedRelation(FilterRelationTestCase testCase)
  {
    var a =
      testCase.A.Deserialize<ObjectFilter>(MagicASTJsonOptions.Strict)
      ?? throw new InvalidOperationException($"Failed to deserialize filter A for {testCase.Name}");
    var b =
      testCase.B.Deserialize<ObjectFilter>(MagicASTJsonOptions.Strict)
      ?? throw new InvalidOperationException($"Failed to deserialize filter B for {testCase.Name}");

    Assert.That(
      ObjectFilterRelations.Intersects(a, b, _ontology).Relation,
      Is.EqualTo(testCase.Expected),
      $"{testCase.Name}: Intersects(A, B)"
    );

    // Intersects is symmetric (operator-spec property).
    Assert.That(
      ObjectFilterRelations.Intersects(b, a, _ontology).Relation,
      Is.EqualTo(testCase.Expected),
      $"{testCase.Name}: Intersects(B, A) — symmetry"
    );
  }

  [TestCaseSource(
    typeof(SubsumptionTestCaseLoader),
    nameof(SubsumptionTestCaseLoader.GetTestCaseData)
  )]
  public void Subsumes_ProducesExpectedRelation(SubsumptionTestCase testCase)
  {
    var sub =
      testCase.Sub.Deserialize<ObjectFilter>(MagicASTJsonOptions.Strict)
      ?? throw new InvalidOperationException($"Failed to deserialize sub for {testCase.Name}");
    var sup =
      testCase.Sup.Deserialize<ObjectFilter>(MagicASTJsonOptions.Strict)
      ?? throw new InvalidOperationException($"Failed to deserialize sup for {testCase.Name}");

    Assert.That(
      ObjectFilterRelations.Subsumes(sub, sup, _ontology).Value,
      Is.EqualTo(testCase.Expected),
      $"{testCase.Name}: Subsumes(sub, sup)"
    );
  }

  /// <summary>Provenance (operator-spec open-Q3): every Disjoint/Unknown names the deciding axis.</summary>
  [Test]
  public void Verdicts_carry_a_provenance_reason()
  {
    var instant = new ObjectFilter { CardTypes = ["instant"] };
    var sorcery = new ObjectFilter { CardTypes = ["sorcery"] };
    var unknown = ObjectFilterRelations.Intersects(instant, sorcery, _ontology);
    Assert.That(unknown.Relation, Is.EqualTo(FilterRelation.Unknown));
    Assert.That(unknown.Reason, Is.EqualTo("Types"));

    var you = new ObjectFilter { Controller = ControllerFilter.You };
    var opponent = new ObjectFilter { Controller = ControllerFilter.Opponent };
    var disjoint = ObjectFilterRelations.Intersects(you, opponent, _ontology);
    Assert.That(disjoint.Relation, Is.EqualTo(FilterRelation.Disjoint));
    Assert.That(disjoint.Reason, Is.EqualTo("Controller"));

    var overlaps = ObjectFilterRelations.Intersects(you, you, _ontology);
    Assert.That(overlaps.Relation, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(overlaps.Reason, Is.Null);

    // A relational axis floors to Unknown and names itself.
    var chosen = new ObjectFilter { ChosenCharacteristic = ChosenCharacteristicKind.CreatureType };
    var floored = ObjectFilterRelations.Intersects(chosen, you, _ontology);
    Assert.That(floored.Relation, Is.EqualTo(FilterRelation.Unknown));
    Assert.That(floored.Reason, Is.EqualTo("ChosenCharacteristic"));
  }

  /// <summary>The coverage rollup tallies verdicts, ranks Unknown reasons, and counts relational-axis demand.</summary>
  [Test]
  public void FilterCoverage_tallies_verdicts_reasons_and_relational_axes()
  {
    var filters = new List<ObjectFilter>
    {
      new() { Controller = ControllerFilter.You },
      new() { Controller = ControllerFilter.Opponent },
      new() { CardTypes = ["instant"] },
      new() { CardTypes = ["sorcery"] },
      new() { ChosenCharacteristic = ChosenCharacteristicKind.CreatureType },
    };

    var report = FilterCoverage.Analyze(filters, _ontology);

    Assert.That(report.FilterCount, Is.EqualTo(5));
    Assert.That(report.PairCount, Is.EqualTo(10)); // C(5,2)
    Assert.That(
      report.IntersectVerdicts.GetValueOrDefault("Disjoint"),
      Is.GreaterThan(0),
      "You ⊥ Opponent is a Disjoint pair"
    );
    Assert.That(
      report.IntersectUnknownReasons.Any(t => t.Axis == "Types"),
      Is.True,
      "instant ∧ sorcery should surface 'Types' as an Unknown reason"
    );
    Assert.That(
      report.IntersectUnknownReasons.Any(t => t.Axis == "ChosenCharacteristic"),
      Is.True,
      "the relational ChosenCharacteristic axis should surface as an Unknown reason"
    );
    Assert.That(
      report.RelationalAxisFrequency.Any(t => t.Axis == "ChosenCharacteristic" && t.Count == 1),
      Is.True
    );
  }
}
