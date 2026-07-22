namespace MagicAtlas.Ast.Tests.Flows.DerivedBacklog;

using MagicAST.AST.Abilities;
using MagicAST.AST.Triggers;
using MagicAST.Interaction;
using MagicAST.Schema;

/// <summary>
/// The pure derivation of ADR-0004 §2's backlog:
/// <c>backlog = projected(corpus) − served(rollup ∪ guards) − asserted-unarmable(golds)</c>.
///
/// <para><see cref="Compute"/> is a pure function of its four term-sets, so it can be self-tested with
/// synthetic inputs (an empty subtrahend, an unprojected-with-gold pair, an unaccounted discriminator) with
/// no filesystem involved — the same pure-core split as <c>CrossTrackJoiner</c>. The live term-sets are
/// reflected/loaded by the helpers here (<see cref="AllByDimension"/>, <see cref="ServedByDimension"/>) and
/// by <see cref="BacklogSources"/> (the golds subtrahend).</para>
///
/// <para><b>projected(corpus)</b> is the projectable universe, not the observed one: every discriminator
/// the PortWalk dispatch can turn into a port. This is corpus-INDEPENDENT (it is a function of the AST
/// schema + enums + engine code + golds), which is why the backlog is fully computable on a corpus-less
/// checkout and its size is deterministic. This exactly matches Appendix C's statement that the retired
/// whitelist's names were <c>all discriminators − PortWalkProjection</c> (the backlog before the subtrahend).</para>
/// </summary>
public static class BacklogDerivation
{
  /// <summary>A (dispatch-dimension, discriminator) pair — the backlog's key.</summary>
  public readonly record struct Term(string Dimension, string Discriminator)
  {
    public override string ToString() => $"{Dimension}.{Discriminator}";
  }

  /// <summary>
  /// The four not-a-port-candidate discriminators (ADR-0004 Appendix C): recognition-failure escape hatches
  /// that hold raw text (<c>IUnparsed</c> residue / "Other" catch-alls), denoting a PARSE failure rather
  /// than any Magic concept. They must be EXCLUDED from the interaction backlog or it double-counts parse
  /// debt as interaction debt. This is an architectural scope ruling (Appendix C, ×4), not drift-prone
  /// state: no card can print "unparsed"/"other", so no consume could ever key on the label.
  /// </summary>
  public static readonly IReadOnlySet<Term> NotPortCandidates = new HashSet<Term>
  {
    new("effectType", "unparsed"),
    new("effectType", "unstructured"),
    new("triggerEvent", "Other"),
    new("restriction", "Other"),
  };

  public const string NotPortCandidateReason =
    "recognition-failure escape hatch holding raw text (parse failure, not a Magic concept) — belongs on "
    + "the parse ledger (fidelity ladder / L2), never the interaction backlog (ADR-0004 Appendix C).";

  public sealed record BacklogResult(
    IReadOnlyList<Term> Backlog,
    IReadOnlyList<Term> Decisions,
    IReadOnlyList<Term> Excluded,
    IReadOnlyList<Term> DanglingDecisions,
    IReadOnlyDictionary<string, DimensionCounts> ByDimension
  );

  public readonly record struct DimensionCounts(int All, int Served, int Backlog, int Decisions, int Excluded);

  /// <summary>
  /// The pure formula. For each dimension, <c>unserved = all − served</c>; each unserved discriminator is
  /// classified once: an excluded not-a-port-candidate, a <b>decision</b> (an asserted-unarmable gold names
  /// it), or <b>backlog</b> (no gold — the default under the inverted prior). An asserted-unarmable term
  /// that is NOT unserved (served, or not a discriminator at all) is a <b>dangling decision</b> — a stale or
  /// contradictory gold, surfaced rather than dropped.
  /// </summary>
  public static BacklogResult Compute(
    IReadOnlyDictionary<string, IReadOnlySet<string>> allByDim,
    IReadOnlyDictionary<string, IReadOnlySet<string>> servedByDim,
    IReadOnlySet<Term> assertedUnarmable,
    IReadOnlySet<Term> notPortCandidates
  )
  {
    var backlog = new List<Term>();
    var decisions = new List<Term>();
    var excluded = new List<Term>();
    var counts = new Dictionary<string, DimensionCounts>(StringComparer.Ordinal);

    // Which asserted-unarmable terms actually landed on an unserved discriminator (a real decision)?
    var realizedDecisions = new HashSet<Term>();

    foreach (var (dim, all) in allByDim.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
      var served = servedByDim.GetValueOrDefault(dim, new HashSet<string>(StringComparer.Ordinal));
      var db = 0;
      var dd = 0;
      var de = 0;
      foreach (var d in all.Where(x => !served.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
      {
        var term = new Term(dim, d);
        if (notPortCandidates.Contains(term))
        {
          excluded.Add(term);
          de++;
        }
        else if (assertedUnarmable.Contains(term))
        {
          decisions.Add(term);
          realizedDecisions.Add(term);
          dd++;
        }
        else
        {
          backlog.Add(term);
          db++;
        }
      }
      counts[dim] = new DimensionCounts(all.Count, all.Count(served.Contains), db, dd, de);
    }

    // A gold asserting no_arm over a served discriminator, or over one that is not a discriminator at all.
    var dangling = assertedUnarmable
      .Where(t => !realizedDecisions.Contains(t))
      .OrderBy(t => t.Dimension, StringComparer.Ordinal)
      .ThenBy(t => t.Discriminator, StringComparer.Ordinal)
      .ToList();

    return new BacklogResult(
      Backlog: backlog,
      Decisions: decisions,
      Excluded: excluded,
      DanglingDecisions: dangling,
      ByDimension: counts
    );
  }

  // ── live term-set reflection (the same four dimensions PortWalkExhaustivenessTests dispatches on) ────

  /// <summary><c>projected(corpus)</c>: every discriminator per dispatch dimension, from the AST schema +
  /// enums.</summary>
  public static IReadOnlyDictionary<string, IReadOnlySet<string>> AllByDimension()
  {
    var schema = SchemaExport.Build();
    IReadOnlySet<string> BaseDiscriminators(string key) =>
      schema
        .Bases.Where(b => b.DiscriminatorKey == key)
        .SelectMany(b => b.Types.Select(t => t.Discriminator))
        .ToHashSet(StringComparer.Ordinal);

    var restrictions = Enum.GetNames<ActivationRestriction>()
      .Concat(Enum.GetNames<TriggeredAbilityRestriction>())
      .ToHashSet(StringComparer.Ordinal);

    return new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
    {
      ["effectType"] = BaseDiscriminators("EffectType"),
      ["costType"] = BaseDiscriminators("CostType"),
      ["triggerEvent"] = Enum.GetNames<TriggerEvent>().ToHashSet(StringComparer.Ordinal),
      ["restriction"] = restrictions,
    };
  }

  /// <summary><c>served(rollup ∪ guards)</c>: the semantically-projected discriminators, reflected from
  /// engine code (<see cref="PortWalkProjection"/>) — never a stored list.</summary>
  public static IReadOnlyDictionary<string, IReadOnlySet<string>> ServedByDimension() =>
    new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
    {
      ["effectType"] = PortWalkProjection.EffectTypes,
      ["costType"] = PortWalkProjection.CostTypes,
      ["triggerEvent"] = PortWalkProjection.TriggerEvents,
      ["restriction"] = PortWalkProjection.GatingRestrictions,
    };
}
