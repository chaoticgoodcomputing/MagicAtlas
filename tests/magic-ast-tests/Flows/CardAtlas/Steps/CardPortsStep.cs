using System.Text.Json;
using System.Text.RegularExpressions;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.Shared;

namespace MagicAtlas.Ast.Tests.Flows.CardAtlas.Steps;

/// <summary>
/// D1 — the CardPorts keystone. Over the parse-ready CSB combo-card union, emits the card↔port index
/// (<see cref="CardPortRow"/>: one row per (card, distinct port label), family + emit/consume side) and
/// the per-card deckbuilding metadata (<see cref="CardMetaRow"/>: colour identity + derived mana value +
/// type line). This is the "shape → buildable" bridge both persona reviews found missing: every family
/// station / archetype can now resolve to actual, filterable cards. Metadata comes straight from the
/// committed <c>card-inputs.json</c> (CardInputDTO carries ManaCost / TypeLine / ColorIdentity); price and
/// EDHREC are not in that source (they'd arrive with a fuller Scryfall fetch in atlas-flows). Clean by
/// construction — projects each REAL card's own ports, so none of the materialize's synthetic
/// "copy of X" projection nodes appear.
/// </summary>
[FlowthruStep]
public static partial class CardPortsStep
{
  [GeneratedRegex(@"\{([^}]+)\}")]
  private static partial Regex ManaSymbol();

  public static Func<
    (IEnumerable<Combo> Combos, IEnumerable<MastCardInput> CardInputs),
    (IEnumerable<CardMetaRow>, IEnumerable<CardPortRow>)
  > Create(string ontologyPath) =>
    inputs =>
    {
      var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(ontologyPath))!;
      var walk = new PortWalk(ontology);
      var parser = new OracleParser();

      var byName = new Dictionary<string, CardInputDTO>(StringComparer.Ordinal);
      foreach (var ci in inputs.CardInputs)
        byName.TryAdd(ci.Input.Name, ci.Input);

      var graphCache = new Dictionary<string, PortGraph>(StringComparer.Ordinal);
      PortGraph GraphFor(string name)
      {
        if (graphCache.TryGetValue(name, out var g))
          return g;
        g = byName.TryGetValue(name, out var dto)
          ? CardAtlasShared.Project(name, dto, parser, walk)
          : new PortGraph();
        graphCache[name] = g;
        return g;
      }
      bool ParseReady(string name)
      {
        var g = GraphFor(name);
        return g.Ports.Count > 0
          && !g.Ports.Any(p => p.Label.StartsWith("emit:unparsed", StringComparison.Ordinal));
      }

      var unionNames = inputs
        .Combos.Where(c => c.Cards.All(card => ParseReady(card.Name)))
        .SelectMany(c => c.Cards.Select(card => card.Name))
        .Distinct(StringComparer.Ordinal)
        .Where(byName.ContainsKey)
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();

      var metas = new List<CardMetaRow>(unionNames.Count);
      var ports = new List<CardPortRow>();
      foreach (var name in unionNames)
      {
        var dto = byName[name];
        var portGroups = GraphFor(name)
          .Ports.GroupBy(p => p.Label, StringComparer.Ordinal)
          .OrderBy(g => g.Key, StringComparer.Ordinal)
          .ToList();
        foreach (var g in portGroups)
        {
          var p = g.First();
          ports.Add(new CardPortRow
          {
            Card = name,
            Label = p.Label,
            Family = ResourceFamilies.Of(p.Label),
            Side = p.Label.StartsWith("emit:", StringComparison.Ordinal) ? "emit" : "consume",
            Tier = TierOf(g),
            OracleLineIndex = p.OracleLineIndex,
            Spans = p.SourceSpan is MagicAST.AST.TextSpan s
              ? new[] { new[] { s.Start, s.End } }
              : null,
          });
        }
        metas.Add(new CardMetaRow
        {
          Card = name,
          ColorIdentity = dto.ColorIdentity is { Count: > 0 } ci ? string.Concat(ci) : "",
          Cmc = DeriveCmc(dto.ManaCost),
          TypeLine = dto.TypeLine,
          PortCount = portGroups.Count,
        });
      }

      // ── Deliverable 2: Inferred / Declared statistical backfill ─────────────────────────────────────
      // Cards catalogued in the combo corpus but with NO parsed ports (unparsed / unparseable) still need
      // to be displayable (statistical-backfill-direction; the fidelity ladder's Inferred/Declared tiers).
      // For each such card, infer its resource family from the MODAL family among its parse-ready combo
      // co-stars' ports — tiered Inferred, with a confidence = the fraction of those co-stars sharing that
      // family. A card with no usable co-star signal (no parse-ready co-star projecting a canonical family)
      // is tiered Declared (catalogued only). These rows are ADDITIVE to the parsed index; FamilyRollupStep
      // filters the backfill tiers back out, so they never inflate the realized D2/D3 analytics.
      var coStars = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      foreach (var combo in inputs.Combos)
      {
        var names = combo.Cards.Select(c => c.Name).Distinct(StringComparer.Ordinal).ToList();
        foreach (var a in names)
        {
          if (!coStars.TryGetValue(a, out var set))
            coStars[a] = set = new HashSet<string>(StringComparer.Ordinal);
          foreach (var b in names)
            if (!string.Equals(a, b, StringComparison.Ordinal))
              set.Add(b);
        }
      }

