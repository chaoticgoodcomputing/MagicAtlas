using System.Reflection;
using MagicAST.Serialization;

namespace MagicAtlas.Ast.Tests.Flows.Common;

/// <summary>
/// The near-duplicate relation over the discriminator vocabulary, and the declaration-site rulings that
/// explain individual pairs. Reflects the MagicAST assembly directly (the same discovery rule the
/// polymorphic converter uses: a base carries <c>PolymorphicBaseAttribute</c>, a concrete type carries a
/// <see cref="PolymorphicTypeAttribute"/>).
///
/// <para>Pure and dependency-free so the <c>DiscriminatorGovernance</c> report and any future consumer
/// share one definition of "near". The Python lint
/// (<c>libs/magic-ast/scripts/lint-discriminators.py</c>) implements the same rule over source text, for
/// worktrees without <c>dotnet</c>; the two are pinned to each other by
/// <c>DiscriminatorNearnessTests</c>.</para>
/// </summary>
public static class DiscriminatorNearness
{
  public const int LevenshteinMax = 2;
  public const int StemMinLength = 4;

  /// <summary>One declared discriminator: its family, value, declaring type, and any ruling.
  ///
  /// <para><b>Family = the polymorphic BASE type, not the JSON discriminator key.</b> Several bases share
  /// a key (<c>Kind</c> is the key of both the ability hierarchy and the ability-reference hierarchy), and
  /// grouping by key would invent cross-hierarchy pairs like <c>activated</c> ~ <c>activatedAbility</c>
  /// that are not the same vocabulary at all. Same scoping as <c>DiscriminatorUniquenessTests</c> and the
  /// Python lint, which keys on the attribute name.</para></summary>
  public sealed record Declared(
    string Family,
    string Value,
    string TypeName,
    IReadOnlyList<string> NearDuplicateOf,
    string? Reason
  );

  /// <summary>Every discriminator declared in the MagicAST assembly.</summary>
  public static IReadOnlyList<Declared> All()
  {
    var assembly = typeof(PolymorphicTypeAttribute).Assembly;
    var allTypes = assembly.GetTypes();
    var declared = new List<Declared>();

    foreach (var baseType in allTypes)
    {
      var baseAttr = baseType.GetCustomAttribute<PolymorphicBaseAttribute>(inherit: false);
      if (baseAttr is null)
        continue;
      foreach (var type in allTypes)
      {
        if (type.IsAbstract || !baseType.IsAssignableFrom(type))
          continue;
        var attr = type.GetCustomAttribute<PolymorphicTypeAttribute>(inherit: false);
        if (attr is null)
          continue;
        declared.Add(
          new Declared(
            baseType.Name,
            attr.Discriminator,
            type.Name,
            attr.NearDuplicateOf,
            attr.Reason
          )
        );
      }
    }
    return declared.OrderBy(d => d.Family, StringComparer.Ordinal)
      .ThenBy(d => d.Value, StringComparer.Ordinal)
      .ToList();
  }

  public static int Levenshtein(string a, string b)
  {
    a = a.ToLowerInvariant();
    b = b.ToLowerInvariant();
    if (a == b)
      return 0;
    if (a.Length == 0)
      return b.Length;
    if (b.Length == 0)
      return a.Length;

    var prev = Enumerable.Range(0, b.Length + 1).ToArray();
    for (var i = 1; i <= a.Length; i++)
    {
      var cur = new int[b.Length + 1];
      cur[0] = i;
      for (var j = 1; j <= b.Length; j++)
      {
        var cost = a[i - 1] == b[j - 1] ? 0 : 1;
        cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
      }
      prev = cur;
    }
    return prev[b.Length];
  }

  /// <summary>One value is a prefix-stem of the other (<c>dealDamage</c> / <c>dealDamageToEach</c>).</summary>
  public static bool SharesStem(string a, string b)
  {
    var x = a.ToLowerInvariant();
    var y = b.ToLowerInvariant();
    if (x == y)
      return false;
    var (shorter, longer) = x.Length <= y.Length ? (x, y) : (y, x);
    return shorter.Length >= StemMinLength && longer.StartsWith(shorter, StringComparison.Ordinal);
  }

  public static bool IsNear(string a, string b) =>
    a != b && (Levenshtein(a, b) <= LevenshteinMax || SharesStem(a, b));

  /// <summary>One near-duplicate pair with the ruling that explains it, if any.</summary>
  public sealed record Pair(
    string Family,
    string A,
    string B,
    string Nearness,
    string? ExplainedBy,
    string? Reason
  );

  /// <summary>Every intra-family near-duplicate pair, each once, ordinal-ordered. A pair is explained if
  /// EITHER side's attribute names the other (the relation is symmetric).</summary>
  public static IReadOnlyList<Pair> NearPairs(IReadOnlyList<Declared>? declared = null)
  {
    var all = declared ?? All();
    var rulings = new Dictionary<(string Family, string A, string B), Declared>();
    foreach (var d in all)
      foreach (var other in d.NearDuplicateOf)
      {
        rulings[(d.Family, d.Value, other)] = d;
        rulings.TryAdd((d.Family, other, d.Value), d);
      }

    var pairs = new List<Pair>();
    foreach (var family in all.GroupBy(d => d.Family, StringComparer.Ordinal))
    {
      var values = family
        .Select(d => d.Value)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(v => v, StringComparer.Ordinal)
        .ToList();
      for (var i = 0; i < values.Count; i++)
        for (var j = i + 1; j < values.Count; j++)
        {
          if (!IsNear(values[i], values[j]))
            continue;
          var ruling = rulings.GetValueOrDefault((family.Key, values[i], values[j]));
          pairs.Add(
            new Pair(
              family.Key,
              values[i],
              values[j],
              Levenshtein(values[i], values[j]) <= LevenshteinMax ? "levenshtein" : "prefix-stem",
              ruling?.Value,
              ruling?.Reason
            )
          );
        }
    }
    return pairs
      .OrderBy(p => p.ExplainedBy is null ? 0 : 1)
      .ThenBy(p => p.Family, StringComparer.Ordinal)
      .ThenBy(p => p.A, StringComparer.Ordinal)
      .ThenBy(p => p.B, StringComparer.Ordinal)
      .ToList();
  }

  /// <summary>Rulings whose counterpart is gone or no longer near — the one liveness failure the
  /// declaration-site attribute cannot make structural (it dies with its OWN type, not the other's).</summary>
  public static IReadOnlyList<string> DeadRulings(IReadOnlyList<Declared>? declared = null)
  {
    var all = declared ?? All();
    var byFamily = all.GroupBy(d => d.Family, StringComparer.Ordinal)
      .ToDictionary(g => g.Key, g => g.Select(d => d.Value).ToHashSet(StringComparer.Ordinal));
    var dead = new List<string>();
    foreach (var d in all)
      foreach (var other in d.NearDuplicateOf)
      {
        if (!byFamily[d.Family].Contains(other))
          dead.Add($"[{d.Family}] {d.TypeName} (\"{d.Value}\") names \"{other}\", which is not declared");
        else if (!IsNear(d.Value, other))
          dead.Add($"[{d.Family}] {d.TypeName} (\"{d.Value}\") names \"{other}\", no longer a near-duplicate");
      }
    return dead.OrderBy(s => s, StringComparer.Ordinal).ToList();
  }
}
