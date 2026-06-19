using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// DICE-COMBO RECONSTRUCTION REPORT — a DIAGNOSTIC (not a gate; diagnostics live in Flowthru, never the
/// NUnit suite). For every Commander Spellbook combo whose results mention die rolls, reconstructs it over
/// the engine "as if the support cards were parsed" — gold-fixture ASTs where they exist, hand-authored
/// flow-relevant stubs (dice-report-stub-asts.json) for the rest, parsed oracle text otherwise — and
/// records the best dice-producing cycle's tier, its hop count vs. the product reconstruction reach
/// (<c>PortGraphEngine.DefaultReconstructionReach</c> = 10), which cards actually join the cycle, and the
/// per-card AST provenance. A SOFT TEST of the dice + damage + blink + token arms end to end.
///
/// <para>The <see cref="NovelLoops"/> section answers "does the derive-don't-transcribe engine find a
/// dice loop NOT transcribed from CSB?" — every reconstructed dice cycle classified DERIVED (its card set
/// is not a known CSB combo). Emitted by the <c>DiceComboReport</c> flow.</para>
/// </summary>
[FlowthruSchema]
public partial record DiceComboReport
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>CSB combos whose results mention die rolls / dice.</summary>
  [SerializedLabel("totalDiceCombos")]
  public int TotalDiceCombos { get; init; }

  /// <summary>Dice combos for which the engine found a dice-producing cycle (any tier, any reach).</summary>
  [SerializedLabel("reconstructedAny")]
  public int ReconstructedAny { get; init; }

  /// <summary>Dice combos whose best dice cycle fits within the product reconstruction reach (≤ 6 hops).</summary>
  [SerializedLabel("reconstructedWithinReach")]
  public int ReconstructedWithinReach { get; init; }

  [SerializedLabel("productReach")]
  public int ProductReach { get; init; }

  [SerializedLabel("combos")]
  public required IReadOnlyList<DiceComboRow> Combos { get; init; }

  /// <summary>Engine-DERIVED dice cycles (card set is not a known CSB combo) — the novel-loop scan.</summary>
  [SerializedLabel("novelLoops")]
  public required IReadOnlyList<DiceCycleRow> NovelLoops { get; init; }

  /// <summary>ANCHORED efficient-engine candidates (the "string off a loop" model): a small core loop
  /// (e.g. a 2-card infinite-ETB/mana engine) carrying a roll-on-ETB card as a pure OFFSHOOT — the roll
  /// is a secondary effect of an event the core loop spins, not a load-bearing hop. Surfaces dice combos
  /// more efficient than the CSB-listed ones.</summary>
  [SerializedLabel("efficientEngines")]
  public required IReadOnlyList<DiceComboRow> EfficientEngines { get; init; }
}

/// <summary>One CSB dice combo's reconstruction verdict.</summary>
[FlowthruSchema]
public partial record DiceComboRow
{
  [SerializedLabel("id")]
  public required string Id { get; init; }

  [SerializedLabel("cards")]
  public required IReadOnlyList<string> Cards { get; init; }

  [SerializedLabel("results")]
  public required IReadOnlyList<string> Results { get; init; }

  /// <summary>The best dice-producing cycle's tier (Green/Amber/Red), or "none" if none drives a roll.</summary>
  [SerializedLabel("bestDiceCycleTier")]
  public required string BestDiceCycleTier { get; init; }

  /// <summary>Hop count (edges) of the best dice cycle; 0 if none.</summary>
  [SerializedLabel("bestDiceCycleHops")]
  public int BestDiceCycleHops { get; init; }

  /// <summary>True when the best dice cycle fits within the product reconstruction reach (≤ 6 hops).</summary>
  [SerializedLabel("withinProductReach")]
  public bool WithinProductReach { get; init; }

  /// <summary>The distinct cards the best CORE loop actually spans (a 1-card self-loop spans one). The
  /// roll may be ON this loop or hang off it as an offshoot — see <see cref="RollAttachment"/>.</summary>
  [SerializedLabel("cardsInCycle")]
  public required IReadOnlyList<string> CardsInCycle { get; init; }

  /// <summary>How the die roll attaches to the core loop: "on-loop" (a load-bearing rolldice hop on the
  /// ring) or "offshoot via &lt;card&gt; (+N hops)" — a roll-on-ETB/event card riding an event the loop
  /// spins each iteration (the string-off-a-loop model). "none" when no roll is produced.</summary>
  [SerializedLabel("rollAttachment")]
  public required string RollAttachment { get; init; }

  /// <summary>CSB-cross-check of the best dice cycle: verified (cards == this combo), partial (≥2 shared), derived, or none.</summary>
  [SerializedLabel("classification")]
  public required string Classification { get; init; }

  /// <summary>Per-card AST provenance: "&lt;card&gt;=gold|stub|parsed|inert".</summary>
  [SerializedLabel("cardAstSources")]
  public required IReadOnlyList<string> CardAstSources { get; init; }

  /// <summary>The limiting hop's reason (why not GREEN), or the structural note.</summary>
  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>The core loop's ring as "card:label" hops, in order (for auditing the reconstruction).</summary>
  [SerializedLabel("coreRing")]
  public required IReadOnlyList<string> CoreRing { get; init; }
}

/// <summary>A reconstructed dice cycle (used for the novel-loop scan).</summary>
[FlowthruSchema]
public partial record DiceCycleRow
{
  [SerializedLabel("cards")]
  public required IReadOnlyList<string> Cards { get; init; }

  [SerializedLabel("tier")]
  public required string Tier { get; init; }

  [SerializedLabel("hops")]
  public int Hops { get; init; }

  [SerializedLabel("classification")]
  public required string Classification { get; init; }

  [SerializedLabel("comboId")]
  public required string ComboId { get; init; }

  /// <summary>The ring as "card:label" hops, in order.</summary>
  [SerializedLabel("ring")]
  public required IReadOnlyList<string> Ring { get; init; }
}
