namespace MagicAST.Interaction;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// ADR-0004 §6 — <b>modeled-dependency completeness</b>. The projection (<see cref="PortWalk"/>) is an
/// over-approximation: it projects ports for mechanisms whose <em>enabling conditions</em> it does not
/// model. Gravecrawler's "as long as you control a Zombie" is the canonical case — the
/// <c>alternativeCast</c> effect projects an unconditional <c>emit:returntobattlefield:self</c>, and the
/// <c>Condition</c> node hanging off that effect is read by nothing.
///
/// <para>An accepted over-approximation stays legal, but it must be <b>declared and enumerable</b>: "which
/// GREENs rest on unmodeled conditions" has to be a query, not an act of memory. ADR-0004 §6 states the
/// derivation exactly — <c>AST condition nodes − conditions the projection consumed</c> — and insists it
/// needs <b>no hand-maintained register</b>.</para>
///
/// <para><b>How "consumed" is derived, without instrumenting the walk.</b> A condition node is CONSUMED
/// iff the projection would be different had it been absent. So the derivation is an <b>ablation</b>:
/// delete the condition node from the AST, re-project, and compare port-sets. Identical port-set ⇒ the
/// node contributed nothing ⇒ it was <b>dropped</b> (an unmodeled dependency). Different port-set ⇒ the
/// projection reacted to it (e.g. an <c>InterveningIf</c> raising <see cref="PortNode.Gated"/>) ⇒
/// consumed. This is behavioural, not a list: it cannot drift out of sync with the code, and a future
/// slice that starts reading a condition flips it out of the report automatically.</para>
///
/// <para><b>Relationship to <c>known-coarse-projections.json</c>.</b> That whitelist covers a
/// <em>different</em> blindness: discriminators PortWalk <b>does</b> dispatch on but projects
/// <b>coarsely</b> (an <c>emit:&lt;x&gt;</c> no flow rule reads). Its unit is a discriminator NAME, it is
/// hand-authored, and it is enforced by a gate. This report's unit is a condition NODE INSTANCE on a
/// specific card, it is fully derived, and it is a diagnostic. Adjacent, never interchangeable: a coarse
/// projection loses <em>resolution</em>; a dropped condition loses a <em>guard</em>.</para>
/// </summary>
public static class ConditionConsumption
{
  /// <summary>The AST property that marks a polymorphic <c>Condition</c> node (see
  /// <c>MagicAST.AST.Abilities.Condition</c>'s <c>[PolymorphicBase("ConditionType")]</c>).</summary>
  private const string Discriminator = "ConditionType";

  /// <summary>One condition node found in a card's ability tree.</summary>
  public sealed record ConditionSite
  {
    /// <summary>Document-order ordinal within the card's abilities array (the ablation key).</summary>
    public required int Ordinal { get; init; }

    /// <summary>Index of the enclosing TOP-LEVEL ability (the port-attribution unit).</summary>
    public required int AbilityIndex { get; init; }

    /// <summary>JSON path from the abilities array, e.g. <c>[1].Effects[0].Condition</c>.</summary>
    public required string Path { get; init; }

    /// <summary>The <c>ConditionType</c> discriminator (<c>count</c>, <c>other</c>, …).</summary>
    public required string ConditionType { get; init; }

    /// <summary>The condition node's own JSON (compact) — the clause, as the AST states it.</summary>
    public required string Json { get; init; }
  }

  /// <summary>A condition node the projection DROPPED, with the ports that consequently rest on it.</summary>
  public sealed record DroppedCondition
  {
    public required ConditionSite Site { get; init; }

    /// <summary>The port labels the enclosing ability projects — every one of them is certified without
    /// regard to this condition, so each is an over-approximation resting on it.</summary>
    public required IReadOnlyList<string> AffectedPortLabels { get; init; }

    /// <summary>The enclosing ability's <c>SourceSpan</c> as <c>[start, end)</c>, or <c>null</c>. Lets a
    /// consumer slice the human-readable oracle clause the condition qualifies.</summary>
    public required int[]? AbilitySpan { get; init; }
  }

  // ── Traversal ──────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Every condition node in <paramref name="abilities"/>, in document order. A node is a condition iff
  /// it carries the <c>ConditionType</c> discriminator. Traversal does NOT descend into a condition node
  /// it has already claimed — the OUTERMOST condition is the unit (an <c>AllCondition</c>'s children are
  /// part of the one clause, and ablating the parent ablates them).
  /// </summary>
  public static IReadOnlyList<ConditionSite> Collect(JsonNode? abilities)
  {
    var sites = new List<ConditionSite>();
    if (abilities is not JsonArray arr)
      return sites;

    for (var i = 0; i < arr.Count; i++)
      Descend(arr[i], $"[{i}]", i, sites, remove: null);
    return sites;
  }

  /// <summary>
  /// A deep clone of <paramref name="abilities"/> with the condition node at <paramref name="ordinal"/>
  /// removed — from its parent property, or spliced out of its parent array. The traversal is the exact
  /// one <see cref="Collect"/> runs, so ordinals line up by construction.
  /// </summary>
  public static JsonArray Ablate(JsonArray abilities, int ordinal)
  {
    var clone = (JsonArray)JsonNode.Parse(abilities.ToJsonString())!;
    var sink = new List<ConditionSite>();
    for (var i = 0; i < clone.Count; i++)
      Descend(clone[i], $"[{i}]", i, sink, remove: ordinal);
    return clone;
  }

