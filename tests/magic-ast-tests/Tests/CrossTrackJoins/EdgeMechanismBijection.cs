namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

using System.Text.Json.Nodes;
using MagicAST.Interaction;

/// <summary>
/// The <b>soundness half of ADR-0004 §2's bijection</b>, closed the way the 2026-07-22 owner ruling
/// mandates: <c>structure ↔ structure</c>, never a hand-typed rule id in engine code (issue #34).
///
/// <para><b>What #30 left open, and what this closes.</b> Issue #30 materialized the guard→witness map from
/// the golds' <c>declares</c> and found the leg that does not close from golds alone: <c>impl:"code"</c>
/// names no symbol, and <b>0 of 29 rule ids appear anywhere in <c>libs/**/*.cs</c></b> — so a literal scan
/// of the engine for rule ids is empty, and the rule↔code leg cannot be checked that way. The <b>forbidden</b>
/// fix is a <c>[WitnessedRule("bridge:…")]</c> attribute (it relocates the hand-maintained correspondence
/// into C#). Instead: the engine tags each edge, at formation, with the <b>structural mechanism</b> that
/// formed it (<see cref="PortEdge.Mechanism"/> / <see cref="PortEdge.Arm"/> — the seam), and this join
/// matches those <em>live</em> tags against what the golds structurally declare. The correspondence
/// arm↔rule is never written down; it is <b>observed</b> — the engine forms the edge, the gold at that combo
/// declares a rules-mechanism there, and the two meet on the endpoints' stems.</para>
///
/// <para><b>Soundness (no unwitnessed behavior), operationalized.</b> A live <see cref="EdgeProvenance.RulesDefined"/>
/// edge is engine behavior a gold must witness (a <see cref="EdgeProvenance.CardDefined"/> edge is
/// self-certifying by ADR-0003 §7, so it is exempt). A rules-mechanism is <b>witnessed</b> when either
/// (a) some gold declares a rules-connector edge with the same structural <c>(fromStem, toStem)</c> — the
/// strong, stem-exact match — or (b) it only fires in the live run of golds that declare a rules-connector
/// edge (the co-firing witness, vocabulary-independent). A mechanism forming edges that satisfies
/// <b>neither</b> is an <b>unwitnessed capability</b> — surfaced as a finding and made RED, never papered
/// over.</para>
///
/// <para><b>Pure by construction</b>, like <see cref="CrossTrackJoiner"/>: the live inventory
/// (<see cref="EdgeMechanismInventory"/>) and the gold/rollup facts are computed by the caller and passed
/// in, so the join runs identically over the real committed fixtures and over the synthetic red-experiment
/// inputs that prove it has teeth.</para>
/// </summary>
public static class EdgeMechanismBijection
{
  /// <summary>The one documented divergence between the golds' <em>documentation</em> stem vocabulary and
  /// the engine's live <see cref="PortStructure.Stem"/>: the trigger families the golds spell
  /// <c>*-dealt</c>/<c>*-rolled</c>/<c>*-presence</c>, which the engine keys as <c>damage</c>/<c>dice</c>/<c>combat</c>.
  /// This is a structural alias between two spellings of the same stem — NOT a rule-id table — kept tiny and
  /// explicit so the stem-exact witness can see through it; any divergence NOT covered here surfaces as a
  /// stem gap in the report.</summary>
  private static readonly IReadOnlyDictionary<string, string> StemAlias = new Dictionary<string, string>(StringComparer.Ordinal)
  {
    ["damage-dealt"] = "damage",
    ["dice-rolled"] = "dice",
    ["combat-presence"] = "combat",
  };

  private static string Normalize(string stem) => StemAlias.GetValueOrDefault(stem, stem);

  /// <summary>A rules-connector edge a gold declares: its structural endpoints (engine-normalized), the
  /// coarse connector <see cref="Mechanism"/> the gold assigned, and the gold it came from. The witnessing
  /// side of the join — the rule attribution (if any) rides <see cref="Rule"/>, but the join matches on
  /// STRUCTURE, not on the rule string.</summary>
  public sealed record GoldConnector(string GoldId, string FromStem, string ToStem, string Mechanism, string? Rule);

