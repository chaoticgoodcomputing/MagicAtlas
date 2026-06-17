using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;

namespace MagicAtlas.Bench;

/// <summary>The reconstruction outcome for one eligible combo.</summary>
public enum ReconstructionOutcome
{
  /// <summary>No cycle over the combo's cards reconstructs the interaction at all.</summary>
  Missed = 0,

  /// <summary>A cycle reconstructs the combo, but the best one the engine certifies is Amber (conditional).</summary>
  Amber = 1,

  /// <summary>A cycle reconstructs the combo at Green (a certified infinite loop).</summary>
  Green = 2,
}

/// <summary>The per-combo result row in the bench report.</summary>
public sealed record ComboResult
{
  public required string Id { get; init; }
  public required int Popularity { get; init; }
  public required IReadOnlyList<string> Cards { get; init; }
  public required ReconstructionOutcome Outcome { get; init; }

  /// <summary>The distinct cards the reconstructing cycle actually spans (empty when missed).</summary>
  public required IReadOnlyList<string> CycleCards { get; init; }
}

/// <summary>The aggregate combo-recall report — the committed bench artifact (initiative 04 §2).</summary>
public sealed record BenchReport
{
  public required int CombosEligible { get; init; }
  public required int ReconstructedGreen { get; init; }
  public required int ReconstructedAmber { get; init; }
  public required int Missed { get; init; }
  public required double RecallAtGreen { get; init; }
  public required double RecallAtAmber { get; init; }

  /// <summary>Per-combo detail, ordered by combo id (deterministic). Not part of the ratchet thresholds.</summary>
  public required IReadOnlyList<ComboResult> Combos { get; init; }
}

/// <summary>
/// The combo-recall benchmark runner (alignment initiative 04, Track A). For each pinned Commander
/// Spellbook combo whose every card has a hand-parsed gold fixture, it runs the EXACT MAST interaction
/// pipeline over precisely that card set —
/// <c>PortWalk.Project</c> (per card) → <c>PortGraphEngine.Materialize</c> → <c>FindCycles</c> — and
/// records whether the engine reconstructs the combo's interaction as a cycle, and at which certainty
/// tier (Green = certified infinite, Amber = conditional, Missed = no spanning cycle).
/// <para>
/// This measures the END PRODUCT: recall against an external, crowd-sourced combo database the agents
/// did not author. A low number is the correct, successful outcome — it is the measurement, not a test
/// to pass. The harness is read-only over the engine; it never mutates it.
/// </para>
/// </summary>
public sealed class ComboRecallRunner
{
  // The canonical reconstruction reach — single source of truth in PortGraphEngine. The flow's
  // MaterializeCyclesStep references the same constant, so the bench measures exactly the reach the
  // product viz shows (the 6-hop cast-recursion blink loop is the longest real cycle today).
  private const int LengthBound = PortGraphEngine.DefaultReconstructionReach;

  private readonly GoldCorpus _corpus;
  private readonly PortWalk _walk;
  private readonly PortGraphEngine _engine;

  public ComboRecallRunner(GoldCorpus corpus, TypeOntology ontology)
  {
    _corpus = corpus;
    _walk = new PortWalk(ontology);
    _engine = new PortGraphEngine(ontology);
  }

