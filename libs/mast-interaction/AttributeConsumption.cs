namespace MagicAST.Interaction;

using System.Text.Json.Nodes;
using MagicAST.Schema;

/// <summary>
/// ADR-0004 §6 — <b>widened-attribute detection</b>, the over-approximation class
/// <see cref="ConditionConsumption"/> structurally cannot see.
///
/// <para><b>The class.</b> A <em>widened attribute</em> is a narrowing FACET the AST carries — a
/// controller, an owner, a self-exclusion, a subtype list — that the projection did not put on the port
/// it produced. The port is therefore BROADER than the card. Chatterfang, Squirrel General is the
/// witness: its oracle line is scoped "under your control", and the <c>createToken</c> side keeps the
/// scope (<c>emit:token:creature:squirrel:controlled</c>) while the intercept side dropped it
/// (<c>replace:token-creation</c>) — one clause, two ports, one of them modelling the card as doubling
/// ANYONE's tokens. Nothing in the output distinguishes that port from a correctly-unscoped one, which is
/// precisely why ADR-0003 §7's per-attribute provenance marker was removed rather than completed: an
/// annotation nothing can compel is paperwork, not detection.</para>
///
/// <para><b>Why ablation transfers.</b> #33 derives "consumed" for a condition NODE by deleting it and
/// re-projecting. The same move works verbatim for an attribute, because deleting an attribute is exactly
/// what widening MEANS: an absent facet is the broadest value (<see cref="PortLabel.Scope"/> maps a
/// missing <c>Controller</c> to no scope facet at all). So "the projection ignores this attribute" and
/// "the AST without this attribute projects identically" are the same statement, and the second one is
/// mechanically checkable. The rejected alternative — comparing AST filter facets against projected port
/// label facets — needs a hand-maintained correspondence table from AST property to label segment, which
/// is the drift surface ADR-0004 exists to remove; it would also be blind to facets that ride the port
/// <see cref="PortNode.Subject"/> rather than the label.</para>
///
/// <para><b>What separates an attribute from a node</b> — and therefore this report from #33's — is
/// derived, not declared. The polymorphic node registry is read in-process from
/// <see cref="SchemaExport.Build"/>: an object is a NODE iff it carries a registered discriminator
/// (key, value) pair. Anything whose subtree contains no node is a node-free FACET subtree, i.e. an
/// attribute. Reading the registered VALUES, not just the key names, is load-bearing: <c>Kind</c> is a
/// discriminator key (on <c>Ability</c>), but <c>{"Kind":"You"}</c> is an <c>ObjectReference</c> — a
/// facet — and a key-name-only test would misfile the very attribute Chatterfang turns on. Because
/// <c>Condition</c> nodes are nodes, no condition can ever surface here: the two reports partition the
/// AST by construction rather than by agreement.</para>
///
/// <para><b>The narrowing filter, and why "the projection reads it somewhere" is not enough.</b> Widening
/// is not "a field the projection ignored" — it is "the port applies to MORE of the game than the card
/// does". Most ignored fields are neither: a <c>SourceSpan</c>, an <c>OracleLineIndex</c>, a literal's
/// <c>Value</c> are provenance and magnitude. Filtering on mere readership buries the class in five-figure
/// noise — measured on the 6,921-card union: <b>58,306</b> rows, led by <c>Kind</c> and <c>SourceSpan</c>.</para>
///
/// <para>So the filter is behavioural too. A facet NAME is <em>narrowing</em> iff, somewhere in the corpus,
/// ablating it produced a port set strictly BROADER in the label grammar's own terms — every ablated label
/// a facet-prefix of a baseline label, at least one of them proper. That is exactly what <c>PortLabel</c>'s
/// facet join does to an absent facet (it drops the segment, and ADR-0002 §2 makes the shorter label the
/// broader query), so "this name carries scope" is read off the projection's own behaviour rather than
/// declared. <c>Controller</c> qualifies (<c>replace:token-creation</c> ⊂
/// <c>replace:token-creation:controlled</c>); <c>SourceSpan</c> cannot, because ablating it moves a span
/// field and never shortens a label. See <see cref="NarrowingNames"/>.</para>
///
/// <para>Keying that filter on the NAME rather than on (node, name) is deliberate and load-bearing: a facet
/// dropped on EVERY instance of some node kind is precisely the defect worth finding, and a per-node key
/// would define it out of existence. A controller is a controller wherever it appears — if the projection
/// can carry one anywhere, failing to carry one here is a defect. An axis the projection narrows on nowhere
/// is <c>known-coarse-projections.json</c>'s territory, not this report's.</para>
/// </summary>
public static class AttributeConsumption
{
  /// <summary>The registered polymorphic discriminator (key, value) pairs, reflected from the MagicAST
  /// assembly in-process — the same export the committed <c>ast-schema.json</c> is gated against.</summary>
  private static readonly Lazy<IReadOnlyDictionary<string, HashSet<string>>> NodeDiscriminators =
    new(() =>
      SchemaExport
        .Build()
        .Bases.GroupBy(b => b.DiscriminatorKey, StringComparer.Ordinal)
        .ToDictionary(
          g => g.Key,
          g => g.SelectMany(b => b.Types.Select(t => t.Discriminator)).ToHashSet(StringComparer.Ordinal),
          StringComparer.Ordinal
        )
    );

