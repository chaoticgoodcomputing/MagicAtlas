namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.Abilities;
using MagicAST.AST.Triggers;
using MagicAST.Interaction;
using MagicAST.Schema;

/// <summary>
/// Exhaustiveness invariant for <see cref="PortWalk"/> (alignment initiative 03 #1, de-ratcheted
/// 2026-06-16). Every AST discriminator that PortWalk dispatches on — every <c>EffectType</c>,
/// <c>CostType</c>, trigger <c>Event</c>, and restriction kind — must be EITHER semantically projected
/// (declared in <see cref="PortWalkProjection"/>) OR carry an EXPLICIT, justified entry in the named
/// whitelist <c>libs/mast-interaction/known-coarse-projections.json</c>.
///
/// Why: a discriminator with no semantic projection falls through to a coarse totality label
/// (<c>emit:&lt;x&gt;</c> / <c>pay:&lt;x&gt;</c> / a coarse trigger role) that no flow rule reads — the
/// port exists but yields zero recall. Every TDD batch that adds an effect type silently degrades
/// interactions while the parser tests stay green. This invariant converts that drift into a failing
/// test: a new discriminator that is neither projected NOR whitelisted fails here, forcing a conscious
/// projection decision. The whitelist is an EXPLICIT NAMED LIST of the discriminators permitted to be
/// coarse, each justified by a reason — not a count and not a moving baseline (same stateless-invariant
/// + explicit-named-carve-out shape as the gold free-text whitelist). A whitelisted discriminator that
/// later becomes projected, or one that no longer exists, fails until removed — the list stays honest by
/// being a list of NAMES.
///
/// The whitelist size is the interaction layer's known-blind-spot metric — see
/// docs/scratch/alignment-session/03-portwalk-exhaustiveness-findings.md.
/// </summary>
[TestFixture]
public class PortWalkExhaustivenessTests
{
  private const string WhitelistRelPath = "libs/mast-interaction/known-coarse-projections.json";

  // The four dispatch dimensions: display key -> (full vocabulary, declared-projected set).
  private static IReadOnlyDictionary<string, (IReadOnlySet<string> All, IReadOnlySet<string> Projected)> Dimensions()
  {
    var schema = SchemaExport.Build();
    IReadOnlySet<string> baseDiscriminators(string discriminatorKey) =>
      schema
        .Bases.Where(b => b.DiscriminatorKey == discriminatorKey)
        .SelectMany(b => b.Types.Select(t => t.Discriminator))
        .ToHashSet(StringComparer.Ordinal);

    var restrictions = Enum.GetNames<ActivationRestriction>()
      .Concat(Enum.GetNames<TriggeredAbilityRestriction>())
      .ToHashSet(StringComparer.Ordinal);

    return new Dictionary<string, (IReadOnlySet<string>, IReadOnlySet<string>)>
    {
      ["effectType"] = (baseDiscriminators("EffectType"), PortWalkProjection.EffectTypes),
      ["costType"] = (baseDiscriminators("CostType"), PortWalkProjection.CostTypes),
      ["triggerEvent"] = (Enum.GetNames<TriggerEvent>().ToHashSet(StringComparer.Ordinal), PortWalkProjection.TriggerEvents),
      ["restriction"] = (restrictions, PortWalkProjection.GatingRestrictions),
    };
  }

  /// <summary>
  /// Pure ratchet logic — kept separate from the schema so it can be self-tested with synthetic input.
  /// Returns a human-readable failure per violation; empty list == pass.
  /// </summary>
  internal static List<string> CheckDimension(
    string dimension,
    IReadOnlySet<string> all,
    IReadOnlySet<string> projected,
    IReadOnlyDictionary<string, string> whitelist
  )
  {
    var failures = new List<string>();

    // 1) Every discriminator is projected or explicitly whitelisted by name.
    foreach (var d in all.OrderBy(x => x, StringComparer.Ordinal))
      if (!projected.Contains(d) && !whitelist.ContainsKey(d))
        failures.Add(
          $"[{dimension}] \"{d}\" is neither projected (PortWalkProjection) nor on the coarse-projection whitelist. "
            + "Add a semantic projection to PortWalk, or add a justified named entry to known-coarse-projections.json."
        );

    // 2) Declared-projected entries must be real discriminators (typo guard).
    foreach (var p in projected.OrderBy(x => x, StringComparer.Ordinal))
      if (!all.Contains(p))
        failures.Add($"[{dimension}] PortWalkProjection declares \"{p}\" but no such discriminator exists (typo/stale).");

    // 3) Whitelist entries must stay honest: a now-projected or vanished name fails until removed, and
    //    every name carries a non-empty reason (it is an EXPLICIT, justified carve-out, not a count).
    foreach (var (key, reason) in whitelist.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
      if (projected.Contains(key))
        failures.Add($"[{dimension}] \"{key}\" is whitelisted as coarse but is now projected — remove it from known-coarse-projections.json.");
      else if (!all.Contains(key))
        failures.Add($"[{dimension}] whitelist entry \"{key}\" is not a known discriminator (stale — remove it).");
      if (string.IsNullOrWhiteSpace(reason))
        failures.Add($"[{dimension}] whitelist entry \"{key}\" has an empty reason — name why it is permitted to be a coarse projection.");
    }

    return failures;
  }

