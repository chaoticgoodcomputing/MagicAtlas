namespace MagicAST.Analysis;

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using MagicAST.AST.References;

/// <summary>
/// Collects every <see cref="ObjectFilter"/> reachable from an AST root — a reflection walk
/// mirroring <see cref="ResidualWalker"/>. Used by <see cref="FilterCoverage"/> to harvest the
/// corpus of filters the relation operators are exercised over.
/// </summary>
public static class ObjectFilterCollector
{
  private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _props = new();
  private static readonly Assembly _astAssembly = typeof(ObjectFilter).Assembly;

  /// <summary>Every <see cref="ObjectFilter"/> reachable from <paramref name="root"/>, at any depth.</summary>
  public static IReadOnlyList<ObjectFilter> Collect(object? root)
  {
    var found = new List<ObjectFilter>();
    Walk(root, found, new HashSet<object>(ReferenceEqualityComparer.Instance));
    return found;
  }

  private static void Walk(object? node, List<ObjectFilter> found, HashSet<object> seen)
  {
    if (node is null || !seen.Add(node))
      return;
    if (node is ObjectFilter filter)
      found.Add(filter);

    foreach (var prop in PropsOf(node.GetType()))
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
      if (value is not null)
        Descend(value, found, seen);
    }
  }

  private static void Descend(object value, List<ObjectFilter> found, HashSet<object> seen)
  {
    if (value is string)
      return;
    if (value is IEnumerable sequence)
    {
      foreach (var item in sequence)
        if (item is not null)
          Descend(item, found, seen);
      return;
    }

    // Only descend into AST nodes (reference types from this assembly); BCL primitives, enums,
    // and value types (e.g. TextSpan) are leaves.
    var type = value.GetType();
    if (type.Assembly == _astAssembly && type is { IsEnum: false, IsValueType: false })
      Walk(value, found, seen);
  }

  private static PropertyInfo[] PropsOf(Type type) =>
    _props.GetOrAdd(
      type,
      t =>
        t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
          .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
          .ToArray()
    );
}
