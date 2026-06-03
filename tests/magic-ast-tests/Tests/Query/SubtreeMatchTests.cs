namespace MagicAST.Query.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Query;
using MagicAST.Query.Patterns;
using MagicAST.Schema;

/// <summary>
/// Phase-A query foundations for the interaction engine (rollout plan A3–A5): subtree-rooted
/// matching, typed captures (the matched node, deserializable to an <c>ObjectFilter</c> for the
/// cross-query join), and the <see cref="Determinacy"/> → <see cref="Trilean"/> mapping.
/// </summary>
[TestFixture]
public class SubtreeMatchTests
{
  private static readonly FilterAndVerifyEngine Engine = new(SchemaExport.Build());

  [Test]
  public void Match_at_a_subtree_captures_a_typed_object_filter()
  {
    // A port-like subtree: an effect node whose Scope is an ObjectFilter.
    var subtree = JsonNode.Parse(
      """{ "Scope": { "Controller": "You", "CardTypes": ["creature"] } }"""
    )!;

    // Pattern rooted AT the subtree (A4) — match the node, capture its Scope filter (A3).
    var pattern = new NodePattern
    {
      Fields = [new FieldConstraint("Scope", new NodePattern { Capture = "scope" })],
    };

    var outcome = Engine.Match(pattern, subtree);

    Assert.That(outcome.Determinacy, Is.EqualTo(Determinacy.Match));
    Assert.That(outcome.Captures, Is.Not.Null);
    Assert.That(outcome.Captures!.ContainsKey("scope"), Is.True);

    // A3: the capture is a typed node — deserialize straight to an ObjectFilter for the join.
    var filter = outcome.Captures["scope"].Deserialize<ObjectFilter>(MagicASTJsonOptions.Strict);
    Assert.That(filter, Is.Not.Null);
    Assert.That(filter!.Controller, Is.EqualTo(ControllerFilter.You));
    Assert.That(filter.CardTypes, Does.Contain("creature"));
  }

  [Test]
  public void Determinacy_maps_onto_the_canonical_trilean()
  {
    Assert.That(Determinacy.Match.ToTrilean(), Is.EqualTo(Trilean.Yes));
    Assert.That(Determinacy.NoMatch.ToTrilean(), Is.EqualTo(Trilean.No));
    Assert.That(Determinacy.Unknown.ToTrilean(), Is.EqualTo(Trilean.Unknown));
  }

  [Test]
  public void Canonical_hash_is_key_order_invariant_and_structure_sensitive()
  {
    var a = JsonNode.Parse("""{ "Controller": "You", "CardTypes": ["creature"] }""")!;
    var reordered = JsonNode.Parse("""{ "CardTypes": ["creature"], "Controller": "You" }""")!;
    var different = JsonNode.Parse("""{ "Controller": "Opponent", "CardTypes": ["creature"] }""")!;

    // A2: port identity must be stable under key order…
    Assert.That(CanonicalJson.Hash(a), Is.EqualTo(CanonicalJson.Hash(reordered)));
    // …and distinguish structurally-distinct ports.
    Assert.That(CanonicalJson.Hash(a), Is.Not.EqualTo(CanonicalJson.Hash(different)));
  }
}
