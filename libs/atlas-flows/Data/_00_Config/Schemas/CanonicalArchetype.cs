using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// One canonical archetype in the prototype-driven taxonomy. Replaces the deprecated
/// <c>ScryfallTagCanonical</c>: no alias mapping, no Scryfall dependency, no hierarchy.
/// Each archetype is defined by a list of <see cref="Prototypes"/> — concise natural-language
/// clauses that describe the mechanic in oracle-text-aligned phrasing. Each prototype string is
/// embedded with the same encoder as the oracle lines; the mean of an archetype's prototype
/// embeddings is its anchor centroid for line attribution.
/// </summary>
/// <remarks>
/// <para>
/// Prototype authoring guidance:
/// <list type="bullet">
///   <item>Use phrasings that appear verbatim or near-verbatim in oracle text — e.g.
///     <c>"this creature can only be blocked by creatures with flying or reach"</c> rather than
///     <c>"keyword evasion ability that restricts blockers to flyers"</c>.</item>
///   <item>One archetype gets multiple focused clauses (typically 3–10), one per natural-language
///     variant of the mechanic. Don't bundle multiple concepts into a single long string —
///     short focused strings embed more tightly than long blended ones.</item>
///   <item>Avoid card names ("e.g. Lightning Bolt") and meta-commentary ("this is a common
///     mechanic in red") — those pull the embedding toward irrelevant regions.</item>
/// </list>
/// </para>
/// <para>
/// <see cref="Patterns"/> is optional: regex strings that deterministically attribute matching
/// oracle lines to this archetype with confidence=1.0, bypassing the embedding scoring path.
/// Useful for keyword-style lines (<c>"^Flying$"</c>) where the synthetic single-keyword line
/// should never be ambiguous.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record CanonicalArchetype
{
  [SerializedLabel("slug")]
  public required string Slug { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("prototypes")]
  public required List<string> Prototypes { get; init; }

  [SerializedLabel("patterns")]
  public List<string> Patterns { get; init; } = new();
}