  /// <summary>A rollup rule with its structural signature (bridges carry <c>from_stem</c>/<c>to_stem</c>;
  /// the operator-layer sections do not) and its gold witnesses (from the <c>.cited</c> twin, ADR-0004 §2).</summary>
  public sealed record RollupRule(string Section, string RuleId, string Status, string? FromStem, string? ToStem, IReadOnlyList<string> Witnesses);

  /// <summary>How a live rules-mechanism is witnessed.</summary>
  public enum WitnessKind
  {
    /// <summary>A gold declares a rules-connector edge with the same <c>(fromStem, toStem)</c> — the strong,
    /// stem-exact structural match.</summary>
    StemExact,

    /// <summary>No stem-exact gold edge, but the mechanism fires only on golds that DO declare rules-connector
    /// edges (the vocabulary-independent co-firing witness); the stem divergence is reported.</summary>
    CoFiringOnly,

    /// <summary>Neither — an unwitnessed capability (a finding, RED).</summary>
    Unwitnessed,
  }

  /// <summary>One live rules-mechanism, classified by how (or whether) the golds witness it.</summary>
  public sealed record MechanismVerdict(EdgeMechanismInventory.LiveMechanism Live, WitnessKind Kind, IReadOnlyList<string> WitnessingGolds)
  {
    public string Describe() => $"{Live.Describe()} ({Kind})";
  }

  /// <summary>One rollup rule, classified by whether the live engine realizes it and whether golds witness it.</summary>
  public sealed record RuleVerdict(RollupRule Rule, bool FlowRealized, bool LiveExercised)
  {
    /// <summary>ADR-0004 §2 soundness: a rule with no witnessing gold is unevidenced. (For the current 29
    /// this is empty — reported as a number, not assumed.)</summary>
    public bool Unwitnessed => Rule.Witnesses.Count == 0;
  }

  /// <summary>The full bijection result. Every list carries its input sizes so an empty-input pass is red.</summary>
  public sealed record Result(
    IReadOnlyList<MechanismVerdict> Mechanisms,
    IReadOnlyList<RuleVerdict> Rules,
    int LiveRulesMechanisms,
    int GoldConnectorCount,
    int RollupRuleCount,
    int SentinelsProjected,
    int EdgesFormed
  )
  {
    public IReadOnlyList<MechanismVerdict> Unwitnessed => [.. Mechanisms.Where(m => m.Kind == WitnessKind.Unwitnessed)];
    public IReadOnlyList<MechanismVerdict> StemExact => [.. Mechanisms.Where(m => m.Kind == WitnessKind.StemExact)];
    public IReadOnlyList<MechanismVerdict> CoFiringOnly => [.. Mechanisms.Where(m => m.Kind == WitnessKind.CoFiringOnly)];

    /// <summary>Rollup rules whose structural signature no live edge realizes — realized elsewhere (a
    /// cycle-layer bridge) or genuinely inert. REPORTED, not gated, because the operator-layer rules
    /// (match_policy / polarity) and the cycle-layer bridges are not <see cref="PortGraphEngine.Materialize"/>
    /// flow edges; the hard soundness gate is the unwitnessed-CAPABILITY set above.</summary>
    public IReadOnlyList<RuleVerdict> NotFlowRealized => [.. Rules.Where(r => !r.FlowRealized)];

    public IReadOnlyList<RuleVerdict> UnwitnessedRules => [.. Rules.Where(r => r.Unwitnessed)];

    /// <summary>Empty inputs ⇒ the join proves nothing (§2/§5's recurring vacuity failure).</summary>
    public bool Vacuous => LiveRulesMechanisms == 0 || GoldConnectorCount == 0 || RollupRuleCount == 0 || SentinelsProjected == 0 || EdgesFormed == 0;
  }