      var canonFamilies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
      HashSet<string> CanonFamiliesOf(string name)
      {
        if (canonFamilies.TryGetValue(name, out var fs))
          return fs;
        fs = GraphFor(name)
          .Ports.Select(p => ResourceFamilies.Of(p.Label))
          .Where(ResourceFamilies.Canonical.Contains)
          .ToHashSet(StringComparer.Ordinal);
        canonFamilies[name] = fs;
        return fs;
      }

      var backfillNames = inputs
        .Combos.SelectMany(c => c.Cards.Select(card => card.Name))
        .Distinct(StringComparer.Ordinal)
        .Where(byName.ContainsKey)
        .Where(n => !ParseReady(n))
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();

      var inferredCount = 0;
      var declaredCount = 0;
      foreach (var name in backfillNames)
      {
        var dto = byName[name];
        var stars = coStars.TryGetValue(name, out var s)
          ? s
          : new HashSet<string>(StringComparer.Ordinal);
        var parseReadyStars = stars.Where(x => byName.ContainsKey(x) && ParseReady(x)).ToList();

        var votes = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var star in parseReadyStars)
          foreach (var fam in CanonFamiliesOf(star))
            votes[fam] = votes.TryGetValue(fam, out var v) ? v + 1 : 1;

        if (parseReadyStars.Count > 0 && votes.Count > 0)
        {
          var modal = votes
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .First();
          ports.Add(new CardPortRow
          {
            Card = name,
            Label = $"inferred:{modal.Key}",
            Family = modal.Key,
            Side = "", // inferred: side is unknown (no parsed emit/consume role)
            Tier = "Inferred",
            Confidence = Math.Round(modal.Value / (double)parseReadyStars.Count, 3),
          });
          inferredCount++;
        }
        else
        {
          ports.Add(new CardPortRow
          {
            Card = name,
            Label = "declared",
            Family = "", // no usable signal — catalogued only
            Side = "",
            Tier = "Declared",
          });
          declaredCount++;
        }
        metas.Add(new CardMetaRow
        {
          Card = name,
          ColorIdentity = dto.ColorIdentity is { Count: > 0 } ci ? string.Concat(ci) : "",
          Cmc = DeriveCmc(dto.ManaCost),
          TypeLine = dto.TypeLine,
          PortCount = 1,
        });
      }

      Console.Error.WriteLine(
        $"[CardPorts] {metas.Count} cards, {ports.Count} card-port rows over the parse-ready combo union"
          + $" (+{inferredCount} inferred, +{declaredCount} declared backfill cards)"
      );
      return (metas, ports);
    };

  /// <summary>Green/Amber tier for a distinct port label (upstream-atlas-data-plan §1.3): GREEN iff at
  /// least one <see cref="PortNode"/> carrying the label fires unconditionally — not
  /// <see cref="PortNode.Gated"/>, not <see cref="PortNode.TapGated"/>, and no
  /// <see cref="PortNode.RequiresCounter"/>; else AMBER (a hard rate limit, a tap gate, a counter-gate,
  /// or an intervening-if makes the mechanism conditional). Grouping by label first means the mechanism is
  /// GREEN when it can fire unconditionally through <em>any</em> of the card's abilities that mint it.</summary>
  private static string TierOf(IEnumerable<PortNode> sameLabel) =>
    sameLabel.Any(p => !p.Gated && !p.TapGated && p.RequiresCounter is null) ? "Green" : "Amber";

  /// <summary>Mana value from a mana-cost string: <c>{3}{G}</c> → 4. Generic <c>{N}</c> adds N; a hybrid /
  /// phyrexian symbol (<c>{2/W}</c>, <c>{W/U}</c>, <c>{W/P}</c>) adds the max of its parts (numeric or 1);
  /// <c>{X}/{Y}/{Z}</c> add 0; any other single symbol (colour / colourless / snow) adds 1.</summary>
  private static int DeriveCmc(string? manaCost)
  {
    if (string.IsNullOrEmpty(manaCost))
      return 0;
    var cmc = 0;
    foreach (Match m in ManaSymbol().Matches(manaCost))
    {
      var s = m.Groups[1].Value;
      if (int.TryParse(s, out var n))
        cmc += n;
      else if (s.Contains('/', StringComparison.Ordinal))
        cmc += s.Split('/').Select(p => int.TryParse(p, out var pn) ? pn : 1).Max();
      else if (s is "X" or "Y" or "Z")
        cmc += 0;
      else
        cmc += 1;
    }
    return cmc;
  }
}
