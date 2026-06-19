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

    foreach (var g in graphs)
      TestContext.Out.WriteLine(
        $"PORTS {g.Ports.FirstOrDefault()?.Card}: "
          + string.Join(", ", g.Ports.Select(p => p.Label))
      );
    foreach (var e in edges)
      TestContext.Out.WriteLine($"EDGE [{e.Tier}] {e.From.Card}:{e.From.Label} → {e.To.Card}:{e.To.Label}");
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

  /// <summary>
  /// PROBE: the Captain Rex Nebula + Brazen Dwarf infinite-dice family (CSB 1410-2141-*). Captain Rex
  /// grants the target a Vehicle with "Crash Land — Whenever this Vehicle deals damage, roll a d6. If the
  /// result equals its mana value, sacrifice it, then it deals that much damage to any target." Replicates
  /// the product path (OracleParser → PortWalk.Project → Materialize → FindCycles) and documents WHAT
  /// reconstructs vs. what each combo still needs.
  ///
  /// <para>Expected today: the engine ROLL ENGINE is Crash Land's own <b>self-loop</b> — the Vehicle's
  /// "it deals that much damage" (a <c>dealDamage</c> emit, source self) re-triggers its own "whenever this
  /// Vehicle deals damage" (the new damage arm), and that trigger drives the roll. The loop is GATED by the
  /// "if the result equals the mana value" conditional → a sound AMBER. Brazen Dwarf attaches as a roll
  /// PAYOFF (the dice arm carries Crash Land's <c>emit:rolldice</c> → Brazen Dwarf's roll trigger), but its
  /// "deals 1 damage to each opponent" does NOT feed back (its self-source damage can't trigger the
  /// Vehicle's self-watching Crash Land — the same-card guard), so the reconstructed CYCLE is the 1-card
  /// Crash Land self-loop. Making the combo a multi-card infinite needs the support cards (Assault Suit's
  /// "can't be sacrificed" to keep the Vehicle alive; a mana-value/result fixer) — documented, not yet
  /// parsed.</para>
  /// </summary>
  [Test]
  public void Captain_rex_brazen_dwarf_reconstruction_status()
  {
    var graphs = new[]
    {
      Walk(
        "Captain Rex Nebula",
        "At the beginning of combat on your turn, choose target nonland permanent you control. Until end of turn, it becomes a Vehicle artifact with base power and toughness each equal to its mana value, and it gains crew 2 and \"Crash Land — Whenever this Vehicle deals damage, roll a six-sided die. If the result is equal to this Vehicle's mana value, sacrifice this Vehicle, then it deals that much damage to any target.\""
      ),
      Walk(
        "Brazen Dwarf",
        "Whenever you roll one or more dice, this creature deals 1 damage to each opponent."
      ),
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(graphs);
    var cycles = engine.FindCycles(edges, PortGraphEngine.DefaultReconstructionReach);

    foreach (var g in graphs)
      TestContext.Out.WriteLine(
        $"PORTS {g.Ports.FirstOrDefault()?.Card}: " + string.Join(", ", g.Ports.Select(p => p.Label))
      );
    foreach (var e in edges.Where(e => e.Provenance == EdgeProvenance.RulesDefined))
      TestContext.Out.WriteLine($"RULES-EDGE [{e.Tier}] {e.From.Card}:{e.From.Label} → {e.To.Card}:{e.To.Label}");

    var damageHop = edges.Any(e =>
      e.From.Label.StartsWith("emit:damage", StringComparison.Ordinal)
      && e.To.Label.StartsWith("trigger:damage", StringComparison.Ordinal)
    );
    var diceHop = edges.Any(e =>
      e.From.Label.StartsWith("emit:rolldice", StringComparison.Ordinal)
      && e.To.Label.StartsWith("trigger:rolldice", StringComparison.Ordinal)
    );
    TestContext.Out.WriteLine($"damage arm hop (Crash Land self-loop closer) present: {damageHop}");
    TestContext.Out.WriteLine($"dice arm hop (Crash Land roll → Brazen Dwarf) present: {diceHop}");
    TestContext.Out.WriteLine($"cycles found: {cycles.Count}");
    foreach (var c in cycles.OrderBy(c => (int)c.Tier).Take(5))
      TestContext.Out.WriteLine(
        $"  [{c.Tier}] {c.Edges.Count} hops, {c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card }).Distinct().Count()} card(s): "
          + string.Join(" → ", c.Edges.Select(e => $"{e.From.Card}:{e.From.Label}"))
      );

    Assert.Pass(
      $"damage-arm hop={damageHop}, dice-arm hop={diceHop}, cycles={cycles.Count}"
        + (cycles.Count > 0 ? $", best tier {cycles.Min(c => c.Tier)}" : "")
    );
  }
}
