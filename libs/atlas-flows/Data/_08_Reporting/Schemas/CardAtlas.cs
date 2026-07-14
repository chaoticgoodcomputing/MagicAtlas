using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

// ════════════════════════════════════════════════════════════════════════════════════════════════
// The CardAtlas data layer (D1–D4) — the "shape → buildable" bridge the explorer/exploiter reviews
// found missing. Scoped to the parse-ready CSB combo-card union. Diagnostics; never gates.
//
// Promoted verbatim from tests/magic-ast-tests/Data/_08_Reporting/Schemas/CardAtlas.cs so the
// CardAtlas reporting flow can run from this shippable library rather than the test assembly
// (upstream-atlas-data-plan §0/§6 P0). Serialized keys are byte-for-byte identical.
// ════════════════════════════════════════════════════════════════════════════════════════════════

// ── D1 CardPorts — the keystone card↔port index. Two row datasets. ────────────────────────────────

/// <summary>One card's deckbuilding metadata (D1). The axis the reviews found entirely absent — lets a
/// consumer filter/constrain any card list to colour identity + mana value. Price/EDHREC are NOT in the
/// testbed's card-inputs source (they'd arrive with a fuller Scryfall fetch in atlas-flows).</summary>
[FlowthruSchema]
public partial record CardMetaRow
{
  [SerializedLabel("card")]
  public required string Card { get; init; }

  /// <summary>Commander colour identity as concatenated WUBRG letters (e.g. "BG"), "" for colorless.</summary>
  [SerializedLabel("colorIdentity")]
  public required string ColorIdentity { get; init; }

  /// <summary>Mana value, derived from the mana-cost string ({3}{G} → 4; {X} counts 0).</summary>
  [SerializedLabel("cmc")]
  public int Cmc { get; init; }

  [SerializedLabel("typeLine")]
  public required string TypeLine { get; init; }

  /// <summary>Distinct interaction-port labels this card projects (its "interface" richness).</summary>
  [SerializedLabel("portCount")]
  public int PortCount { get; init; }
}

/// <summary>One (card, port) row (D1) — the card↔port index. A card appears once per distinct port label
/// it projects. <see cref="Side"/> is emit (produces the resource) or consume (a cost/trigger that uses
/// it). The family-relative role (outlet / fodder / payoff) is a QUERY over this + the resource graph,
/// not a stored column — it depends on which family you're browsing.</summary>
[FlowthruSchema]
public partial record CardPortRow
{
  [SerializedLabel("card")]
  public required string Card { get; init; }

  [SerializedLabel("label")]
  public required string Label { get; init; }

  [SerializedLabel("family")]
  public required string Family { get; init; }

  /// <summary><c>emit</c> (producer) or <c>consume</c> (cost/trigger).</summary>
  [SerializedLabel("side")]
  public required string Side { get; init; }

  /// <summary>Index of the oracle-text line this port was minted from (§4 provenance).</summary>
  [SerializedLabel("oracleLineIndex")]
  public int OracleLineIndex { get; init; }

  /// <summary>Source spans in the oracle text as half-open <c>[[start,end), …]</c> pairs (§4
  /// provenance); <c>null</c> when the port carries no source span.</summary>
  [SerializedLabel("spans")]
  public int[][]? Spans { get; init; }
}

// ── D4 ComboInstances — per-combo reconstructed loops with named cards, tier, and result. ──────────

/// <summary>A reconstructed combo instance (D4) — one row per (parse-ready CSB combo, distinct
/// family-signature cycle the engine reconstructs from its cards' parsed text). The "shape → buildable"
/// payoff: named cards + certainty tier + what it does. Anchoring on a family (e.g. sacrifice) is a filter
/// on <see cref="FamilySignature"/>. Generalises <c>DiceComboReport</c> beyond the dice family.</summary>
[FlowthruSchema]
public partial record ComboInstanceRow
{
  [SerializedLabel("comboId")]
  public required string ComboId { get; init; }

  /// <summary>The cycle's distinct cards, " + "-joined (the buildable piece list).</summary>
  [SerializedLabel("cards")]
  public required string Cards { get; init; }

  [SerializedLabel("cardCount")]
  public int CardCount { get; init; }

  /// <summary>The sorted distinct canonical families the cycle touches, ", "-joined (the archetype key).</summary>
  [SerializedLabel("familySignature")]
  public required string FamilySignature { get; init; }

  /// <summary>The families in ring order, " → "-joined (the loop's shape).</summary>
  [SerializedLabel("familyRing")]
  public required string FamilyRing { get; init; }

