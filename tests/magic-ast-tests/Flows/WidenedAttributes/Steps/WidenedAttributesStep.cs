namespace MagicAtlas.Ast.Tests.Flows.WidenedAttributes.Steps;

using System.Text.Json;
using System.Text.Json.Nodes;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// ADR-0004 §6 — computes the widened-attribute delta: <c>AST facets − facets the projection consumed</c>,
/// joined to the D1 ports (and their tiers) that are consequently broader than their card.
///
/// <para>Two passes over one ablation, because the narrowing set is corpus-scoped. Pass 1 classifies every
/// attribute site on every card (ablate, re-project, compare) and keeps the DROPPED ones; the CONSUMED ones
/// contribute their names to the narrowing set IFF their ablation shed a label facet, then are discarded.
/// Pass 2 filters the dropped sites to narrowing names. Nothing is declared: which facets carry scope is
/// itself a behavioural fact of this build.</para>
///
/// <para>Scope is the D1 <c>CardPorts</c> card set, so the tier join is total and the report speaks about
/// exactly the ports the atlas publishes. Abilities are re-parsed here rather than read from a dataset —
/// the same idiom <c>CardAtlasShared.Project</c> uses, which keeps the projection this step ablates
/// bit-identical to the one that produced the ports it joins to.</para>
/// </summary>
[FlowthruStep]
public static class WidenedAttributesStep
{
  private const string Note =
    "Widened-attribute report (ADR-0004 §6). Each row is a narrowing FACET the AST carries — a "
    + "controller, an owner, an exclusion, a subtype list — that the PortWalk projection did NOT put on "
    + "the port it produced: deleting the facet from the AST leaves the projected port graph "
    + "bit-identical, so the port names more of the game than the card does. Derived by ablation, which "
    + "transfers to facets exactly because deleting an attribute IS widening it (an absent facet is the "
    + "broadest value). No hand-maintained register: a slice that starts carrying a facet drops those "
    + "rows automatically, and a slice that starts reading a NEW facet enlarges the report's reach with "
    + "no edit. THREE ADJACENT CLASSES, never interchangeable — (1) known-coarse-projections.json names "
    + "DISCRIMINATORS projected coarsely (lost RESOLUTION; hand-authored, gate-enforced); (2) the "
    + "over-approximation report names CONDITION NODES dropped entirely (lost GUARD; derived); (3) this "
    + "report names FACETS dropped from a port that is otherwise right (lost SCOPE; derived). The "
    + "partition between (2) and (3) is structural, not agreed: an attribute site is a subtree containing "
    + "no polymorphic node, and a Condition IS a node, so neither report can ever contain the other's "
    + "rows. FILTER: only facet names the projection demonstrably treats as NARROWING are reported — a "
    + "name qualifies iff ablating it somewhere SHED a label facet (replace:token-creation is a proper "
    + "facet-prefix of replace:token-creation:controlled). A name that narrows nowhere is an axis the "
    + "label grammar does not carry at all, i.e. class (1)'s territory; filtering on mere readership "
    + "instead was measured at 58,306 rows dominated by SourceSpan and OracleLineIndex provenance.";

