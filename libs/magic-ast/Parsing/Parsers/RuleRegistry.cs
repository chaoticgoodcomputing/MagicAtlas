namespace MagicAST.Parsing.Parsers;

using System.Reflection;

/// <summary>
/// Generic reflection-discovery helper for registry-style parser families. Given a
/// rule interface <typeparamref name="TRule"/> and a decorating attribute
/// <typeparamref name="TAttr"/> (which carries an <see cref="IPrioritizedRuleAttribute.Priority"/>),
/// <see cref="Discover{TRule,TAttr}"/> instantiates every decorated type in the
/// MagicAST assembly and returns them ranked by descending priority, with a stable
/// secondary sort by type name for determinism within a priority band.
///
/// <para>
/// This consolidates the previously hand-rolled <c>DiscoverRules()</c> loops (one per
/// parser family). Each family now states its dispatch order purely through the
/// <typeparamref name="TAttr"/> priority on each rule file — no shared ordered list to
/// edit when a rule is added.
/// </para>
/// </summary>
public static class RuleRegistry
{
  /// <summary>
  /// One discovered rule plus its dispatch priority and a diagnostic name.
  /// </summary>
  public readonly record struct DiscoveredRule<TRule>(TRule Rule, int Priority, string Name)
    where TRule : class;

  /// <summary>
  /// Discovers every type in <paramref name="assembly"/> (defaulting to the MagicAST
  /// assembly) that carries a <typeparamref name="TAttr"/> attribute, asserts it
  /// implements <typeparamref name="TRule"/>, instantiates it via its parameterless
  /// constructor, and returns the instances ranked by descending priority then by
  /// ordinal type name.
  /// </summary>
  /// <typeparam name="TRule">The rule interface the decorated type must implement.</typeparam>
  /// <typeparam name="TAttr">
  /// The discovery attribute. Must derive from <see cref="Attribute"/> and implement
  /// <see cref="IPrioritizedRuleAttribute"/>.
  /// </typeparam>
  public static IReadOnlyList<DiscoveredRule<TRule>> Discover<TRule, TAttr>(Assembly? assembly = null)
    where TRule : class
    where TAttr : Attribute, IPrioritizedRuleAttribute
  {
    assembly ??= typeof(RuleRegistry).Assembly;
    var found = new List<DiscoveredRule<TRule>>();
    foreach (var type in assembly.GetTypes())
    {
      var attr = type.GetCustomAttribute<TAttr>(inherit: false);
      if (attr is null)
      {
        continue;
      }
      if (!typeof(TRule).IsAssignableFrom(type))
      {
        throw new InvalidOperationException(
          $"{type.FullName} has [{typeof(TAttr).Name}] but does not implement {typeof(TRule).Name}."
        );
      }
      var instance = (TRule?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException(
          $"Failed to instantiate {type.FullName} (parameterless constructor required)."
        );
      found.Add(new DiscoveredRule<TRule>(instance, attr.Priority, type.Name));
    }

    // Highest priority first; stable secondary by name for determinism within a band.
    return found
      .OrderByDescending(r => r.Priority)
      .ThenBy(r => r.Name, StringComparer.Ordinal)
      .ToList();
  }
}