  /// <summary>The engine's cycle-level certainty tier: Green (reliable) / Amber (conditional).</summary>
  [SerializedLabel("tier")]
  public required string Tier { get; init; }

  /// <summary>Whether the loop is firable (no unrenewed gate) — a fast reliability read.</summary>
  [SerializedLabel("firable")]
  public bool Firable { get; init; }

  /// <summary>What the combo produces, from the CSB variant's declared results, "; "-joined.</summary>
  [SerializedLabel("results")]
  public required string Results { get; init; }

  /// <summary>The CSB popularity signal (build-priority).</summary>
  [SerializedLabel("popularity")]
  public int Popularity { get; init; }
}

// ── D2 ResourceGraph — the family "subway map", enriched with realized-combo tiers. ────────────────

/// <summary>The resource "subway map" (D2) — family stations + directed lines, now annotated with how
/// many reconstructed combos (D4) realize each line and the best tier among them. The strategy-layer view
/// for "which engines does my shell plug into." A single object (stations + lines).</summary>
[FlowthruSchema]
public partial record ResourceGraph
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  [SerializedLabel("stations")]
  public required IReadOnlyList<ResourceStation> Stations { get; init; }

  [SerializedLabel("lines")]
  public required IReadOnlyList<ResourceLine> Lines { get; init; }
}

/// <summary>A resource-family station: how many in-scope cards touch it (size) and its distinct labels.</summary>
[FlowthruSchema]
public partial record ResourceStation
{
  [SerializedLabel("family")]
  public required string Family { get; init; }

  [SerializedLabel("cards")]
  public int Cards { get; init; }

  [SerializedLabel("labels")]
  public int Labels { get; init; }
}

/// <summary>A directed line between resource stations, realized by ≥1 reconstructed combo.</summary>
[FlowthruSchema]
public partial record ResourceLine
{
  [SerializedLabel("from")]
  public required string From { get; init; }

  [SerializedLabel("to")]
  public required string To { get; init; }

  /// <summary>Reconstructed combos (D4) whose ring traverses this family hop.</summary>
  [SerializedLabel("realizingCombos")]
  public int RealizingCombos { get; init; }

  /// <summary>The best certainty tier among the realizing combos (Green &gt; Amber), "" if none.</summary>
  [SerializedLabel("bestTier")]
  public required string BestTier { get; init; }

  /// <summary>True iff the reverse line (To→From) is also realized — a bidirectional fundamental engine.</summary>
  [SerializedLabel("engine")]
  public bool Engine { get; init; }
}

// ── D3 ArchetypeCatalog — the realized combo-shape catalog, tier-annotated, untruncated. ───────────

/// <summary>The archetype catalog (D3) — every distinct family-signature that ≥1 reconstructed combo (D4)
/// realizes, with its combo count, best tier, an example piece list, and the produced results. The
/// actionable "what can I build" catalog; ranked by how many known combos realize it. (The full
/// STRUCTURAL catalog of theoretically-possible shapes lives in the PortGraphAtlas report,
/// <c>familyArchetypeCatalog</c>, now untruncated.)</summary>
[FlowthruSchema]
public partial record ArchetypeCatalog
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>Distinct realized family-signatures (archetypes with ≥1 reconstructed combo).</summary>
  [SerializedLabel("realizedArchetypes")]
  public int RealizedArchetypes { get; init; }

  [SerializedLabel("entries")]
  public required IReadOnlyList<ArchetypeEntry> Entries { get; init; }
}

/// <summary>One realized archetype: a family-signature + its reconstructed combos' rollup.</summary>
[FlowthruSchema]
public partial record ArchetypeEntry
{
  [SerializedLabel("families")]
  public required string Families { get; init; }

  [SerializedLabel("familyCount")]
  public int FamilyCount { get; init; }

  /// <summary>Reconstructed combos (D4) with this exact family-signature.</summary>
  [SerializedLabel("realizingCombos")]
  public int RealizingCombos { get; init; }

  /// <summary>Best certainty tier among the realizing combos (Green &gt; Amber).</summary>
  [SerializedLabel("bestTier")]
  public required string BestTier { get; init; }

  /// <summary>Green-tier fraction among realizing combos (reliability at a glance), 0–1.</summary>
  [SerializedLabel("greenFraction")]
  public double GreenFraction { get; init; }

  /// <summary>An example piece list from the most-popular realizing combo.</summary>
  [SerializedLabel("exampleCards")]
  public required string ExampleCards { get; init; }

  /// <summary>The union of the realizing combos' declared results, "; "-joined.</summary>
  [SerializedLabel("results")]
  public required string Results { get; init; }
}
