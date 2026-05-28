namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;

/// <summary>
/// Service for expanding keywords into their full ability subtrees.
/// </summary>
public interface IKeywordExpander
{
  /// <summary>
  /// Expands a keyword into its full ability subtree.
  /// </summary>
  /// <param name="keyword">The keyword name (case-insensitive).</param>
  /// <param name="parameter">Optional parameter for parameterized keywords.</param>
  /// <returns>The expanded ability with keywordSource set.</returns>
  /// <exception cref="KeyNotFoundException">If the keyword is not registered.</exception>
  Ability Expand(string keyword, string? parameter = null);

  /// <summary>
  /// Checks if a keyword is registered and can be expanded.
  /// </summary>
  bool CanExpand(string keyword);

  /// <summary>
  /// Gets the definition for a keyword, if registered.
  /// </summary>
  KeywordDefinition? GetDefinition(string keyword);

  /// <summary>
  /// Gets all registered keyword definitions.
  /// </summary>
  IEnumerable<KeywordDefinition> AllDefinitions { get; }
}

/// <summary>
/// Default implementation of keyword expansion using a registry of definitions.
/// </summary>
public sealed class KeywordExpander : IKeywordExpander
{
  private readonly Dictionary<string, KeywordDefinition> _definitions;

  public KeywordExpander(IEnumerable<KeywordDefinition> definitions)
  {
    _definitions = definitions.ToDictionary(
      d => d.Name.ToLowerInvariant(),
      d => d,
      StringComparer.OrdinalIgnoreCase
    );
  }

  public Ability Expand(string keyword, string? parameter = null)
  {
    var normalizedKeyword = keyword.ToLowerInvariant();

    if (!_definitions.TryGetValue(normalizedKeyword, out var definition))
    {
      throw new KeyNotFoundException($"Keyword '{keyword}' is not registered.");
    }

    if (definition.HasParameter && string.IsNullOrEmpty(parameter))
    {
      throw new ArgumentException($"Keyword '{keyword}' requires a parameter.", nameof(parameter));
    }

    return definition.CreateExpansion(parameter);
  }

  public bool CanExpand(string keyword) => _definitions.ContainsKey(keyword.ToLowerInvariant());

  public KeywordDefinition? GetDefinition(string keyword) =>
    _definitions.TryGetValue(keyword.ToLowerInvariant(), out var def) ? def : null;

  public IEnumerable<KeywordDefinition> AllDefinitions => _definitions.Values;

  /// <summary>
  /// Creates a KeywordExpander with the standard Magic keyword definitions.
  ///
  /// <para>
  /// Phase-2 bridge: definitions discovered from one-file-per-keyword
  /// <see cref="IKeyword"/> files (<see cref="KeywordRegistry.RegisteredDefinitions"/>)
  /// take precedence, then the legacy <see cref="KeywordDefinitions.All"/> fills in any
  /// keyword not yet migrated. A migrated keyword's legacy entry is therefore shadowed.
  /// Dedup is by case-insensitive <see cref="KeywordDefinition.Name"/> (the expander's
  /// constructor builds a name-keyed dictionary and would throw on a duplicate key), so
  /// this dedup is load-bearing, not cosmetic.
  /// </para>
  /// </summary>
  public static KeywordExpander CreateDefault()
  {
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var merged = new List<KeywordDefinition>();
    foreach (var def in KeywordRegistry.RegisteredDefinitions.Concat(KeywordDefinitions.All))
    {
      if (seen.Add(def.Name))
      {
        merged.Add(def);
      }
    }
    return new KeywordExpander(merged);
  }
}
