namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.Abilities;
using MagicAST.AST.Triggers;
using MagicAST.Interaction;
using MagicAST.Schema;

/// <summary>
/// Exhaustiveness ratchet for <see cref="PortWalk"/> (alignment initiative 03 #1). Every AST
/// discriminator that PortWalk dispatches on — every <c>EffectType</c>, <c>CostType</c>, trigger
/// <c>Event</c>, and restriction kind — must be EITHER semantically projected (declared in
/// <see cref="PortWalkProjection"/>) OR carry a justified entry in
/// <c>libs/mast-interaction/known-coarse-projections.json</c>.
///
/// Why: a discriminator with no semantic projection falls through to a coarse totality label
/// (<c>emit:&lt;x&gt;</c> / <c>pay:&lt;x&gt;</c> / a coarse trigger role) that no flow rule reads — the
/// port exists but yields zero recall. Every TDD batch that adds an effect type silently degrades
/// interactions while the parser tests stay green. This ratchet converts that drift into a failing
/// test: a new discriminator that is neither projected nor allowlisted fails here, forcing a conscious
/// projection decision. The allowlist is shrink-or-justify (same pattern as oracle-text-quarantine):
/// an entry that becomes projected must be removed.
///
/// The allowlist size is the interaction layer's known-blind-spot metric — see
/// docs/scratch/alignment-session/03-portwalk-exhaustiveness-findings.md.
/// </summary>
[TestFixture]
public class PortWalkExhaustivenessTests
{
  private const string AllowlistRelPath = "libs/mast-interaction/known-coarse-projections.json";

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
    IReadOnlyDictionary<string, string> allowlist
  )
  {
    var failures = new List<string>();

    // 1) Every discriminator is projected or allowlisted.
    foreach (var d in all.OrderBy(x => x, StringComparer.Ordinal))
      if (!projected.Contains(d) && !allowlist.ContainsKey(d))
        failures.Add(
          $"[{dimension}] \"{d}\" is neither projected (PortWalkProjection) nor in the allowlist. "
            + "Add a semantic projection to PortWalk, or add a justified entry to known-coarse-projections.json."
        );

    // 2) Declared-projected entries must be real discriminators (typo guard).
    foreach (var p in projected.OrderBy(x => x, StringComparer.Ordinal))
      if (!all.Contains(p))
        failures.Add($"[{dimension}] PortWalkProjection declares \"{p}\" but no such discriminator exists (typo/stale).");

    // 3) Allowlist entries: shrink-or-justify + non-empty reason + not stale.
    foreach (var (key, reason) in allowlist.OrderBy(kv => kv.Key, StringComparer.Ordinal))
    {
      if (projected.Contains(key))
        failures.Add($"[{dimension}] \"{key}\" is allowlisted but now projected — remove it from the allowlist (shrink-or-justify).");
      else if (!all.Contains(key))
        failures.Add($"[{dimension}] allowlist entry \"{key}\" is not a known discriminator (stale — remove it).");
      if (string.IsNullOrWhiteSpace(reason))
        failures.Add($"[{dimension}] allowlist entry \"{key}\" has an empty reason — one line on why it's a coarse projection.");
    }

    return failures;
  }

  [Test]
  public void Every_discriminator_is_projected_or_justified()
  {
    var allowlist = LoadAllowlist();
    var failures = new List<string>();
    foreach (var (dim, (all, projected)) in Dimensions())
      failures.AddRange(CheckDimension(dim, all, projected, allowlist.GetValueOrDefault(dim, new())));

    Assert.That(
      failures,
      Is.Empty,
      "PortWalk projection exhaustiveness ratchet failed:\n  " + string.Join("\n  ", failures)
    );
  }

  // ----- self-tests of the ratchet logic (synthetic, no real schema) -----

  [Test]
  public void Ratchet_fails_a_new_unprojected_unallowlisted_discriminator()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken", "brandNewEffect" },
      projected: new HashSet<string> { "createToken" },
      allowlist: new Dictionary<string, string>()
    );
    Assert.That(failures, Has.Some.Contains("brandNewEffect"));
  }

  [Test]
  public void Ratchet_passes_when_new_discriminator_is_allowlisted()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken", "brandNewEffect" },
      projected: new HashSet<string> { "createToken" },
      allowlist: new Dictionary<string, string> { ["brandNewEffect"] = "inert; no flow rule consumes it" }
    );
    Assert.That(failures, Is.Empty);
  }

  [Test]
  public void Ratchet_fails_an_allowlisted_entry_that_is_now_projected()
  {
    var failures = CheckDimension(
      "effectType",
      all: new HashSet<string> { "createToken" },
      projected: new HashSet<string> { "createToken" },
      allowlist: new Dictionary<string, string> { ["createToken"] = "stale" }
    );
    Assert.That(failures, Has.Some.Contains("now projected"));
  }

  [Test]
  public void Ratchet_fails_an_empty_reason_and_a_stale_entry()
  {
    var empty = CheckDimension("costType", new HashSet<string> { "mana" }, new HashSet<string> { "mana" }, new Dictionary<string, string>());
    Assert.That(empty, Is.Empty, "baseline sanity");

    var emptyReason = CheckDimension("costType", new HashSet<string> { "mana", "discard" }, new HashSet<string> { "mana" }, new Dictionary<string, string> { ["discard"] = "  " });
    Assert.That(emptyReason, Has.Some.Contains("empty reason"));

    var stale = CheckDimension("costType", new HashSet<string> { "mana" }, new HashSet<string> { "mana" }, new Dictionary<string, string> { ["goneAway"] = "x" });
    Assert.That(stale, Has.Some.Contains("not a known discriminator"));
  }

  // ----- allowlist IO -----

  private static string AllowlistPath() => Path.Combine(RepoRoot(), AllowlistRelPath);

  private static Dictionary<string, Dictionary<string, string>> LoadAllowlist()
  {
    var path = AllowlistPath();
    Assert.That(File.Exists(path), Is.True, $"Missing allowlist at {path}. Seed it via the [Explicit] Regenerate test.");
    return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
      ?? new();
  }

  /// <summary>
  /// Seeds / refreshes known-coarse-projections.json: every non-projected discriminator gets an entry,
  /// preserving existing reasons and defaulting new ones. Projected discriminators are dropped (shrink).
  /// Run after a deliberate projection change: <c>nx run mast:test -- --filter Regenerate_coarse_projection_allowlist</c>
  /// (or the dotnet equivalent), then edit reasons and commit.
  /// </summary>
  [Test, Explicit("Writes known-coarse-projections.json to the source tree.")]
  public void Regenerate_coarse_projection_allowlist()
  {
    var existing = File.Exists(AllowlistPath()) ? LoadAllowlist() : new();
    const string defaultReason =
      "baseline coarse fallback (initiative-03 inception 2026-06-15) — no flow rule consumes it yet";

    var output = new Dictionary<string, Dictionary<string, string>>();
    foreach (var (dim, (all, projected)) in Dimensions())
    {
      var dimMap = new Dictionary<string, string>();
      foreach (var d in all.Where(x => !projected.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
        dimMap[d] = existing.GetValueOrDefault(dim, new()).GetValueOrDefault(d, defaultReason);
      output[dim] = dimMap;
    }

    File.WriteAllText(
      AllowlistPath(),
      JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }) + "\n"
    );
    TestContext.Out.WriteLine($"Wrote {AllowlistPath()}");
    foreach (var (dim, map) in output)
      TestContext.Out.WriteLine($"  {dim}: {map.Count} coarse");
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
