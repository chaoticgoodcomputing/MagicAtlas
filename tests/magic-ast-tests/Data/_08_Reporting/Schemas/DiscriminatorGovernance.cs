using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Discriminator governance report (alignment initiative 02, ADR-0004 §1 issue #38) — every intra-family
/// NEAR-DUPLICATE discriminator pair, split into the ones a declaration site explains and the ones nobody
/// has ruled on yet.
///
/// <para><b>A report, not a gate.</b> The hard per-family COLLISION check is still a gate
/// (<c>DiscriminatorUniquenessTests</c>) and needs no whitelist, because a genuine duplicate is always a
/// serialization bug. A near-duplicate is a design question — is <c>shuffleIntoLibrary</c> beside
/// <c>shuffle</c> sprawl, or two real concepts? — and the answer is an architectural ruling, which
/// ADR-0004 §1 routes to prose rather than to a data file a gate consumes. The ruling now lives as
/// <c>NearDuplicateOf</c>/<c>Reason</c> on the discriminator attribute at the declaration site, so it
/// cannot outlive the type it explains; <c>discriminator-justifications.json</c> is deleted.</para>
///
/// <para>The report exists because the useful question is not "are there near-duplicates" (there always
/// are, and they are usually fine) but "is this one NEW" — which is exactly the distinction the
/// surviving Reason makes possible.</para>
/// </summary>
[FlowthruSchema]
public partial record DiscriminatorGovernance
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>Discriminators declared across every polymorphic family.</summary>
  [SerializedLabel("discriminators")]
  public int Discriminators { get; init; }

  /// <summary>Intra-family near-duplicate pairs (Levenshtein ≤ 2, or one a prefix-stem of the other).</summary>
  [SerializedLabel("nearDuplicatePairs")]
  public int NearDuplicatePairs { get; init; }

  /// <summary>Pairs no declaration site explains — the report's actual signal.</summary>
  [SerializedLabel("unexplainedPairs")]
  public int UnexplainedPairs { get; init; }

  /// <summary>A declared <c>NearDuplicateOf</c> counterpart that is no longer near (or no longer exists
  /// in the family). The attribute makes a ruling die with its own type; this catches the OTHER side of
  /// the pair going away.</summary>
  [SerializedLabel("deadRulings")]
  public required IReadOnlyList<string> DeadRulings { get; init; }

  /// <summary>A type declaring <c>NearDuplicateOf</c> with no <c>Reason</c> — an unexplained
  /// explanation.</summary>
  [SerializedLabel("rulingsWithoutReason")]
  public required IReadOnlyList<string> RulingsWithoutReason { get; init; }

  /// <summary>Every near-duplicate pair, explained first (with its ruling) then unexplained.</summary>
  [SerializedLabel("pairs")]
  public required IReadOnlyList<NearDuplicatePair> Pairs { get; init; }
}

/// <summary>One intra-family near-duplicate pair and the ruling that explains it, if any.</summary>
[FlowthruSchema]
public partial record NearDuplicatePair
{
  /// <summary>The polymorphic family's discriminator JSON key (e.g. <c>effectType</c>).</summary>
  [SerializedLabel("family")]
  public required string Family { get; init; }

  [SerializedLabel("a")]
  public required string A { get; init; }

  [SerializedLabel("b")]
  public required string B { get; init; }

  /// <summary>Why the two names are near: <c>levenshtein</c> or <c>prefix-stem</c>.</summary>
  [SerializedLabel("nearness")]
  public required string Nearness { get; init; }

  /// <summary>The discriminator whose attribute carries the ruling, or null when nobody has ruled.</summary>
  [SerializedLabel("explainedBy")]
  public string? ExplainedBy { get; init; }

  /// <summary>The verbatim ruling, from the declaring type's <c>Reason</c>.</summary>
  [SerializedLabel("reason")]
  public string? Reason { get; init; }
}
