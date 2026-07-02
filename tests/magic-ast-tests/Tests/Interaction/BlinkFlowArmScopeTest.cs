namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Blink (flicker) flow arm — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>; the SEPARATE-arm boundary called out in
/// <c>copy-inheritance-scope.md §2</c>). A <b>blink</b> exiles a permanent then returns the just-exiled
/// card (the linked <c>ExiledWith:Self</c> reference) — the permanent re-enters as a NEW object (CR
/// 603.6e / 400.7): its ETB retriggers AND it re-enters UNTAPPED. The arm projects the
/// exile-then-return composite as one <c>emit:blink</c> (Subject = the blinked filter) and connects it
/// two ways:
/// <list type="bullet">
/// <item><b>blink → etb</b> (refuel an ETB-driven loop): the blinked permanent's Enters-trigger fires
/// again. Felidar Guardian + Restoration Angel — each ETB blinks the OTHER, re-firing its blink ETB.</item>
/// <item><b>blink → tap renewal</b> (the dual of an inherited untap, copy-inheritance Decision 4): a
/// blink that re-enters the tap-gated source untaps it. Kiki copies Felidar/Restoration; the copy's ETB
/// blinks Kiki → Kiki re-enters untapped → its {T} renews.</item>
/// </list>
///
/// <para><b>Every blink in scope is an OPTIONAL ETB ("you may exile…", CR 117.7)</b>, so the
/// <c>emit:blink</c> is <see cref="PortNode.Gated"/> — a loop through it can't be certified infinite
/// (§8) and floors to AMBER. This is the honest, soundness-preserving tier: NOT GREEN (the controller
/// may decline), NOT Red (the loop is structurally feasible). Earning GREEN later would need a
/// non-optional blink, never an engine fudge (adding-a-flow-arm anti-pattern 2).</para>
/// </summary>
[TestFixture]
public class BlinkFlowArmScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortGraph Walk(string set, string file, string card)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      string.IsNullOrEmpty(set) ? "" : set,
      file
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    return new PortWalk(Ontology).Project(card, gold!["Output"]!["Oracle"]!["Abilities"]);
  }

  /// <summary>
  /// LEAD BLINK COMBO (bench 1090-2781, pop 48 050) — the pure two-card blink, no copier. Felidar
  /// Guardian's ETB "you may exile another target permanent you control, then return it" blinks
  /// Restoration Angel; Resto re-enters and its ETB blinks Felidar; Felidar re-enters and re-blinks
  /// Resto — each card's <c>emit:blink</c> refuels the OTHER's Enters-trigger (the blink → etb arm).
  ///
  /// <para>Target: <b>AMBER</b>. Both ETBs are <c>optional</c> ("you may"), so each blink is Gated — the
  /// loop is structurally feasible but cannot be certified to fire forever (§8). Sound AMBER, not GREEN,
  /// not a fudge. (No tap gate here — the floor is the optional ETB, surfaced as Gated.)</para>
  /// </summary>
  [Test]
  public void Felidar_guardian_x_restoration_angel_reconstructs_amber_pure_two_card_blink()
  {
    var graphs = new[]
    {
      Walk("", "FelidarGuardian.json", "Felidar Guardian"),
      Walk("", "RestorationAngel.json", "Restoration Angel"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Felidar", StringComparison.Ordinal))
      && c.Edges.Any(e => e.From.Card.Contains("Restoration", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:blink", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:blink", StringComparison.Ordinal)
      )
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "each card's ETB blink should refuel the other's Enters-trigger (the blink → etb arm)"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "both blinks are optional ('you may') → Gated → the loop floors to a sound AMBER, never GREEN"
    );
  }

  /// <summary>
  /// COPY + BLINK COMBO (bench 618-2781, pop 13 066). Kiki-Jiki copies Felidar Guardian; the copy's ETB
  /// "you may exile another target permanent you control, then return it" blinks Kiki — Kiki re-enters
  /// UNTAPPED (CR 603.6e/400.7), renewing its {T} so it can copy again. Copy-inheritance grafts Felidar's
  /// ports onto the copy; the blink arm's tap-renewal hop (the dual of Corridor Monitor's inherited untap)
  /// closes the loop back to Kiki's tap.
  ///
  /// <para>Target: <b>AMBER</b>. The grafted blink is optional → the renewal is uncertain (the copy may
  /// decline to blink Kiki). Soundly irreducible; the copy genuinely IS recognised (the not-null
  /// assertion), but the optional ETB floors GREEN to AMBER.</para>
  /// </summary>
  [Test]
  public void Kiki_x_felidar_guardian_reconstructs_amber_blink_renews_tap()
  {
    var graphs = new[]
    {
      Walk("", "KikiJikiMirrorBreaker.json", "Kiki-Jiki, Mirror Breaker"),
      Walk("", "FelidarGuardian.json", "Felidar Guardian"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Kiki", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:blink", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:blink", StringComparison.Ordinal)
      )
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "the copy of Felidar should graft its blink and renew Kiki's tap (blink → tap arm)"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "optional inherited blink → uncertain renewal → soundly AMBER, not GREEN"
    );
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD. The blink arm fires ONLY on a real exile-then-return-the-just-exiled action
  /// (the linked <c>ExiledWith:Self</c> reference). A composite that exiles a permanent then returns
  /// something ELSE (a graveyard reanimation, an unrelated exile) is NOT a blink — it must project no
  /// <c>emit:blink</c>, so it can never refuel an ETB or renew a tap. Here a synthesized "exile a
  /// permanent, then return a creature card FROM YOUR GRAVEYARD" composite (Zone:Graveyard, no
  /// ExiledWith link) must yield NO <c>emit:blink</c> port — manufacturing one would be a false combo.
  /// </summary>
  [Test]
  public void Non_blink_exile_then_return_manufactures_no_blink_port_the_false_positive_guard()
  {
    // A composite whose return is a GRAVEYARD reanimation (not the just-exiled card): no ExiledWith link.
    var abilities = new JsonArray(
      new JsonObject
      {
        ["Kind"] = "triggered",
        ["Trigger"] = new JsonObject
        {
          ["Timing"] = "When",
          ["Event"] = "Enters",
          ["Filter"] = new JsonObject { ["CardTypes"] = new JsonArray("creature"), ["IsSelf"] = true },
        },
        ["Effects"] = new JsonArray(
          new JsonObject
          {
            ["EffectType"] = "composite",
            ["Effects"] = new JsonArray(
              new JsonObject
              {
                ["EffectType"] = "exile",
                ["Target"] = new JsonObject
                {
                  ["Kind"] = "Target",
                  ["Filter"] = new JsonObject { ["CardTypes"] = new JsonArray("permanent"), ["Controller"] = "You" },
                },
              },
              new JsonObject
              {
                ["EffectType"] = "returnToBattlefield",
                ["Target"] = new JsonObject
                {
                  ["Kind"] = "Target",
                  ["Filter"] = new JsonObject
                  {
                    ["CardTypes"] = new JsonArray("creature"),
                    ["Zone"] = "Graveyard", // a reanimation — NOT the just-exiled card
                    ["Controller"] = "You",
                  },
                },
              }
            ),
          }
        ),
      }
    );
    var graph = new PortWalk(Ontology).Project("Pseudo Reanimator", abilities);

    Assert.That(
      graph.Ports.Any(p => p.Label.StartsWith("emit:blink", StringComparison.Ordinal)),
      Is.False,
      "an exile + graveyard-reanimation (no ExiledWith:Self link) is NOT a blink — no emit:blink may be manufactured"
    );
  }
}
