namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Die-roll flow arm — ACCEPTANCE PINS (see <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>).
/// A "roll [N] dice" effect (CR 706) emits a die-roll event; "whenever you roll one or more dice"
/// (Brazen Dwarf) consumes it. The arm connects <c>emit:rolldice</c> → <c>trigger:rolldice</c> so a
/// self-feeding roll engine closes (roll → roll-trigger → effect → … → roll). Both the roller and the
/// watched player are the controller ("you roll" watches YOUR rolls), so You↔You overlaps tier GREEN.
///
/// <para>NOTE this pins the ARM (the emit→trigger hop), not a full infinite-dice cycle: the real combos
/// (Captain Rex Nebula × Brazen Dwarf, Storm-Kiln × Pair o' Dice Lost) close the loop through OTHER hops
/// (combat-damage→roll, magecraft-copy) that are separate backlog arms. This test certifies the dice hop
/// the engine could not see at all before — die rolls were coarse-projected inert.</para>
/// </summary>
[TestFixture]
public class DiceFlowArmScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static readonly ObjectFilter Roller = new() { Controller = ControllerFilter.You };

  /// <summary>A "roll a six-sided die" emit feeds a "whenever you roll one or more dice" trigger — the
  /// dice flow arm forms the edge, tiered GREEN on the You↔You roller scope.</summary>
  [Test]
  public void Roll_emit_feeds_roll_trigger_green()
  {
    var rollEmit = new PortNode
    {
      Card = "Roller Card",
      Label = PortLabel.RollDiceEmit(Roller),
      Side = PortSide.Emit,
      Identity = "Roller Card::emit:rolldice",
      Quantity = 1,
      Subject = Roller,
    };
    var rollTrigger = new PortNode
    {
      Card = "Brazen Dwarf",
      Label = PortLabel.RollDiceTrigger(Roller),
      Side = PortSide.Consume,
      Identity = "Brazen Dwarf::trigger:rolldice",
      Subject = Roller,
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize([new PortGraph { Ports = [rollEmit, rollTrigger] }]);

    var armHop = edges.SingleOrDefault(e =>
      e.From.Label.StartsWith("emit:rolldice", StringComparison.Ordinal)
      && e.To.Label.StartsWith("trigger:rolldice", StringComparison.Ordinal)
    );
    Assert.That(
      armHop,
      Is.Not.Null,
      "a die-roll emit must refuel a die-roll trigger (the dice flow arm, CR 706.2)"
    );
    Assert.That(
      armHop!.Tier,
      Is.EqualTo(CertaintyTier.Green),
      "both the roller and the watched player are the controller (You↔You) → the dice hop is GREEN"
    );
  }

  /// <summary>FALSE-POSITIVE GUARD. A non-dice emit (a token) beside the die-roll trigger must NOT
  /// manufacture a rolldice edge — only an actual roll satisfies a roll-trigger.</summary>
  [Test]
  public void Non_roll_emit_feeds_no_roll_trigger()
  {
    var tokenEmit = new PortNode
    {
      Card = "Token Maker",
      Label = "emit:token:creature:soldier:controlled",
      Side = PortSide.Emit,
      Identity = "Token Maker::emit:token",
      Subject = new ObjectFilter { CardTypes = ["creature"], IsToken = true },
    };
    var rollTrigger = new PortNode
    {
      Card = "Brazen Dwarf",
      Label = PortLabel.RollDiceTrigger(Roller),
      Side = PortSide.Consume,
      Identity = "Brazen Dwarf::trigger:rolldice",
      Subject = Roller,
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize([new PortGraph { Ports = [tokenEmit, rollTrigger] }]);

    Assert.That(
      edges.Any(e => e.To.Label.StartsWith("trigger:rolldice", StringComparison.Ordinal)),
      Is.False,
      "a token emit is not a die roll — no edge may feed the roll trigger"
    );
  }
}