  /// <summary>Shared traversal. When <paramref name="remove"/> is set, the matching condition node is
  /// detached from its parent instead of merely recorded.</summary>
  private static void Descend(
    JsonNode? node,
    string path,
    int abilityIndex,
    List<ConditionSite> sites,
    int? remove
  )
  {
    switch (node)
    {
      case JsonArray a:
      {
        // Reverse-safe: collect the child list first, so a removal doesn't perturb the walk.
        for (var i = 0; i < a.Count; i++)
        {
          var child = a[i];
          if (IsCondition(child))
          {
            var ord = sites.Count;
            Claim(child!, $"{path}[{i}]", abilityIndex, ord, sites);
            if (remove == ord)
            {
              a.RemoveAt(i);
              return;
            }
            continue;
          }
          Descend(child, $"{path}[{i}]", abilityIndex, sites, remove);
        }
        return;
      }
      case JsonObject o:
      {
        foreach (var key in o.Select(kv => kv.Key).ToList())
        {
          var child = o[key];
          if (IsCondition(child))
          {
            var ord = sites.Count;
            Claim(child!, $"{path}.{key}", abilityIndex, ord, sites);
            if (remove == ord)
            {
              o.Remove(key);
              return;
            }
            continue;
          }
          Descend(child, $"{path}.{key}", abilityIndex, sites, remove);
        }
        return;
      }
      default:
        return;
    }
  }

  private static bool IsCondition(JsonNode? n) => n is JsonObject o && o[Discriminator] is not null;

  private static void Claim(JsonNode node, string path, int abilityIndex, int ordinal, List<ConditionSite> sites) =>
    sites.Add(
      new ConditionSite
      {
        Ordinal = ordinal,
        AbilityIndex = abilityIndex,
        Path = path,
        ConditionType = node[Discriminator]?.ToString() ?? "",
        Json = node.ToJsonString(),
      }
    );

  // ── The delta: AST condition nodes − conditions the projection consumed ────────────────────────

  /// <summary>
  /// The condition nodes <paramref name="walk"/> DROPS for this card — computed by ablation, per the
  /// class remarks. Each carries the port labels of its enclosing ability, which are exactly the ports
  /// certified without regard to it.
  /// </summary>
  public static IReadOnlyList<DroppedCondition> Dropped(
    PortWalk walk,
    string card,
    JsonNode? abilities,
    JsonNode? manaCostSymbols = null
  )
  {
    if (abilities is not JsonArray arr)
      return [];
    var sites = Collect(arr);
    if (sites.Count == 0)
      return [];

    var baseline = Fingerprint(walk.Project(card, arr, manaCostSymbols));
    var dropped = new List<DroppedCondition>();

    foreach (var site in sites)
    {
      var ablated = Ablate(arr, site.Ordinal);
      if (Fingerprint(walk.Project(card, ablated, manaCostSymbols)) != baseline)
        continue; // the projection reacted to this condition — it is CONSUMED, not dropped.

      dropped.Add(
        new DroppedCondition
        {
          Site = site,
          AffectedPortLabels = AbilityPortLabels(walk, card, arr, site.AbilityIndex, manaCostSymbols),
          AbilitySpan = SpanOf(arr[site.AbilityIndex]),
        }
      );
    }
    return dropped;
  }

  /// <summary>The port labels ONE top-level ability projects, obtained by projecting it alone.</summary>
  private static IReadOnlyList<string> AbilityPortLabels(
    PortWalk walk,
    string card,
    JsonArray abilities,
    int abilityIndex,
    JsonNode? manaCostSymbols
  )
  {
    var one = new JsonArray(JsonNode.Parse(abilities[abilityIndex]!.ToJsonString())!);
    return walk.Project(card, one, manaCostSymbols).Ports.Select(p => p.Label).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
  }

  private static int[]? SpanOf(JsonNode? ability) =>
    ability?["SourceSpan"] is JsonObject s
    && s["Start"]?.GetValue<int>() is { } start
    && s["Length"]?.GetValue<int>() is { } len
      ? [start, start + len]
      : null;

  /// <summary>
  /// A total, order-insensitive fingerprint of a projection — every field an ablation could plausibly
  /// move, including the §8 gate flags (an <c>InterveningIf</c>'s ONLY effect is to raise
  /// <see cref="PortNode.Gated"/>, so a fingerprint that omitted it would misreport every intervening-if
  /// as dropped). Deliberately over-inclusive: a false "consumed" is a missed finding, never a false one.
  /// </summary>
  public static string Fingerprint(PortGraph graph)
  {
    var sb = new StringBuilder();
    foreach (var line in graph.Ports.Select(PortFingerprint).Order(StringComparer.Ordinal))
      sb.Append(line).Append('\n');
    foreach (
      var line in graph
        .CardDefinedEdges.Select(e => $"E {e.From.Identity} -> {e.To.Identity}")
        .Order(StringComparer.Ordinal)
    )
      sb.Append(line).Append('\n');
    return sb.ToString();
  }

  private static string PortFingerprint(PortNode p) =>
    string.Join(
      '|',
      p.Identity,
      p.Side.ToString(),
      p.Quantity?.ToString() ?? "~",
      p.Gated ? "G" : "-",
      p.TapGated ? "T" : "-",
      p.RequiresCounter ?? "~",
      p.OracleLineIndex.ToString(),
      p.SourceSpan is { } s ? $"{s.Start}+{s.Length}" : "~",
      p.Subject is null ? "~" : JsonSerializer.Serialize(p.Subject, MagicAST.MagicASTJsonOptions.Strict)
    );
}