  /// <summary>
  /// The join. For each live RulesDefined mechanism, decide how the golds witness it (stem-exact,
  /// co-firing, or not at all); for each rollup rule, decide whether the live engine realizes/exercises it.
  /// No rule id enters the decision — the mechanisms are keyed on structure and the golds supply structure.
  /// </summary>
  public static Result Join(
    EdgeMechanismInventory.Inventory inventory,
    IReadOnlyList<GoldConnector> goldConnectors,
    IReadOnlyList<RollupRule> rollupRules,
    IReadOnlySet<string> goldsDeclaringRulesEdges,
    IReadOnlySet<string> projectedSentinels
  )
  {
    // The gold-declared rules-connector stem signatures (engine-normalized) — the stem-exact witness set.
    var goldStemPairs = goldConnectors
      .Select(g => (g.FromStem, g.ToStem))
      .ToHashSet();
    var goldFromStems = goldConnectors.Select(g => g.FromStem).ToHashSet(StringComparer.Ordinal);

    var mechanisms = new List<MechanismVerdict>();
    foreach (var live in inventory.Mechanisms.Where(m => m.IsRulesDefined))
    {
      // Stem-exact: a gold declares a rules-connector with these endpoints. When the live to-endpoint has no
      // structure ("(none)" — an intercept / a scalar tap:self), fall back to matching the from-stem alone,
      // since the connector's identity is carried by the emit side there (Modifier, GraftClosing).
      var stemExact = live.ToStem == "(none)"
        ? goldFromStems.Contains(live.FromStem)
        : goldStemPairs.Contains((live.FromStem, live.ToStem));

      var witnessingGolds = goldConnectors
        .Where(g => g.FromStem == live.FromStem && (live.ToStem == "(none)" || g.ToStem == live.ToStem))
        .Select(g => g.GoldId)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

      var coFiring = live.FiringSentinels.Any(goldsDeclaringRulesEdges.Contains);
      var coFiringGolds = live.FiringSentinels.Where(goldsDeclaringRulesEdges.Contains).OrderBy(x => x, StringComparer.Ordinal).ToList();

      var kind = stemExact ? WitnessKind.StemExact : coFiring ? WitnessKind.CoFiringOnly : WitnessKind.Unwitnessed;
      mechanisms.Add(new MechanismVerdict(live, kind, stemExact ? witnessingGolds : coFiringGolds));
    }

    // A live edge's engine-vocab (fromStem,toStem) signatures — the realization side for the rollup bridges.
    var liveRulesStemPairs = inventory.Mechanisms
      .Where(m => m.IsRulesDefined)
      .Select(m => (m.FromStem, m.ToStem))
      .ToHashSet();

    var rules = new List<RuleVerdict>();
    foreach (var rule in rollupRules)
    {
      // A bridge carries a structural signature — realized iff a live rules-edge forms at those stems.
      var flowRealized =
        rule.FromStem is { } fs && rule.ToStem is { } ts
        && (liveRulesStemPairs.Contains((Normalize(fs), Normalize(ts)))
          || liveRulesStemPairs.Any(p => p.FromStem == Normalize(fs) && p.ToStem == "(none)"));

      // Operator-layer rules (match_policy / polarity) and guards carry no stem signature; they are
      // "live-exercised" when ≥1 of their witnessing golds projected and materialized in the live run.
      var liveExercised = rule.Witnesses.Any(projectedSentinels.Contains);

      rules.Add(new RuleVerdict(rule, flowRealized, liveExercised));
    }

    return new Result(
      Mechanisms: [.. mechanisms.OrderBy(m => m.Live.Describe(), StringComparer.Ordinal)],
      Rules: [.. rules.OrderBy(r => r.Rule.Section, StringComparer.Ordinal).ThenBy(r => r.Rule.RuleId, StringComparer.Ordinal)],
      LiveRulesMechanisms: mechanisms.Count,
      GoldConnectorCount: goldConnectors.Count,
      RollupRuleCount: rollupRules.Count,
      SentinelsProjected: inventory.SentinelsProjected,
      EdgesFormed: inventory.EdgesFormed
    );
  }

  // ── loaders (I/O half) ────────────────────────────────────────────────────────────────────────────