  public static ComboRecallRunner Create(string fixturesRoot, string ontologyPath)
  {
    var ontology =
      JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))
      ?? throw new InvalidOperationException($"Could not parse ontology at {ontologyPath}");
    return new ComboRecallRunner(GoldCorpus.Load(fixturesRoot), ontology);
  }

  /// <summary>Run the recall benchmark over every combo in the snapshot. Combos whose cards aren't all
  /// in the gold corpus are skipped (the snapshot is already scoped, but this is the safety belt).</summary>
  public BenchReport Run(ComboSnapshot snapshot)
  {
    var results = new List<ComboResult>();

    foreach (var combo in snapshot.Combos.OrderBy(c => c.Id, StringComparer.Ordinal))
    {
      var cardNames = combo.Cards.Select(c => c.Name).Distinct(StringComparer.Ordinal).ToList();
      if (cardNames.Count < 2 || !cardNames.All(_corpus.Contains))
        continue; // not eligible against the gold corpus

      results.Add(Evaluate(combo, cardNames));
    }

    var green = results.Count(r => r.Outcome == ReconstructionOutcome.Green);
    var amber = results.Count(r => r.Outcome == ReconstructionOutcome.Amber);
    var missed = results.Count(r => r.Outcome == ReconstructionOutcome.Missed);
    var eligible = results.Count;

    return new BenchReport
    {
      CombosEligible = eligible,
      ReconstructedGreen = green,
      ReconstructedAmber = amber,
      Missed = missed,
      RecallAtGreen = Recall(green, eligible),
      RecallAtAmber = Recall(green + amber, eligible),
      Combos = [.. results.OrderBy(r => r.Id, StringComparer.Ordinal)],
    };
  }

  private ComboResult Evaluate(SnapshotCombo combo, IReadOnlyList<string> cardNames)
  {
    var comboCardSet = cardNames.ToHashSet(StringComparer.Ordinal);

    // Walk each combo card's gold AST into its port graph, then materialize + find cycles over EXACTLY
    // this card set — no corpus-wide subsidy, no other combo's cards (the spec: "run the engine over
    // exactly that card set").
    var graphs = cardNames
      .Select(name =>
        _walk.Project(name, _corpus.AbilitiesFor(name), _corpus.ManaCostSymbolsFor(name))
      )
      .ToList();
    var cycles = _engine.FindCycles(_engine.Materialize(graphs), LengthBound);

    // A cycle reconstructs THIS combo iff every card it spans belongs to the combo and it spans ≥2 of
    // them (a genuine multi-card interaction, not a 1-card artifact). The combo's tier is the BEST
    // (lowest CertaintyTier) such cycle — Green beats Amber. Missed when no spanning cycle exists.
    ComboResult? best = null;
    foreach (var cycle in cycles)
    {
      // Attribute every cycle PORT to its combo card(s). A port's own Card is the usual case; a copy-token
      // graft (copy-inheritance-scope.md) carries a synthesized copy identity — the copy IS the copier's
      // object AND carries the copied card's abilities (CR 707.2), so it attributes to BOTH its Grafter
      // (the copier) and the card it was CopiedFrom. A cycle reconstructs THIS combo iff every port
      // attributes only to combo cards and the cycle spans ≥2 distinct combo cards (Kiki + Corridor, via
      // the grafted copy bridging them).
      var ports = cycle.Edges.SelectMany(e => new[] { e.From, e.To }).ToList();
      var attributedCards = new HashSet<string>(StringComparer.Ordinal);
      var unattributable = false;
      foreach (var p in ports)
      {
        if (comboCardSet.Contains(p.Card))
          attributedCards.Add(p.Card);
        else if (p.Grafter is not null || p.CopiedFrom is not null)
        {
          if (p.Grafter is { } g && comboCardSet.Contains(g))
            attributedCards.Add(g);
          if (p.CopiedFrom is { } cf && comboCardSet.Contains(cf))
            attributedCards.Add(cf);
          if (
            (p.Grafter is null || !comboCardSet.Contains(p.Grafter))
            && (p.CopiedFrom is null || !comboCardSet.Contains(p.CopiedFrom))
          )
            unattributable = true; // a graft from outside the combo — not this combo's reconstruction
        }
        else
          unattributable = true; // a port on a card not in the combo
      }

      if (unattributable || attributedCards.Count < 2)
        continue;
      var cycleCards = attributedCards.ToList();

      var outcome =
        cycle.Tier == CertaintyTier.Green ? ReconstructionOutcome.Green
        : ReconstructionOutcome.Amber; // Red cycles don't certify a reconstruction

      if (best is null || (int)outcome > (int)best.Outcome)
        best = new ComboResult
        {
          Id = combo.Id,
          Popularity = combo.Popularity,
          Cards = cardNames,
          Outcome = outcome,
          CycleCards = [.. cycleCards.OrderBy(c => c, StringComparer.Ordinal)],
        };

      if (best.Outcome == ReconstructionOutcome.Green)
        break; // can't do better than Green
    }

    return best
      ?? new ComboResult
      {
        Id = combo.Id,
        Popularity = combo.Popularity,
        Cards = cardNames,
        Outcome = ReconstructionOutcome.Missed,
        CycleCards = [],
      };
  }

  private static double Recall(int hit, int eligible) =>
    eligible == 0 ? 0.0 : Math.Round((double)hit / eligible, 4);
}
