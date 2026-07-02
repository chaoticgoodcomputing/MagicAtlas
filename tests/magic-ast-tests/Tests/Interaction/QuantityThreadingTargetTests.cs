namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Initiative 06 — Track B (quantity completion) TARGET tests. These document the desired §8 behavior
/// AFTER the quantity threading lands (see <c>libs/mast-interaction/docs/06-quantity-plan.md</c>):
/// <see cref="PortGraph.Qty"/> and the §8 balance learn the EXISTING quantity nodes — a token-doubler's
/// multiplier reaches <see cref="PortNode.Quantity"/> so the loop's net-positive production certifies
/// GREEN, while an unbounded variable-X loop stays symbolic → AMBER.
///
/// <para>
/// They are <see cref="IgnoreAttribute">Ignored</see> on purpose: this is a SCOPE/measurement pass, so
/// the tests sit in the tree as executable specifications WITHOUT running (the suite stays green; NUnit
/// reports them as skipped, not failed). Remove the <c>[Ignore]</c> when the threading arm is built.
/// </para>
///
/// <para>
/// CORPUS EVIDENCE (06-quantity-plan.md): the only doubling in the gold is a typed
/// <c>ReplacementModifier {Type:"double"}</c> (Doubling Season, Anointed Procession) or a
/// <c>calculated {Operation:"match"}</c> replacement (Chatterfang) — both EXISTING shapes folded into
/// <see cref="EdgeFamily.Modifier"/>. No Product/Sum node is needed; these tests assert the multiplied
/// quantity, not a new node.
/// </para>
/// </summary>
[TestFixture]
public class QuantityThreadingTargetTests
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortNode Consume(string card, string label, ObjectFilter? subj, int? qty = 1) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Consume,
      Subject = subj,
      Quantity = qty,
      Identity = card + "::" + label,
    };

  private static PortNode Emit(string card, string label, ObjectFilter? subj, int? qty = 1) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Emit,
      Subject = subj,
      Quantity = qty,
      Identity = card + "::" + label,
    };

  // TARGET (06): a token-doubler loop certifies GREEN once the doubler's factor reaches the emit
  // quantity. Shape: a "Sacrifice a creature: {C}{C}{C}" outlet (Ashnod's-Altar-like) feeds an ability
  // "{2}: create one creature token". One token per sac is net-zero (one in, one out) → NOT productive
  // → Amber. A token-doubler (Doubling Season's Modifier:{Type:"double"}) intercepts the create and
  // multiplies the emission to TWO tokens per iteration → net +1 creature per loop → productive → Green.
  //
  // The doubling is the EXISTING ReplacementModifier path folded into EdgeFamily.Modifier (no new node):
  // the engine multiplies the intercepted emit's per-iteration Quantity by the modifier factor before
  // the §8 productivity test. Pre-threading, the Modifier edge intercepts but does NOT multiply, so the
  // surplus is invisible and the loop floors to Amber — this test asserts the post-threading GREEN.
  [Test]
  [Ignore("06 — pending quantity threading")]
  public void A_token_doubler_loop_certifies_green_via_a_multiplied_quantity()
  {
    var you = ControllerFilter.You;
    var creatureTok = new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = you };
    var engine = new PortGraphEngine(Ontology);

    // Maker: "{2}: create a creature token" — the base emission is ONE token per activation.
    var makerPay = Consume("Maker", "pay:mana", null, 2);
    var makerTok = Emit("Maker", "emit:token:creature:controlled", creatureTok, 1);
    // Engine: "Sacrifice a creature: add {C}{C}{C}" — the sac consumes the loop's creature, the mana
    // refunds the {2}; with one token per sac this is a self-sustaining net-zero filter (no surplus).
    var altarSac = Consume(
      "Altar",
      "sac:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], Controller = you }
    );
    var altarMana = Emit("Altar", "emit:mana:colorless", null, 3);

    CardDefinedEdge[] makerEdges = [new() { From = makerPay, To = makerTok }];
    CardDefinedEdge[] altarEdges = [new() { From = altarSac, To = altarMana }];

    // The doubler: a replacement that intercepts the token creation and doubles it (Doubling Season /
    // Anointed Procession — ReplacementModifier {Type:"double"}). Post-threading, this multiplies
    // makerTok's per-iteration Quantity 1 → 2, so the loop nets +1 creature each iteration.
    var doublerIntercept = Consume(
      "Doubler",
      "replace:token-creation:controlled",
      new ObjectFilter { IsToken = true, Controller = you }
    );

    var cycles = engine.FindCycles(
      engine.Materialize(
        new[]
        {
          new PortGraph { Ports = [makerPay, makerTok], CardDefinedEdges = makerEdges },
          new PortGraph { Ports = [altarSac, altarMana], CardDefinedEdges = altarEdges },
          new PortGraph { Ports = [doublerIntercept] },
        }
      ),
      maxLength: 6
    );

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card == "Maker") && c.Edges.Any(e => e.From.Card == "Altar")
    );
    Assert.That(loop, Is.Not.Null, "the maker↔altar loop should reconstruct");

    // With the doubler's factor threaded into the emit quantity, the loop nets +1 creature/iteration —
    // a productive, mana-balanced infinite engine → GREEN. (Pre-threading: Amber, the doubler is inert.)
    Assert.That(loop!.Productive, Is.True, "two tokens out for one in is a net-positive engine");
    Assert.That(loop.Balanced, Is.True, "{C}{C}{C} covers the {2} activation");
    Assert.That(loop.Tier, Is.EqualTo(CertaintyTier.Green));
  }

  // TARGET (06): an UNBOUNDED variable-X loop stays AMBER — the quantity threading must NOT resolve a
  // free-choice X to a loop invariant. Shape: an ability "{1}: create X creature tokens" where X is a
  // VariableQuantity (the spec's "X, where X is…"). PortGraph.Qty maps `variable` → null (symbolic),
  // so the §8 balance abstains and the loop floors to Amber — exactly the conservative behavior the
  // plan preserves. This is the control that proves threading certifies only what it can PROVE.
  [Test]
  [Ignore("06 — pending quantity threading")]
  public void An_unbounded_variable_x_loop_stays_amber()
  {
    var you = ControllerFilter.You;
    var creatureTok = new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = you };
    var engine = new PortGraphEngine(Ontology);

    // Maker: "{1}: create X creature tokens" — X is symbolic (a free choice / unbounded variable), so
    // the emit Quantity is null. The threading leaves `variable` symbolic by design.
    var makerPay = Consume("Maker", "pay:mana", null, 1);
    var makerTok = Emit("Maker", "emit:token:creature:controlled", creatureTok, qty: null);
    var altarSac = Consume(
      "Altar",
      "sac:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], Controller = you }
    );
    var altarMana = Emit("Altar", "emit:mana:colorless", null, 3);

    var cycles = engine.FindCycles(
      engine.Materialize(
        new[]
        {
          new PortGraph
          {
            Ports = [makerPay, makerTok],
            CardDefinedEdges = [new() { From = makerPay, To = makerTok }],
          },
          new PortGraph
          {
            Ports = [altarSac, altarMana],
            CardDefinedEdges = [new() { From = altarSac, To = altarMana }],
          },
        }
      ),
      maxLength: 6
    );

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card == "Maker") && c.Edges.Any(e => e.From.Card == "Altar")
    );
    Assert.That(loop, Is.Not.Null, "the maker↔altar loop should reconstruct");

    // A symbolic, unbounded X is never certified: the engine can't prove net-positive production, so it
    // stays AMBER. The threading resolves CountOf/doubler invariants, NOT a free-choice variable.
    Assert.That(loop!.Tier, Is.EqualTo(CertaintyTier.Amber));
  }
}
