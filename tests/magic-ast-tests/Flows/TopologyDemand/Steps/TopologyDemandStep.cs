using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.TopologyDemand.Steps;

/// <summary>
/// Ranks the ADR-3 topology concepts by combo demand (see <see cref="PortTopologyDemand"/>). Reads the
/// committed hermetic topology (the CITED twin — it carries the per-stem <c>witnesses</c> and the
/// supergroup defs), the golds (for each gold's <c>source.popularity</c>), the scaffold (for the
/// stem→supergroup membership of the non-prefixed stems), and the corpus-gated combo-anchor report (may be
/// absent → graceful degrade). Emits a value-ranked overlay: witnessed stems by gold popularity,
/// supergroups by rolled-up corpus. Never throws on a missing corpus; logs a warning and nulls the corpus
/// signals.
///
/// <para><b>ADR-0004 §7 / issue #26.</b> The topology no longer carries a <c>holes{}</c> registry, so the
/// <c>holes</c> section is always empty — as it already was in practice, every declared hole having been
/// witnessed. Restoring a demand-ranked backlog is ADR-0004 §2 / issue #32 work: a hole becomes the set
/// difference (proposed stems − witnessed stems), computed here rather than read from a stored status.
/// Likewise <c>declared_stems</c> is always empty: every stem in the topology is witnessed by
/// construction.</para>
/// </summary>
[FlowthruStep]
public static partial class TopologyDemandStep
{
  private const string GeneratedStamp = "Flows/TopologyDemand";

  private const string HonestNote =
    "Value-ranked demand overlay for the ADR-3 topology (coarse slang overlay, NOT ground truth). "
    + "'witnessed' = Σ gold source.popularity over a stem's witnesses. 'corpus' = Σ combo-anchor "
    + "popularityMass over anchors whose payoffs name the concept — this measures PAYOFF-side demand, so "
    + "it UNDER-COUNTS enabler concepts (cost-modification / restriction-grant emit no combo payoff string) "
    + "and fairly counts payoff concepts (prevention → 'Prevent all damage…', untap → 'Infinite untap…'). "
    + "The real per-stem demand join lands at Migration Stage 4 when the engine emits ADR-3 ports.";

  private const string CorpusUnavailableNote =
    " CORPUS OVERLAY UNAVAILABLE — Data/_08_Reporting/combo-anchor-report.json was absent "
    + "(worktree / no corpus; regenerate via `nx run mast:combo-anchors`). All corpus signals are null; "
    + "holes are ranked by hand priority only; witnessed stems still rank by gold popularity.";

  // Generic function words that would match payoff prose without signalling real demand (matters mostly
  // for the sentence-shaped supergroup defs). Content tokens (slang, stem/hole names) survive.
  private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
  {
    "the", "a", "an", "of", "to", "you", "your", "on", "in", "at", "by", "for", "with", "and", "or",
    "not", "no", "into", "onto", "per", "each", "one", "all", "any", "that", "this", "would", "be",
    "is", "are", "as", "it", "its", "they", "them", "their", "from", "target", "near", "up", "down",
    "may", "can", "then", "when", "whose", "what", "which", "object", "objects", "play", "leaves",
    "enter", "enters", "endpoint", "carries", "spans", "view", "kind",
  };

  [GeneratedRegex("[^a-z0-9]+")]
  private static partial Regex NonAlnum();

  private sealed class Anchor
  {
    public required long Mass { get; init; }
    public required string[][] PayoffWords { get; init; }
    public required string[] Payoffs { get; init; }
  }

