namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// PROBE (not a ratchet pin): does the first fully-parse-ready known infinite-dice combo —
/// Storm-Kiln Artist + Reverberate + Pair o' Dice Lost (CSB 4101-5026-5195, "Infinite die rolls /
/// magecraft / storm") — reconstruct now that Pair o' Dice Lost parses? Replicates the product
/// reconstruction path (OracleParser → PortWalk.Project → Materialize → FindCycles) on just the combo's
/// own 3 cards (a tiny tractable graph), and reports the best cycle's tier. Its loop is magecraft +
/// spell-copy (the die rolls are a byproduct), so this exercises those arms, not the dice arm.
/// </summary>
[TestFixture]
public class DiceComboReconstructionProbe
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortGraph Walk(string name, string oracle)
  {
    var abilities = JsonSerializer.SerializeToNode(
      new OracleParser().Parse(oracle).Output.Abilities,
      MagicASTJsonOptions.Strict
    );
    return new PortWalk(Ontology).Project(name, abilities);
  }

  [Test]
  public void Storm_kiln_reverberate_pair_o_dice_lost_reconstruction_status()
  {
    var graphs = new[]
    {
      Walk(
        "Storm-Kiln Artist",
        "This creature gets +1/+0 for each artifact you control.\nMagecraft — Whenever you cast or copy an instant or sorcery spell, create a Treasure token. (It's an artifact with \"{T}, Sacrifice this token: Add one mana of any color.\")"
      ),
      Walk(
        "Reverberate",
        "Copy target instant or sorcery spell. You may choose new targets for the copy."
      ),
      Walk(
        "Pair o' Dice Lost",
        "Roll two six-sided dice. Return any number of cards with total mana value X or less from your graveyard to your hand, where X is the total of those results. Exile Pair o' Dice Lost."
      ),
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(graphs);
    var cycles = engine.FindCycles(edges, PortGraphEngine.DefaultReconstructionReach);

    TestContext.Out.WriteLine($"edges among the 3 cards: {edges.Count}");
    TestContext.Out.WriteLine($"emit:rolldice ports present: {edges.Count(e => e.From.Label.StartsWith("emit:rolldice"))}");
    TestContext.Out.WriteLine($"cycles found: {cycles.Count}");
    foreach (var c in cycles.OrderBy(c => (int)c.Tier).Take(5))
      TestContext.Out.WriteLine(
        $"  [{c.Tier}] {c.Edges.Count} hops: "
          + string.Join(" → ", c.Edges.Select(e => $"{e.From.Card}:{e.From.Label}"))
      );

    // This is a status PROBE — it always passes; the message reports what the engine can see today.
    Assert.Pass(
      cycles.Count == 0
        ? "No cycle yet — the magecraft/spell-copy loop is not closed by current arms (the die rolls are a byproduct of that loop)."
        : $"Reconstructs: {cycles.Count} cycle(s), best tier {cycles.Min(c => c.Tier)}."
    );
  }
}
