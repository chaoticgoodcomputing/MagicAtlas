namespace MagicAST.Analysis;

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using MagicAST.AST;

/// <summary>
/// Tallies the two ADR-0001 debt categories in a parsed AST subtree:
/// <see cref="IResidual"/> nodes + non-empty <see cref="FreeTextFieldAttribute"/>
/// fields (deferred-structure debt, keyed by type name or <c>Type.Property</c>),
/// and <see cref="IUnparsed"/> nodes (parse failures, keyed by type name).
/// Reflection-driven so the counts track the markers, not a hand-maintained type
/// list — a new residual/unparsed node is counted the moment it adopts a marker.
/// </summary>
public static class ResidualWalker
{
  private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _props = new();
  private static readonly Assembly _astAssembly = typeof(CardOracle).Assembly;

  /// <summary>Deferred-structure residual debt reachable from <paramref name="root"/>, by kind.</summary>
  public static IReadOnlyDictionary<string, int> Count(object? root) => Analyze(root).Residuals;

  /// <summary>Parse-failure (<see cref="IUnparsed"/>) nodes reachable from <paramref name="root"/>, by type name.</summary>
  public static IReadOnlyDictionary<string, int> CountUnparsed(object? root) => Analyze(root).Unparsed;

  /// <summary>Walks <paramref name="root"/> once, returning both debt tallies.</summary>
  public static AstDebt Analyze(object? root)
  {
    var residuals = new Dictionary<string, int>(StringComparer.Ordinal);
    var unparsed = new Dictionary<string, int>(StringComparer.Ordinal);
    Walk(root, residuals, unparsed);
    return new AstDebt(residuals, unparsed);
  }

  private static void Walk(
    object? node,
    Dictionary<string, int> residuals,
    Dictionary<string, int> unparsed
  )
  {
    if (node is null)
    {
      return;
    }

    var type = node.GetType();

    if (node is IUnparsed)
    {
      Bump(unparsed, type.Name);
    }
    else if (node is IResidual)
    {
      Bump(residuals, type.Name);
    }

    foreach (var prop in PropsOf(type))
    {
      object? value;
      try
      {
        value = prop.GetValue(node);
      }
      catch
      {
        continue;
      }

      if (value is null)
      {
        continue;
      }

      if (prop.IsDefined(typeof(FreeTextFieldAttribute), inherit: false) && IsNonEmptyFreeText(value))
      {
        Bump(residuals, $"{type.Name}.{prop.Name}");
      }

      Descend(value, residuals, unparsed);
    }
  }

  private static void Descend(
    object value,
    Dictionary<string, int> residuals,
    Dictionary<string, int> unparsed
  )
  {
    if (value is string)
    {
      return;
    }

    if (value is IEnumerable sequence)
    {
      foreach (var item in sequence)
      {
        if (item is not null)
        {
          Descend(item, residuals, unparsed);
        }
      }

      return;
    }

    // Only descend into AST nodes (reference types from this assembly); BCL
    // primitives, enums, and value types (e.g. TextSpan) are leaves.
    var type = value.GetType();
    if (type.Assembly == _astAssembly && type is { IsEnum: false, IsValueType: false })
    {
      Walk(value, residuals, unparsed);
    }
  }

  private static bool IsNonEmptyFreeText(object value) =>
    value switch
    {
      string s => !string.IsNullOrWhiteSpace(s),
      IEnumerable sequence => sequence.Cast<object?>().Any(),
      _ => true,
    };

  private static PropertyInfo[] PropsOf(Type type) =>
    _props.GetOrAdd(
      type,
      t =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
          .ToArray()
    );

  private static void Bump(Dictionary<string, int> tally, string key) =>
    tally[key] = tally.GetValueOrDefault(key) + 1;
}

/// <summary>
/// The two ADR-0001 debt tallies from one AST walk. <see cref="Residuals"/> is
/// deferred-structure debt (allowed but trending down); <see cref="Unparsed"/>
/// is parse-failure debt (must fail triage, banned from gold).
/// </summary>
public sealed record AstDebt(
  IReadOnlyDictionary<string, int> Residuals,
  IReadOnlyDictionary<string, int> Unparsed
);