  public static Func<IEnumerable<JsonNode>, PortTopologyDemand> Create(
    string topologyCitedPath,
    string scaffoldPath,
    string comboAnchorPath
  ) =>
    golds =>
    {
      // ── gold id → popularity ──
      var popularity = new Dictionary<string, long>(StringComparer.Ordinal);
      foreach (var gn in golds)
      {
        var g = gn!.AsObject();
        var id = g["id"]!.GetValue<string>();
        popularity[id] = g["source"]?["popularity"]?.GetValue<long>() ?? 0L;
      }

      var topology = JsonNode.Parse(File.ReadAllText(topologyCitedPath))!.AsObject();
      var scaffold = JsonNode.Parse(File.ReadAllText(scaffoldPath))!.AsObject();

      // ── corpus anchors (graceful degrade if absent) ──
      List<Anchor>? anchors = null;
      if (File.Exists(comboAnchorPath))
      {
        anchors = new List<Anchor>();
        var report = JsonNode.Parse(File.ReadAllText(comboAnchorPath))!.AsObject();
        foreach (var an in report["topAnchors"] as JsonArray ?? new JsonArray())
        {
          var a = an!.AsObject();
          var payoffs = (a["topPayoffs"] as JsonArray ?? new JsonArray())
            .Select(p => p!.GetValue<string>())
            .ToArray();
          anchors.Add(new Anchor
          {
            Mass = a["popularityMass"]?.GetValue<long>() ?? 0L,
            Payoffs = payoffs,
            PayoffWords = payoffs.Select(Tokenize).ToArray(),
          });
        }
      }
      else
      {
        Console.Error.WriteLine(
          $"[TopologyDemand] WARNING: {comboAnchorPath} absent — emitting priority-only overlay (corpus=null)."
        );
      }
      var corpusAvailable = anchors is not null;

      // Corpus payoff-mass for a concept's match tokens: sum of anchor mass where any payoff names a token.
      (long? Mass, IReadOnlyList<string>? Matched) Corpus(IEnumerable<string> tokens)
      {
        if (anchors is null)
          return (null, null);
        var toks = tokens
          .Select(t => t.ToLowerInvariant())
          .Where(t => t.Length > 0 && !StopWords.Contains(t))
          .ToHashSet(StringComparer.Ordinal);
        long mass = 0;
        var matched = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var anchor in anchors)
        {
          var hit = false;
          for (var i = 0; i < anchor.PayoffWords.Length; i++)
            if (anchor.PayoffWords[i].Any(w => toks.Any(t => WordMatch(t, w))))
            {
              hit = true;
              matched.Add(anchor.Payoffs[i]);
            }
          if (hit)
            mass += anchor.Mass;
        }
        return (mass, matched.Count > 0 ? matched.ToList() : null);
      }

      // ── stem → supergroup membership (prefix, else scaffold's explicit supergroup field) ──
      var supergroupNames = (topology["supergroups"]!.AsObject()).Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
      // Membership for stems whose NAME does not begin with the supergroup name (mana → resources, tap →
      // structure, …). Declared on the surviving `supergroups` scaffold section since #26 deleted
      // `stems_representative`; a gold declares a port's stem, never its supergroup, so this is not
      // witnessable. Matched on the exact stem name, deliberately — no prefix/fuzzy widening.
      var scaffoldStemSupergroup = new Dictionary<string, string>(StringComparer.Ordinal);
      if (scaffold["supergroups"] is JsonObject sgs)
        foreach (var kv in sgs)
          if (!kv.Key.StartsWith('$') && kv.Value is JsonObject so && so["stems"] is JsonArray members)
            foreach (var m in members)
              scaffoldStemSupergroup[m!.GetValue<string>()] = kv.Key;

      string? SupergroupOf(string stem)
      {
        var colon = stem.IndexOf(':');
        var prefix = colon >= 0 ? stem[..colon] : stem;
        if (supergroupNames.Contains(prefix))
          return prefix;
        return scaffoldStemSupergroup.GetValueOrDefault(stem);
      }

      // ── stems ──
      var witnessedStems = new List<DemandEntry>();
      var declaredStems = new List<DemandEntry>();
      var stemCorpusMass = new Dictionary<string, long>(StringComparer.Ordinal);
      var stemMatched = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

      foreach (var kv in topology["stems"]!.AsObject())
      {
        var stem = kv.Key;
        var s = kv.Value!.AsObject();
        var kind = s["kind"]?.GetValue<string>();
        var status = s["status"]?.GetValue<string>() ?? "declared";
        var (corpusMass, matched) = Corpus(Tokenize(stem));
        if (corpusMass is long cm)
          stemCorpusMass[stem] = cm;
        if (matched is not null)
          stemMatched[stem] = matched;

        if (status == "witnessed")
        {
          long witnessed = 0;
          if (s["witnesses"] is JsonArray ws)
            foreach (var w in ws)
              witnessed += popularity.GetValueOrDefault(w!.GetValue<string>(), 0L);
          witnessedStems.Add(new DemandEntry
          {
            Concept = stem,
            Kind = kind,
            Status = status,
            Demand = new DemandCounts { Witnessed = witnessed, Corpus = corpusMass },
            MatchedPayoffs = matched,
          });
        }
        else
        {
          declaredStems.Add(new DemandEntry
          {
            Concept = stem,
            Kind = kind,
            Status = status,
            Demand = new DemandCounts { Witnessed = null, Corpus = corpusMass },
            MatchedPayoffs = matched,
          });
        }
      }

