namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0004 §6 — gates the <b>widened-attribute</b> derivation behind the <c>WidenedAttributes</c>
/// Flowthru report: <c>AST facets − facets the projection consumed</c>, with "consumed" derived by
/// ABLATION rather than declared.
///
/// <para>The report is a report (project convention: diagnostics are Flowthru flows, NUnit is for
/// gates). What is gated here is the machinery and its acceptance witnesses, all hermetic — committed
/// hand-parsed golds. Without this, the report could silently start computing an empty delta and nothing
/// would notice, which is the absence-blindness §6 exists to close.</para>
///
/// <para>Stateless invariants + named witnesses: no count baseline, no ratchet. Every assertion names the
/// card and the facet it is about.</para>
/// </summary>
[TestFixture]
public class AttributeConsumptionTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static JsonArray Abilities(string file)
  {
    var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "HandParsedCards", file);
    return (JsonArray)JsonNode.Parse(File.ReadAllText(path))!["Output"]!["Oracle"]!["Abilities"]!;
  }

  /// <summary>
  /// The <c>Kind</c> collision, stated directly, because the whole detector turns on it.
  /// <c>Kind</c> IS a registered discriminator key (on <c>Ability</c>), so a key-name-only node test
  /// would misfile <c>{"Kind":"You"}</c> — an <c>ObjectReference</c> facet — as a polymorphic node and
  /// never collect it. Reading the registered discriminator VALUES is what keeps it an attribute.
  /// </summary>
  [Test]
  public void An_ObjectReference_is_a_facet_even_though_Kind_is_a_discriminator_key()
  {
    var abilities = Abilities("MH2/Chatterfang.json");
    var sites = AttributeConsumption.Collect(abilities);

    Assert.That(
      sites.Select(s => s.Path),
      Does.Contain("[1].Effects[0].Replacement.Player"),
      "the createToken effect's Player ObjectReference must be COLLECTED as a facet"
    );
    Assert.That(
      sites.Where(s => s.Path.StartsWith("[1].Effects[0].Replacement.Player", StringComparison.Ordinal))
        .Select(s => s.Json),
      Does.Contain("\"You\"").Or.Contains("{\"Kind\":\"You\"}")
    );
    Assert.That(
      sites.Select(s => s.Path),
      Does.Not.Contain("[1].Effects[0].Replacement"),
      "the createToken EFFECT is a polymorphic node, never a facet — that is #33's half of the AST"
    );
  }

  /// <summary>
  /// The partition with #33 is structural, not an agreement between two reports. A <c>Condition</c> is a
  /// polymorphic node, so no condition can ever appear as an attribute site — Gravecrawler's "as long as
  /// you control a Zombie" belongs to <see cref="ConditionConsumptionTest"/> and must be absent here.
  /// Conflating the two classes would let one launder the other.
  /// </summary>
  [Test]
  public void A_condition_node_is_never_an_attribute_site()
  {
    var sites = AttributeConsumption.Collect(Abilities("Gravecrawler.json"));
    Assert.That(
      sites.Select(s => s.Path),
      Has.None.EqualTo("[1].Effects[0].Condition"),
      "conditions are #33's class; they are nodes, so this collector cannot see them"
    );
    Assert.That(
      sites.Any(s => s.Path.StartsWith("[1].Effects[0].Condition.", StringComparison.Ordinal)),
      Is.True,
      "the condition's own INNER facets are still attributes (the node/facet boundary is by node, not by name)"
    );
  }

  /// <summary>
  /// The witness the issue is about, on the family members whose PARSE is faithful. Anointed Procession
  /// and Peregrin Took both print "under your control", both state it on the <c>tokenCreation</c> event,
  /// and the intercept must therefore carry it. CR 614.1: a replacement effect replaces a SPECIFIC event
  /// — here only the event of YOU creating tokens — so an unscoped label models the card as doubling
  /// anyone's tokens, opponents included, which no printed text supports.
  /// </summary>
  [TestCase("AKH/AnointedProcession.json", "Anointed Procession")]
  [TestCase("LTR/PeregrinTook.json", "Peregrin Took")]
  public void A_stated_event_controller_is_carried_by_the_intercept(string file, string card)
  {
    var abilities = Abilities(file);

    var controller = AttributeConsumption
      .Collect(abilities)
      .SingleOrDefault(s => s.Name == "Controller" && s.OwnerNode == "tokenCreation");
    Assert.That(controller, Is.Not.Null, "the tokenCreation event must STATE the printed 'under your control' scope");
    Assert.That(controller!.Json, Does.Contain("You"));

    Assert.That(
      new PortWalk(Ontology).Project(card, abilities).Ports.Select(p => p.Label),
      Does.Contain("replace:token-creation:controlled"),
      "the intercept must carry the controller scope the AST states"
    );
  }

  /// <summary>
  /// The behavioural core, stated directly: the scope facet is CONSUMED, so ablating it moves the
  /// projection. This is what makes "consumed" derived rather than maintained, and it is the property
  /// that keeps the report honest as the projection grows new facet-reading slices — the moment the
  /// projection stops reading the controller, this fails rather than the report quietly emptying.
  /// </summary>
  [Test]
  public void Ablating_a_consumed_facet_moves_the_projection()
  {
    var abilities = Abilities("AKH/AnointedProcession.json");
    var walk = new PortWalk(Ontology);
    const string card = "Anointed Procession";

    var verdicts = AttributeConsumption.Classify(walk, card, abilities);
    var controller = verdicts.Single(v => v.Site.Name == "Controller" && v.Site.OwnerNode == "tokenCreation");
    Assert.That(controller.Consumed, Is.True, "removing the event's controller must widen the intercept label");

    // …and the ablation is exactly the widening it claims to be.
    Assert.That(
      walk.Project(card, AttributeConsumption.Ablate(abilities, controller.Site.Ordinal)).Ports.Select(p => p.Label),
      Does.Contain("replace:token-creation").And.Not.Contain("replace:token-creation:controlled")
    );
  }

  /// <summary>
  /// <b>The Chatterfang gap is a PARSE gap, not a widened attribute</b> — the distinction the issue's
  /// premise inverts, and the reason it cannot appear in the widened-attribute report. Its oracle line
  /// reads "If one or more tokens would be created under your control", but
  /// <c>TokenAugmentationReplacementRule</c> emits a <c>tokenCreation</c> event carrying only
  /// <c>MinimumQuantity</c>, where its four siblings (<c>TokenDoublingReplacementRule</c>,
  /// <c>FoodTokenAugmentationReplacementRule</c>) set <c>Controller = You</c>. A facet the AST never
  /// states cannot be a facet the projection dropped; ablation is blind to it by construction, and
  /// rightly so.
  ///
  /// <para>This is an <b>explicit named whitelist of one</b>, not a baseline: closing the parse gap
  /// (a one-line addition, which re-points <c>Fixtures/HandParsedCards/MH2/Chatterfang.json</c> and is
  /// therefore orchestrator back-prop, not worker work) makes this test fail loudly, and the correct
  /// response is to delete it — the assertions above already cover the fixed state.</para>
  /// </summary>
  [Test]
  public void Chatterfangs_under_your_control_is_an_open_PARSE_gap_not_a_widened_attribute()
  {
    var abilities = Abilities("MH2/Chatterfang.json");

    Assert.That(
      AttributeConsumption.Collect(abilities).Any(s => s.Name == "Controller" && s.OwnerNode == "tokenCreation"),
      Is.False,
      "KNOWN GAP: TokenAugmentationReplacementRule drops the printed 'under your control' from the "
        + "tokenCreation event, unlike its siblings. Fix the RULE (Controller = ObjectReference.You()); "
        + "the projection already carries the facet, so the port is born correct. Then delete this test."
    );
    Assert.That(
      new PortWalk(Ontology).Project("Chatterfang, Squirrel General", abilities).Ports.Select(p => p.Label),
      Does.Contain("replace:token-creation").And.Contain("emit:token:creature:squirrel:controlled"),
      "the two sides of ONE oracle clause currently disagree about whose tokens it is about — the "
        + "create side keeps the scope (CR 111.2), the intercept has none to keep"
    );
  }

  /// <summary>
  /// The other side of the delta, so the derivation cannot pass by reporting everything as dropped: a
  /// facet the projection genuinely ignores must be classified DROPPED. Chatterfang's added Squirrel
  /// token is printed 1/1 green, and the port label carries only its types
  /// (<c>emit:token:creature:squirrel:controlled</c>) — power/toughness/colour are facets no port reads.
  /// </summary>
  [Test]
  public void A_facet_the_projection_ignores_is_dropped()
  {
    var abilities = Abilities("MH2/Chatterfang.json");
    var verdicts = AttributeConsumption.Classify(new PortWalk(Ontology), "Chatterfang, Squirrel General", abilities);

    var power = verdicts.SingleOrDefault(v => v.Site.Path == "[1].Effects[0].Replacement.Token.Power");
    Assert.That(power, Is.Not.Null, "the token's printed power must be collected as a facet");
    Assert.That(power!.Consumed, Is.False, "no port label or subject reads a token's power — it is dropped");
  }

  /// <summary>
  /// The narrowing filter is what keeps the report about WIDENING rather than about every unread field.
  /// A name qualifies only if ablating it somewhere SHED a label facet — <c>Controller</c> does
  /// (<c>replace:token-creation</c> ⊂ <c>replace:token-creation:controlled</c>), while provenance like
  /// <c>SourceSpan</c> and <c>OracleLineIndex</c> moves other fields and can never shorten a label.
  /// Without this, the corpus report is 58,306 rows of noise instead of a burn-down list.
  /// </summary>
  [Test]
  public void Narrowing_is_derived_from_label_shedding_not_from_mere_readership()
  {
    var walk = new PortWalk(Ontology);
    var verdicts = new[]
      {
        "AKH/AnointedProcession.json",
        "LTR/PeregrinTook.json",
        "MH2/Chatterfang.json",
        "Gravecrawler.json",
        "KodamaoftheEastTree.json",
      }
      .SelectMany(f => AttributeConsumption.Classify(walk, f, Abilities(f)))
      .ToList();

    var narrowing = AttributeConsumption.NarrowingNames(verdicts);
    Assert.That(narrowing, Does.Contain("Controller"), "ablating a controller sheds the scope facet — it narrows");
    Assert.That(narrowing, Does.Not.Contain("SourceSpan"), "a span is provenance: consumed, but it never shortens a label");
    Assert.That(narrowing, Does.Not.Contain("OracleLineIndex"), "likewise bookkeeping, not scope");

    // Narrowing implies consumed — a facet that is not read cannot shed anything.
    Assert.That(verdicts.Where(v => v.Broadened).All(v => v.Consumed), Is.True);
  }

  /// <summary>A node's own discriminator is its IDENTITY, not a facet of it. Ablating <c>Kind</c> from a
  /// static ability asks "what if this were a different kind of ability", which is not a widening
  /// question — and left in, it was the single largest source of noise in the corpus report.</summary>
  [Test]
  public void A_nodes_own_discriminator_is_not_an_attribute_site()
  {
    var sites = AttributeConsumption.Collect(Abilities("MH2/Chatterfang.json"));
    Assert.That(sites.Select(s => s.Path), Does.Not.Contain("[1].Kind"));
    Assert.That(sites.Select(s => s.Path), Does.Not.Contain("[1].Effects[0].EffectType"));
    Assert.That(
      sites.Select(s => s.Path),
      Does.Contain("[1].Effects[0].Replacement.Player.Kind"),
      "but Kind on an ObjectReference is a FACET — that object is not a registered node"
    );
  }

  /// <summary>Nested findings collapse to the outermost, mirroring #33's outermost-condition rule: a
  /// dropped compound facet and its dropped inner parts are ONE finding.</summary>
  [Test]
  public void Nested_dropped_facets_collapse_to_the_outermost()
  {
    var abilities = Abilities("MH2/Chatterfang.json");
    var verdicts = AttributeConsumption.Classify(new PortWalk(Ontology), "Chatterfang, Squirrel General", abilities);
    var outermost = AttributeConsumption.OutermostDropped(verdicts).Select(v => v.Site.Path).ToList();

    Assert.That(
      outermost.Any(p => outermost.Any(q => p != q && p.StartsWith(q + ".", StringComparison.Ordinal))),
      Is.False,
      "no kept path may be nested inside another kept path"
    );
  }

  /// <summary>Ablation must never mutate its input — the report re-projects the same AST many times.</summary>
  [Test]
  public void Ablation_does_not_mutate_the_source_ast()
  {
    var abilities = Abilities("MH2/Chatterfang.json");
    var before = abilities.ToJsonString();
    _ = AttributeConsumption.Ablate(abilities, AttributeConsumption.Collect(abilities)[0].Ordinal);
    Assert.That(abilities.ToJsonString(), Is.EqualTo(before));
  }
}