  [Test]
  public void Every_discriminator_is_projected_or_justified()
  {
    var whitelist = LoadWhitelist();
    var failures = new List<string>();
    foreach (var (dim, (all, projected)) in Dimensions())
      failures.AddRange(CheckDimension(dim, all, projected, whitelist.GetValueOrDefault(dim, new())));

    Assert.That(
      failures,
      Is.Empty,
      "PortWalk projection exhaustiveness invariant failed:\n  " + string.Join("\n  ", failures)
    );
  }

  // ----- self-tests of the invariant logic (synthetic, no real schema) -----

  [Test]
  public void Fails_a_new_unprojected_unwhitelisted_discriminator()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken", "brandNewEffect" },
      projected: new HashSet<string> { "createToken" },
      whitelist: new Dictionary<string, string>()
    );
    Assert.That(failures, Has.Some.Contains("brandNewEffect"));
  }

  [Test]
  public void Passes_when_new_discriminator_is_whitelisted()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken", "brandNewEffect" },
      projected: new HashSet<string> { "createToken" },
      whitelist: new Dictionary<string, string> { ["brandNewEffect"] = "inert; no flow rule consumes it" }
    );
    Assert.That(failures, Is.Empty);
  }

  [Test]
  public void Fails_a_whitelisted_entry_that_is_now_projected()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken" },
      projected: new HashSet<string> { "createToken" },
      whitelist: new Dictionary<string, string> { ["createToken"] = "stale" }
    );
    Assert.That(failures, Has.Some.Contains("now projected"));
  }

  [Test]
  public void Fails_an_empty_reason_and_a_stale_entry()
  {
    var empty = CheckDimension("costType", new HashSet<string> { "mana" }, new HashSet<string> { "mana" }, new Dictionary<string, string>());
    Assert.That(empty, Is.Empty, "baseline sanity");

    var emptyReason = CheckDimension("costType", new HashSet<string> { "mana", "discard" }, new HashSet<string> { "mana" }, new Dictionary<string, string> { ["discard"] = "  " });
    Assert.That(emptyReason, Has.Some.Contains("empty reason"));

    var stale = CheckDimension("costType", new HashSet<string> { "mana" }, new HashSet<string> { "mana" }, new Dictionary<string, string> { ["goneAway"] = "x" });
    Assert.That(stale, Has.Some.Contains("not a known discriminator"));
  }

  // ----- whitelist IO -----

  private static string WhitelistPath() => Path.Combine(RepoRoot(), WhitelistRelPath);

  // Reserved top-level prose key (a string, not a dimension) — the named-whitelist doc-string.
  private const string DocKey = "_doc";

  private static Dictionary<string, Dictionary<string, string>> LoadWhitelist()
  {
    var path = WhitelistPath();
    Assert.That(File.Exists(path), Is.True, $"Missing coarse-projection whitelist at {path}. Seed it via the [Explicit] Regenerate test.");

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
    foreach (var dim in doc.RootElement.EnumerateObject())
    {
      if (dim.Name == DocKey)
        continue; // the prose doc-string, not a dispatch dimension
      var map = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var entry in dim.Value.EnumerateObject())
        map[entry.Name] = entry.Value.GetString() ?? "";
      result[dim.Name] = map;
    }

    return result;
  }

  /// <summary>
  /// Seeds / refreshes known-coarse-projections.json: every non-projected discriminator gets a named
  /// entry, preserving existing reasons and defaulting new ones. Projected discriminators are dropped (the
  /// whitelist holds only live, coarse names). Run after a deliberate projection change:
  /// <c>nx run mast:test -- --filter Regenerate_coarse_projection_whitelist</c> (or the dotnet
  /// equivalent), then edit reasons and commit.
  /// </summary>
  [Test, Explicit("Writes known-coarse-projections.json to the source tree.")]
  public void Regenerate_coarse_projection_whitelist()
  {
    var existing = File.Exists(WhitelistPath()) ? LoadWhitelist() : new();
    const string defaultReason =
      "no semantic projection yet — coarse fallback no flow rule consumes; explicit named carve-out";

    // Preserve the prose doc-string as the first key on regen.
    var output = new Dictionary<string, object>
    {
      [DocKey] =
        "Explicit named whitelist of PortWalk dispatch discriminators permitted to be COARSE — each "
        + "carries a reason justifying why it has no semantic projection (it falls to an emit:/pay:/role "
        + "label no flow rule reads). STATELESS INVARIANT (PortWalkExhaustivenessTests): every "
        + "discriminator must be EITHER projected (PortWalkProjection) OR named here; a new one that is "
        + "neither fails loudly. A name here that becomes projected, or that no longer exists, fails until "
        + "removed. This is a list of NAMES, each justified — not a count and not a moving ratchet. The "
        + "size is the interaction layer's known-blind-spot metric (see "
        + "docs/scratch/alignment-session/03-portwalk-exhaustiveness-findings.md).",
    };
    foreach (var (dim, (all, projected)) in Dimensions())
    {
      var dimMap = new Dictionary<string, string>();
      foreach (var d in all.Where(x => !projected.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
        dimMap[d] = existing.GetValueOrDefault(dim, new()).GetValueOrDefault(d, defaultReason);
      output[dim] = dimMap;
    }

    File.WriteAllText(
      WhitelistPath(),
      JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }) + "\n"
    );
    TestContext.Out.WriteLine($"Wrote {WhitelistPath()}");
    foreach (var (dim, map) in output)
      if (map is Dictionary<string, string> d)
        TestContext.Out.WriteLine($"  {dim}: {d.Count} coarse");
  }

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above test dir).");
  }
}