      // ── holes: ADR-0004 §7 removed the topology's holes{} registry (#26). Always empty until #32
      //    recomputes the backlog as (proposed stems − witnessed stems); the section is retained so the
      //    report's shape — and the mast-loop pick surface that reads it — does not change underneath it.
      var holes = new List<DemandEntry>();

      // ── supergroups (union over member-stem tokens ∪ def/name tokens — anchors counted once) ──
      var supergroups = new List<DemandEntry>();
      var memberStems = new Dictionary<string, List<string>>(StringComparer.Ordinal);
      foreach (var kv in topology["stems"]!.AsObject())
        if (SupergroupOf(kv.Key) is string sg)
          (memberStems.TryGetValue(sg, out var lst) ? lst : memberStems[sg] = new()).Add(kv.Key);

      foreach (var kv in topology["supergroups"]!.AsObject())
      {
        var name = kv.Key;
        var sgo = kv.Value!.AsObject();
        var kindView = sgo["kind_view"]?.GetValue<string>();
        var def = sgo["def"]?.GetValue<string>() ?? "";

        var tokens = new List<string>();
        tokens.AddRange(Tokenize(name));
        tokens.AddRange(Tokenize(def));
        foreach (var member in memberStems.GetValueOrDefault(name, new()))
          tokens.AddRange(Tokenize(member));

        var (corpusMass, matched) = Corpus(tokens);
        supergroups.Add(new DemandEntry
        {
          Concept = name,
          Kind = kindView,
          Status = "supergroup",
          Demand = new DemandCounts { Witnessed = null, Corpus = corpusMass },
          MatchedPayoffs = matched,
        });
      }

      // ── rank ──
      witnessedStems = witnessedStems
        .OrderByDescending(e => e.Demand.Witnessed ?? 0)
        .ThenByDescending(e => e.Demand.Corpus ?? 0)
        .ThenBy(e => e.Concept, StringComparer.Ordinal)
        .ToList();
      declaredStems = declaredStems
        .OrderByDescending(e => e.Demand.Corpus ?? 0)
        .ThenBy(e => e.Concept, StringComparer.Ordinal)
        .ToList();
      holes = holes
        .OrderByDescending(e => e.Demand.Corpus ?? 0)
        .ThenBy(e => e.Priority ?? int.MaxValue)
        .ThenBy(e => e.Concept, StringComparer.Ordinal)
        .ToList();
      supergroups = supergroups
        .OrderByDescending(e => e.Demand.Corpus ?? 0)
        .ThenBy(e => e.Concept, StringComparer.Ordinal)
        .ToList();

      Console.Error.WriteLine(
        $"[TopologyDemand] {witnessedStems.Count} witnessed + {declaredStems.Count} declared stems, "
          + $"{holes.Count} holes, {supergroups.Count} supergroups"
          + (corpusAvailable ? $" over {anchors!.Count} combo anchors" : " (corpus unavailable)")
      );

      return new PortTopologyDemand
      {
        Note = HonestNote + (corpusAvailable ? "" : CorpusUnavailableNote),
        Generated = GeneratedStamp,
        CorpusAvailable = corpusAvailable,
        WitnessedStems = witnessedStems,
        DeclaredStems = declaredStems,
        Holes = holes,
        Supergroups = supergroups,
      };
    };

  /// <summary>Word-level match: a payoff word <paramref name="w"/> names token <paramref name="t"/> when
  /// they are equal, or (both ≥4 chars) one is a prefix of the other — so "prevention" names "prevent" and
  /// "creatures" names "creature", but "lock" does not match inside "block".</summary>
  private static bool WordMatch(string t, string w)
  {
    if (w == t)
      return true;
    return t.Length >= 4 && w.Length >= 4 && (w.StartsWith(t, StringComparison.Ordinal) || t.StartsWith(w, StringComparison.Ordinal));
  }

  private static string[] Tokenize(string s) =>
    NonAlnum().Split(s.ToLowerInvariant()).Where(t => t.Length > 0).ToArray();

}