  private static readonly string[] RuleSections = ["polarity", "match_policy", "guards", "bridges"];

  /// <summary>Read every gold-declared rules-connector edge (<c>mechanism ≠ card-defined</c>), resolving
  /// its endpoints to engine-normalized stems via the gold's own <c>ports</c> block. Also returns the set of
  /// golds that declare ≥1 such edge (the co-firing witness input).</summary>
  public static (IReadOnlyList<GoldConnector> Connectors, IReadOnlySet<string> GoldsWithRulesEdges) LoadGoldConnectors(string repoRoot)
  {
    var dir = Path.Combine(repoRoot, "tests/magic-ast-tests/Fixtures/Interactions/golds");
    var connectors = new List<GoldConnector>();
    var withRules = new HashSet<string>(StringComparer.Ordinal);
    if (!Directory.Exists(dir))
      return (connectors, withRules);

    foreach (var path in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.Ordinal))
    {
      JsonNode? node;
      try { node = JsonNode.Parse(File.ReadAllText(path)); }
      catch (System.Text.Json.JsonException) { continue; }
      if (node is not JsonObject gold || gold["id"]?.ToString() is not { } goldId)
        continue;

      // port ref ("Card.Pn") → engine-normalized stem, from the gold's own ports block.
      var stemByRef = new Dictionary<string, string>(StringComparer.Ordinal);
      if (gold["ports"] is JsonObject ports)
        foreach (var (card, arr) in ports)
          foreach (var p in arr as JsonArray ?? [])
            if (p is JsonObject po && po["id"]?.ToString() is { } pid && po["stem"]?.ToString() is { } stem)
              stemByRef[$"{card}.{pid}"] = Normalize(stem);

      foreach (var e in gold["edges"] as JsonArray ?? [])
      {
        if (e is not JsonObject edge || edge["mechanism"]?.ToString() is not { } mech || mech == "card-defined")
          continue;
        withRules.Add(goldId);
        var from = edge["from"]?.ToString();
        var to = edge["to"]?.ToString();
        connectors.Add(
          new GoldConnector(
            GoldId: goldId,
            FromStem: from is not null && stemByRef.TryGetValue(from, out var fs) ? fs : "(unresolved)",
            ToStem: to is not null && stemByRef.TryGetValue(to, out var ts) ? ts : "(unresolved)",
            Mechanism: mech,
            Rule: edge["rule"]?.ToString()
          )
        );
      }
    }

    return (connectors, withRules);
  }

  /// <summary>Read the committed rollup's rules with their structural signature (bridges only) and their
  /// witnesses, from the <c>.cited</c> twin.</summary>
  public static IReadOnlyList<RollupRule> LoadRollupRules(string repoRoot)
  {
    var path = Path.Combine(repoRoot, "tests/magic-ast-tests/Fixtures/Interactions/rollup/port-interactions.cited.json");
    var list = new List<RollupRule>();
    if (!File.Exists(path))
      return list;
    var root = JsonNode.Parse(File.ReadAllText(path));
    foreach (var section in RuleSections)
      foreach (var node in root?[section] as JsonArray ?? [])
        if (node is JsonObject rule && rule["id"]?.ToString() is { } id)
          list.Add(
            new RollupRule(
              Section: section,
              RuleId: id,
              Status: rule["status"]?.ToString() ?? "",
              FromStem: rule["from_stem"]?.ToString(),
              ToStem: rule["to_stem"]?.ToString(),
              Witnesses: (rule["witnesses"] as JsonArray)?.Select(w => w?.ToString() ?? "").Where(s => s.Length > 0).ToList() ?? []
            )
          );
    return list;
  }

  /// <summary>The set of sentinel names (= gold ids) that projected and formed ≥1 live edge.</summary>
  public static IReadOnlySet<string> ProjectedSentinels(EdgeMechanismInventory.Inventory inventory) =>
    inventory.Mechanisms.SelectMany(m => m.FiringSentinels).ToHashSet(StringComparer.Ordinal);
}
