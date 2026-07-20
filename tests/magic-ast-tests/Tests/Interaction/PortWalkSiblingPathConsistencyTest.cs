namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0004 §6 — <b>sibling-path consistency</b>, the first invariant-layer gate. It catches
/// <em>absence</em>, which no regeneration or join can: a path that was never made consistent produces
/// no drift to detect, and there is nothing about the inconsistency visible from outside — it surfaces
/// only by reading the two code paths side by side.
///
/// <para><b>Why this is a metamorphic relation and not an example test.</b> We have no oracle for "what
/// ports SHOULD a composite under a replacement project" — that is exactly the ground truth the
/// projection is trying to establish. What we CAN state is a relation between executions: <c>PortWalk</c>
/// has several <b>composite-capable</b> effect paths (the top-level ability-effects list, <c>optional</c>'s
/// <c>Inner</c>, <c>composite</c>'s <c>Effects</c>, <c>conditional</c>'s <c>Then</c>/<c>Else</c>,
/// <c>rollResultsTable</c>'s row effects, and <c>replacement</c>'s <c>Replacement</c>), and <b>the ports a
/// body projects must not depend on which of them reached it</b>. Whatever the right answer is, all the
/// paths must give the same one.</para>
///
/// <para><b>The historical failure this locks down.</b> The top-level ability-effects path recursed into
/// composite sub-effects; the sibling replacement-effects path called <c>EmitPort</c> directly, collapsing
/// a composite <c>Replacement</c> into one coarse dead-end <c>emit:composite</c>. Academy Manufactor's
/// Clue/Food/Treasure triple became a single opaque port, and the Food token's intrinsic
/// <c>ResolvePredefinedTokens</c> life-gain (ADR-0002 §9, CR 111.10b) was silently swallowed — it only
/// sees a created token whose OWN <c>createToken</c> port reached <c>ports</c>. Fixed in
/// <c>9e319ea7</c>; this test is what makes the fix non-regressible, and what makes any FUTURE
/// composite-capable path prove itself on arrival.</para>
///
/// <para><b>Falsification (required by the issue, run at authoring time).</b> Commenting the recursive
/// <c>Effects(e["Replacement"], …)</c> call in <c>PortGraph.cs</c> back to a direct
/// <c>EmitPort(e["Replacement"], …)</c> — i.e. deliberately reintroducing the asymmetry — turns the
/// <c>replacement</c> cases RED (the composite bodies collapse to a single <c>emit:composite</c> the bare
/// path never produces) while every other wrapper stays green. The relation is therefore load-bearing,
/// not vacuously satisfied.</para>
/// </summary>
[TestFixture]
public class PortWalkSiblingPathConsistencyTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  // ── The bodies. Deliberately shape-only: the relation is AGREEMENT, so the exact labels are
  // irrelevant — what matters is that a body is composite-shaped (so a non-recursing path collapses it)
  // and that it projects real flow ports (so a collapse is observable). None is a blink
  // (exile + returnToBattlefield[ExiledWith:Self]), which the whole-composite BlinkPort claims BEFORE the
  // generic recursion and which is therefore a genuinely path-specific shape, not a sibling asymmetry.

  private static JsonNode CreateToken(string subtype) =>
    JsonNode.Parse(
      $$"""
      { "EffectType": "createToken", "Count": { "QuantityType": "literal", "Value": 1 },
        "Token": { "Name": "{{subtype}}", "Types": ["Creature"], "Subtypes": ["{{subtype}}"] } }
      """
    )!;

  private static JsonNode AddMana() =>
    JsonNode.Parse("""{ "EffectType": "addMana", "Mana": "{B}" }""")!;

  private static JsonNode GainLife() =>
    JsonNode.Parse(
      """{ "EffectType": "gainLife", "Amount": { "QuantityType": "literal", "Value": 1 }, "Player": { "Kind": "You" } }"""
    )!;

  private static JsonNode Composite(params JsonNode[] children) =>
    new JsonObject
    {
      ["EffectType"] = "composite",
      ["Effects"] = new JsonArray(children.Select(c => JsonNode.Parse(c.ToJsonString())!).ToArray()),
    };

  public sealed record Body(string Name, Func<JsonNode> Make)
  {
    public override string ToString() => Name;
  }

  private static readonly Body[] Bodies =
  [
    new("single-createToken", () => CreateToken("Squirrel")),
    new("composite-token+mana", () => Composite(CreateToken("Treasure"), AddMana())),
    new("composite-triple-token", () => Composite(CreateToken("Clue"), CreateToken("Food"), CreateToken("Treasure"))),
    new("nested-composite", () => Composite(Composite(CreateToken("Food"), AddMana()), GainLife())),
  ];

  // ── The composite-capable paths. Each wraps a body so that PortWalk reaches it through a DIFFERENT
  // branch of Effects(). "bare" is the reference execution (the top-level ability-effects list).

  public sealed record Path(string Name, Func<JsonNode, JsonNode> Wrap)
  {
    public override string ToString() => Name;
  }

  private static readonly Path[] Paths =
  [
    new("optional.Inner", b => new JsonObject { ["EffectType"] = "optional", ["Inner"] = b }),
    new("composite.Effects", b => new JsonObject { ["EffectType"] = "composite", ["Effects"] = new JsonArray(b) }),
    new("conditional.Then", b => new JsonObject { ["EffectType"] = "conditional", ["Then"] = b }),
    new("conditional.Else", b => new JsonObject { ["EffectType"] = "conditional", ["Else"] = b }),
    new(
      "rollResultsTable.Rows",
      b => new JsonObject
      {
        ["EffectType"] = "rollResultsTable",
        ["Rows"] = new JsonArray(new JsonObject { ["Effects"] = new JsonArray(b) }),
      }
    ),
    new(
      "replacement.Replacement",
      b => new JsonObject
      {
        ["EffectType"] = "replacement",
        ["Event"] = new JsonObject { ["EventType"] = "tokenCreation" },
        ["Replacement"] = b,
      }
    ),
  ];

  public static IEnumerable<TestCaseData> Cases() =>
    from p in Paths
    from b in Bodies
    select new TestCaseData(p, b).SetName($"Agrees_{Slug(p.Name)}_on_{Slug(b.Name)}");

  private static string Slug(string s) =>
    new(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

  /// <summary>
  /// The metamorphic relation: the EMIT ports a body projects are the same whichever composite-capable
  /// path reached it. Emit-side only — each wrapper legitimately contributes its own structural ports
  /// (a replacement's intercept consume) and its own §8 gate flags (optional/conditional/result-table
  /// branches are Gated); those are the wrapper's semantics, not the body's. What may NOT differ is which
  /// of the body's sub-effects became ports at all.
  /// </summary>
  [TestCaseSource(nameof(Cases))]
  public void Composite_capable_paths_project_the_same_body_emits(Path path, Body body)
  {
    var walk = new PortWalk(Ontology);

    var bare = Emits(walk, body.Make());
    var viaPath = Emits(walk, path.Wrap(body.Make()));

    Assert.That(
      bare,
      Is.Not.Empty,
      "the body must project real emit ports, else the relation is vacuous"
    );
    Assert.That(
      viaPath,
      Is.EquivalentTo(bare),
      $"SIBLING-PATH ASYMMETRY: '{path.Name}' does not project the same emits for body '{body.Name}' as "
        + "the top-level ability-effects path. One of the two composite-capable paths is not recursing "
        + "into sub-effects (ADR-0004 §6)."
    );
  }

  /// <summary>
  /// The relation, stated once more at whole-card scope over the corpus witness that motivated it:
  /// Academy Manufactor's Clue/Food/Treasure triple sits under a <c>replacement</c>, and its
  /// <b>predefined-token resolution</b> (§9) only fires for tokens whose own <c>createToken</c> port
  /// reached <c>ports</c>. A collapsed replacement body is therefore observable as a MISSING derived
  /// affordance, not just a missing emit — the second-order consequence the asymmetry actually caused.
  /// </summary>
  [Test]
  public void Replacement_composite_children_reach_predefined_token_resolution()
  {
    var walk = new PortWalk(Ontology);
    var ability = new JsonObject
    {
      ["Kind"] = "static",
      ["Effects"] = new JsonArray(
        Paths.Single(p => p.Name == "replacement.Replacement")
          .Wrap(Composite(CreateToken("Clue"), CreateToken("Food"), CreateToken("Treasure")))
      ),
    };
    var labels = walk.Project("SiblingPathWitness", new JsonArray(ability)).Ports.Select(p => p.Label).ToList();

    Assert.That(labels, Has.Some.StartsWith("emit:token"), "the replacement body's tokens must project individually");
    Assert.That(
      labels.Count(l => l.StartsWith("emit:token", StringComparison.Ordinal)),
      Is.EqualTo(3),
      "each of the three replacement-created tokens is its own port — a collapsed composite yields one"
    );
    Assert.That(
      labels,
      Has.None.EqualTo("emit:composite"),
      "a composite Replacement must be unwrapped, never projected as the coarse dead-end emit:composite"
    );
  }

  private static IReadOnlyList<string> Emits(PortWalk walk, JsonNode effect)
  {
    var ability = new JsonObject { ["Kind"] = "static", ["Effects"] = new JsonArray(effect) };
    return walk.Project("SiblingPathProbe", new JsonArray(ability))
      .Ports.Where(p => p.Side == PortSide.Emit)
      .Select(p => p.Label)
      .ToList();
  }
}
