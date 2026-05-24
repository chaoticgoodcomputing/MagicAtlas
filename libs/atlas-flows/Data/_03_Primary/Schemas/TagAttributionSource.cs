namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Vocabulary of values for <see cref="OracleLineCanonicalAssignment.Source"/>. Kept as
/// <c>const string</c> rather than a C# enum so the underlying schema field stays a plain string
/// for Flowthru's Arrow-backed serialization. Reference these constants in C# code; the
/// Python-side mirror lives at <c>libs/atlas-flows/Flows/TagLabeling/_sources.py</c>.
/// </summary>
/// <remarks>
/// <para>
/// Confidence semantics by source:
/// <list type="bullet">
///   <item><b>Anchor</b> (1.0) — single-line card whose Scryfall tag uniquely identifies the
///     line. Highest analytical trust.</item>
///   <item><b>Pattern</b> (1.0) — deterministic regex match against the line text (Pass 0).
///     Equally strong as anchor.</item>
///   <item><b>ScryfallInference</b> (cosine) — inference restricted to canonicals the line's
///     card was Scryfall-tagged with. Card-tag set acts as a candidate filter.</item>
///   <item><b>EmbeddingInference</b> (cosine) — inference against ALL canonical anchors with a
///     top-K cap; no card-tag restriction. Embedding-driven discovery beyond Scryfall's coverage.</item>
///   <item><b>FallbackAll</b> (0.5) — tagged card had no usable anchor; every line of the card
///     was attributed. Lowest trust.</item>
/// </list>
/// </para>
/// </remarks>
public static class TagAttributionSource
{
  public const string Anchor = "anchor";
  public const string Pattern = "pattern";
  public const string ScryfallInference = "scryfall-inference";
  public const string EmbeddingInference = "embedding-inference";
  public const string FallbackAll = "fallback-all";
}