  /// <summary>One attribute site — a node-free facet subtree hanging off some AST property.</summary>
  public sealed record AttributeSite
  {
    /// <summary>Document-order ordinal within the card's abilities array (the ablation key).</summary>
    public required int Ordinal { get; init; }

    /// <summary>Index of the enclosing TOP-LEVEL ability (the port-attribution unit).</summary>
    public required int AbilityIndex { get; init; }

    /// <summary>JSON path from the abilities array, e.g. <c>[1].Effects[0].Event.Controller</c>.</summary>
    public required string Path { get; init; }

    /// <summary>The property name — the facet's identity across cards (the relevance-set key).</summary>
    public required string Name { get; init; }

    /// <summary>The discriminator value of the nearest enclosing NODE (<c>tokenCreation</c>,
    /// <c>replacement</c>, …), or <c>""</c> at the top. Says WHERE the facet was dropped, which is what
    /// makes a row actionable — <c>Controller</c> is read on an <c>ObjectFilter</c> and dropped on a
    /// <c>tokenCreation</c> event, and only the second is a defect.</summary>
    public required string OwnerNode { get; init; }

    /// <summary>The facet's own JSON (compact) — the value, as the AST states it.</summary>
    public required string Json { get; init; }
  }

  /// <summary>An attribute site plus the projection's behavioural verdict on it.</summary>
  public sealed record AttributeVerdict
  {
    public required AttributeSite Site { get; init; }

    /// <summary><c>true</c> iff ablating the attribute moved the projection — the projection
    /// demonstrably READS it here.</summary>
    public required bool Consumed { get; init; }

    /// <summary><c>true</c> iff ablating the attribute made the port labels strictly BROADER (each
    /// ablated label a facet-prefix of a baseline label, at least one proper) — the evidence that this
    /// facet name carries scope. Only ever <c>true</c> when <see cref="Consumed"/> is.</summary>
    public required bool Broadened { get; init; }
  }

  /// <summary>A widened attribute: a facet the AST carries and the projected ports do not.</summary>
  public sealed record WidenedAttribute
  {
    public required AttributeSite Site { get; init; }

    /// <summary>The port labels the enclosing ability projects — every one of them is certified without
    /// this facet, so each is broader than the card.</summary>
    public required IReadOnlyList<string> AffectedPortLabels { get; init; }

    /// <summary>The enclosing ability's <c>SourceSpan</c> as <c>[start, end)</c>, or <c>null</c>.</summary>
    public required int[]? AbilitySpan { get; init; }
  }

  // ── Traversal ──────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Every attribute site in <paramref name="abilities"/>, in document order. A property is a site iff
  /// its value's subtree contains NO polymorphic node — the derived attribute/node boundary. Traversal
  /// still descends INTO a claimed site, so a compound facet is reported at every granularity
  /// (<c>Event.Controller</c> and <c>Event.Controller.Kind</c>); the report keeps the outermost dropped
  /// one (<see cref="OutermostDropped"/>), mirroring #33's outermost-condition rule.
  /// </summary>
  public static IReadOnlyList<AttributeSite> Collect(JsonNode? abilities)
  {
    var sites = new List<AttributeSite>();
    if (abilities is not JsonArray arr)
      return sites;

    for (var i = 0; i < arr.Count; i++)
      Descend(arr[i], $"[{i}]", i, "", sites, remove: null);
    return sites;
  }

  /// <summary>
  /// A deep clone of <paramref name="abilities"/> with the attribute at <paramref name="ordinal"/>
  /// removed from its parent object. The traversal is the exact one <see cref="Collect"/> runs, so
  /// ordinals line up by construction. Removal — not nulling — is the point: an absent facet is the
  /// broadest value, which is what "widened" means.
  /// </summary>
  public static JsonArray Ablate(JsonArray abilities, int ordinal)
  {
    var clone = (JsonArray)JsonNode.Parse(abilities.ToJsonString())!;
    var sink = new List<AttributeSite>();
    for (var i = 0; i < clone.Count; i++)
      Descend(clone[i], $"[{i}]", i, "", sink, remove: ordinal);
    return clone;
  }

