namespace MagicAST.Parsing.Parsers;

using System.Reflection;

// IPrioritizedRuleAttribute lives in its own file (IPrioritizedRuleAttribute.cs).

/// <summary>One discovered rule instance plus its dispatch metadata.</summary>
/// <typeparam name="TRule">The rule interface (e.g. <c>ISpellRule</c>).</typeparam>
public readonly record struct DiscoveredRule<TRule>(TRule Rule, string Name, int Priority);

/// <summary>
/// Generic reflection-based rule discovery, shared by every ability parser that
/// dispatches over a priority-ordered set of one-file-per-rule implementations.
/// Each parser is a thin dispatcher over <see cref="Discover{TRule, TAttr}"/>;
/// adding a new shape is dropping a new <c>[TAttr]</c>-decorated <c>TRule</c> file
/// with no edits to any shared file — which is what keeps the rule directories
/// free of merge conflicts under parallel batch dispatch.
/// </summary>
public static class RuleRegistry
{
  /// <summary>
  /// Scans the assembly containing <typeparamref name="TRule"/> for types marked
  /// with <typeparamref name="TAttr"/>, instantiates each (parameterless ctor
  /// required), and returns them ordered by descending priority then ordinal name
  /// for deterministic tie-breaking within a priority band.
  /// </summary>
  /// <param name="namePrefix">
  /// Prefix for the diagnostic rule name (e.g. <c>"SpellAbilityParser"</c>), yielding
  /// names like <c>"SpellAbilityParser.DestroyTargetSimpleRule"</c> for
  /// <c>LastAttemptedRule</c> telemetry.
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// A type carries <typeparamref name="TAttr"/> but doesn't implement
  /// <typeparamref name="TRule"/>, or lacks a parameterless constructor.
  /// </exception>
  public static IReadOnlyList<DiscoveredRule<TRule>> Discover<TRule, TAttr>(string namePrefix)
    where TRule : class
    where TAttr : Attribute, IPrioritizedRuleAttribute
  {
    var assembly = typeof(TRule).Assembly;
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

      var instance =
        (TRule?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException(
          $"Failed to instantiate {type.FullName} (parameterless constructor required)."
        );

      found.Add(new DiscoveredRule<TRule>(instance, $"{namePrefix}.{type.Name}", attr.Priority));
    }

    // Highest priority first; stable ordinal-name secondary so same-priority rules
    // (the common case — mutually-exclusive recognizers at the default band) have a
    // deterministic, run-stable order.
    return found
      .OrderByDescending(r => r.Priority)
      .ThenBy(r => r.Name, StringComparer.Ordinal)
      .ToList();
  }
}