  public static Func<
    (IEnumerable<CardPortRow> Ports, IEnumerable<MastCardInput> CardInputs),
    WidenedAttributeReport
  > Create(string ontologyPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
      var parser = new OracleParser();

      var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        byName.TryAdd(ci.Input.Name, ci.Input);

      var tier = new Dictionary<(string, string), string>();
      var cardSet = new HashSet<string>(StringComparer.Ordinal);
      foreach (var p in inputs.Ports)
      {
        cardSet.Add(p.Card);
        tier[(p.Card, p.Label)] = p.Tier;
      }
      var cards = cardSet.OrderBy(c => c, StringComparer.Ordinal).ToList();

      // ── Pass 1: ablate everything once. Keep the dropped sites; from the CONSUMED ones keep only the
      // names whose ablation shed a label facet — the derived "this name carries scope" evidence.
      var total = 0;
      var consumed = 0;
      var narrowing = new HashSet<string>(StringComparer.Ordinal);
      var candidates = new List<(string Card, string Text, JsonArray Abilities, List<AttributeConsumption.AttributeVerdict> Dropped)>();

      foreach (var card in cards)
      {
        if (!byName.TryGetValue(card, out var dto))
          continue;
        var text = OracleTextOf(dto);
        if (string.IsNullOrWhiteSpace(text))
          continue;

        var abilities =
          JsonSerializer.SerializeToNode(parser.Parse(text).Output.Abilities, MagicASTJsonOptions.Strict)
          as JsonArray;
        if (abilities is null)
          continue;

        var verdicts = AttributeConsumption.Classify(walk, card, abilities);
        if (verdicts.Count == 0)
          continue;

        total += verdicts.Count;
        foreach (var v in verdicts)
          if (v.Consumed)
          {
            consumed++;
            if (v.Broadened)
              narrowing.Add(v.Site.Name);
          }

        var dropped = AttributeConsumption.OutermostDropped(verdicts).ToList();
        if (dropped.Count > 0)
          candidates.Add((card, text, abilities, dropped));
      }

      // ── Pass 2: the narrowing filter (corpus-scoped, so it cannot run inside pass 1) + the tier join.
      var rows = new List<WidenedAttributeRow>();
      foreach (var (card, text, abilities, dropped) in candidates)
      {
        foreach (var w in AttributeConsumption.Widened(walk, card, abilities, narrowing, dropped))
        {
          var greens = w
            .AffectedPortLabels.Where(l =>
              tier.TryGetValue((card, l), out var t) && string.Equals(t, "Green", StringComparison.Ordinal)
            )
            .ToList();
          rows.Add(
            new WidenedAttributeRow
            {
              Card = card,
              OwnerNode = w.Site.OwnerNode,
              AttributeName = w.Site.Name,
              Path = w.Site.Path,
              ValueJson = w.Site.Json,
              OracleClause = Slice(text, w.AbilitySpan),
              AffectedPorts = w.AffectedPortLabels,
              GreenPorts = greens,
            }
          );
        }
      }

      var byFacet = rows
        .GroupBy(r => (r.OwnerNode, r.AttributeName))
        .Select(g => new WidenedFacetRow
        {
          OwnerNode = g.Key.OwnerNode,
          AttributeName = g.Key.AttributeName,
          WidenedCount = g.Count(),
          CardCount = g.Select(r => r.Card).Distinct(StringComparer.Ordinal).Count(),
          GreenPorts = g.SelectMany(r => r.GreenPorts.Select(l => (r.Card, l))).Distinct().Count(),
          ExampleCard = g.OrderBy(r => r.Card, StringComparer.Ordinal).First().Card,
          ExampleValues = g.Select(r => r.ValueJson).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(4).ToList(),
        })
        .OrderByDescending(r => r.WidenedCount)
        .ThenBy(r => r.OwnerNode, StringComparer.Ordinal)
        .ThenBy(r => r.AttributeName, StringComparer.Ordinal)
        .ToList();

      return new WidenedAttributeReport
      {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        Note = Note,
        CardsScanned = cards.Count,
        AttributeSitesTotal = total,
        AttributeSitesConsumed = consumed,
        NarrowingFacetNames = narrowing.Order(StringComparer.Ordinal).ToList(),
        WidenedCount = rows.Count,
        CardsWithWidenedAttributes = rows.Select(r => r.Card).Distinct(StringComparer.Ordinal).Count(),
        GreenPortsWidened = rows.SelectMany(r => r.GreenPorts.Select(l => (r.Card, l))).Distinct().Count(),
        AmberPortsWidened = rows
          .SelectMany(r =>
            r.AffectedPorts.Where(l =>
                tier.TryGetValue((r.Card, l), out var t) && string.Equals(t, "Amber", StringComparison.Ordinal)
              )
              .Select(l => (r.Card, l))
          )
          .Distinct()
          .Count(),
        ByFacet = byFacet,
        Widened = rows
          .OrderByDescending(r => r.GreenPorts.Count)
          .ThenBy(r => r.Card, StringComparer.Ordinal)
          .ThenBy(r => r.Path, StringComparer.Ordinal)
          .ToList(),
      };
    };

  /// <summary>The card's oracle text, composing DFC faces exactly as <c>CardAtlasShared.Project</c> does
  /// (so the spans this step slices index the same string the parser saw).</summary>
  private static string OracleTextOf(CardInputDTO dto)
  {
    var text = dto.OracleText;
    if (string.IsNullOrWhiteSpace(text) && dto.CardFaces is { Count: > 0 })
      text = string.Join("\n\n", dto.CardFaces.Select(f => f.OracleText ?? "").Where(t => t.Length > 0));
    return text ?? "";
  }

  private static string Slice(string text, int[]? span) =>
    span is [var s, var e] && s >= 0 && e <= text.Length && e > s ? text[s..e] : "";
}