  private static void Descend(
    JsonNode? node,
    string path,
    int abilityIndex,
    string ownerNode,
    List<AttributeSite> sites,
    int? remove
  )
  {
    switch (node)
    {
      case JsonArray a:
      {
        for (var i = 0; i < a.Count; i++)
          Descend(a[i], $"{path}[{i}]", abilityIndex, ownerNode, sites, remove);
        return;
      }
      case JsonObject o:
      {
        var (ownKey, ownValue) = NodeIdentityOf(o);
        var owner = ownValue ?? ownerNode;
        foreach (var key in o.Select(kv => kv.Key).ToList())
        {
          var child = o[key];
          // A node's own discriminator is its IDENTITY, not a facet of it — ablating it asks "what if
          // this were a different kind of node", which is not a widening question.
          if (string.Equals(key, ownKey, StringComparison.Ordinal))
            continue;
          if (child is null || ContainsNode(child))
          {
            Descend(child, $"{path}.{key}", abilityIndex, owner, sites, remove); // structural — recurse
            continue;
          }

          var ord = sites.Count;
          sites.Add(
            new AttributeSite
            {
              Ordinal = ord,
              AbilityIndex = abilityIndex,
              Path = $"{path}.{key}",
              Name = key,
              OwnerNode = owner,
              Json = child.ToJsonString(),
            }
          );
          if (remove == ord)
          {
            o.Remove(key);
            return;
          }
          Descend(child, $"{path}.{key}", abilityIndex, owner, sites, remove); // sub-facets
        }
        return;
      }
      default:
        return;
    }
  }

  /// <summary>The registered discriminator (key, value) this object carries, or <c>(null, null)</c> if it
  /// is not a polymorphic node. BOTH the key and the value must be registered — <c>{"Kind":"You"}</c> is
  /// an <c>ObjectReference</c> facet, not an <c>Ability</c>, and a key-name-only test would misfile it.</summary>
  private static (string? Key, string? Value) NodeIdentityOf(JsonObject o)
  {
    foreach (var (key, values) in NodeDiscriminators.Value)
      if (o[key] is JsonValue v && v.TryGetValue<string>(out var s) && values.Contains(s))
        return (key, s);
    return (null, null);
  }

  private static string? NodeDiscriminatorOf(JsonObject o) => NodeIdentityOf(o).Value;

  /// <summary>Whether a polymorphic node appears anywhere in this subtree (including at its root).</summary>
  private static bool ContainsNode(JsonNode node) =>
    node switch
    {
      JsonObject o => NodeDiscriminatorOf(o) is not null || o.Any(kv => kv.Value is not null && ContainsNode(kv.Value)),
      JsonArray a => a.Any(x => x is not null && ContainsNode(x)),
      _ => false,
    };

  // ── The delta: AST facets − facets the projection consumed ─────────────────────────────────────

  /// <summary>
  /// Every attribute site in this card with the projection's behavioural verdict on it, computed by
  /// ablation. Deserialization failures on ablation (a <c>required</c> property removed) count as
  /// CONSUMED — deliberately over-inclusive, exactly as #33's fingerprint is: a false "consumed" is a
  /// missed finding, never a false one.
  /// </summary>
  public static IReadOnlyList<AttributeVerdict> Classify(
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

    var baseGraph = walk.Project(card, arr, manaCostSymbols);
    var baseline = ConditionConsumption.Fingerprint(baseGraph);
    var baseLabels = baseGraph.Ports.Select(p => p.Label).ToList();
    var verdicts = new List<AttributeVerdict>(sites.Count);

    foreach (var site in sites)
    {
      bool consumed;
      var broadened = false;
      try
      {
        var graph = walk.Project(card, Ablate(arr, site.Ordinal), manaCostSymbols);
        consumed = ConditionConsumption.Fingerprint(graph) != baseline;
        if (consumed)
          broadened = IsStrictlyBroader(graph.Ports.Select(p => p.Label).ToList(), baseLabels);
      }
      catch
      {
        consumed = true; // an unprojectable ablation is not evidence the facet is ignored.
      }
      verdicts.Add(new AttributeVerdict { Site = site, Consumed = consumed, Broadened = broadened });
    }
    return verdicts;
  }

  /// <summary>
  /// Whether <paramref name="ablated"/> is the same port set as <paramref name="baseline"/> but with
  /// facets SHED — a bijection under which every ablated label is a facet-prefix of its baseline label,
  /// at least one properly so. This is "broader" in the label grammar's own terms (ADR-0002 §2: a port
  /// matches every prefix of its leaf, so a shorter label is the wider query).
  /// </summary>
  private static bool IsStrictlyBroader(List<string> ablated, IReadOnlyList<string> baseline)
  {
    if (ablated.Count != baseline.Count || ablated.Count == 0)
      return false;

    var pool = baseline.ToList();
    var sawProper = false;
    foreach (var a in ablated)
    {
      var i = pool.FindIndex(b => IsFacetPrefix(a, b));
      if (i < 0)
        return false;
      if (!string.Equals(a, pool[i], StringComparison.Ordinal))
        sawProper = true;
      pool.RemoveAt(i);
    }
    return sawProper;
  }

  /// <summary>Whether <paramref name="a"/> is <paramref name="b"/> with zero or more trailing
  /// <c>:</c>-facets removed.</summary>
  private static bool IsFacetPrefix(string a, string b) =>
    string.Equals(a, b, StringComparison.Ordinal)
    || (b.Length > a.Length && b[a.Length] == ':' && b.StartsWith(a, StringComparison.Ordinal));

  /// <summary>
  /// The facet NAMES this projection demonstrably treats as NARROWING — any name whose ablation somewhere
  /// shed a label facet. Callers pass the verdicts of the whole corpus (or, in a hermetic gate, of the
  /// fixtures under test). This is the report's filter, derived from the same pass rather than declared:
  /// a name absent here names an axis the projection narrows on nowhere, which is a coarse projection
  /// (<c>known-coarse-projections.json</c>) rather than a widening.
  /// </summary>
  public static IReadOnlySet<string> NarrowingNames(IEnumerable<AttributeVerdict> verdicts) =>
    verdicts.Where(v => v.Broadened).Select(v => v.Site.Name).ToHashSet(StringComparer.Ordinal);

  /// <summary>
  /// The dropped sites of one card, keeping only the OUTERMOST of any nested run — if a whole
  /// <c>Controller</c> facet is dropped, its inner <c>Controller.Kind</c> is the same finding, not a
  /// second one. Mirrors #33's rule that the outermost condition is the unit.
  /// </summary>
  public static IReadOnlyList<AttributeVerdict> OutermostDropped(IEnumerable<AttributeVerdict> verdicts)
  {
    var dropped = verdicts.Where(v => !v.Consumed).OrderBy(v => v.Site.Ordinal).ToList();
    var kept = new List<AttributeVerdict>();
    foreach (var v in dropped)
      if (!kept.Any(k => v.Site.Path.StartsWith(k.Site.Path + ".", StringComparison.Ordinal)))
        kept.Add(v);
    return kept;
  }

  /// <summary>
  /// The widened attributes of one card: outermost dropped sites whose facet name is NARROWING (see
  /// <see cref="NarrowingNames"/>) and whose enclosing ability actually projects ports — a facet on an
  /// ability that projects nothing widens nothing. Joined to the port labels that over-approximate.
  /// </summary>
  public static IReadOnlyList<WidenedAttribute> Widened(
    PortWalk walk,
    string card,
    JsonNode? abilities,
    IReadOnlySet<string> narrowingNames,
    IEnumerable<AttributeVerdict> verdicts,
    JsonNode? manaCostSymbols = null
  )
  {
    if (abilities is not JsonArray arr)
      return [];

    var labels = new Dictionary<int, IReadOnlyList<string>>();
    var widened = new List<WidenedAttribute>();

    foreach (var v in OutermostDropped(verdicts))
    {
      if (!narrowingNames.Contains(v.Site.Name))
        continue;
      if (!labels.TryGetValue(v.Site.AbilityIndex, out var ports))
        labels[v.Site.AbilityIndex] = ports = AbilityPortLabels(walk, card, arr, v.Site.AbilityIndex, manaCostSymbols);
      if (ports.Count == 0)
        continue;

      widened.Add(
        new WidenedAttribute
        {
          Site = v.Site,
          AffectedPortLabels = ports,
          AbilitySpan = SpanOf(arr[v.Site.AbilityIndex]),
        }
      );
    }
    return widened;
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
    return walk
      .Project(card, one, manaCostSymbols)
      .Ports.Select(p => p.Label)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal)
      .ToList();
  }

  private static int[]? SpanOf(JsonNode? ability) =>
    ability?["SourceSpan"] is JsonObject s
    && s["Start"]?.GetValue<int>() is { } start
    && s["Length"]?.GetValue<int>() is { } len
      ? [start, start + len]
      : null;
}
