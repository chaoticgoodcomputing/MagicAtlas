namespace MagicAST.Parsing.Tokens.Keywords;

using System.Reflection;

/// <summary>
/// Discovers all <see cref="StructuralKeywordAttribute"/>-tagged types in the
/// MagicAST assembly and exposes a case-insensitive text -> token lookup.
///
/// Discovery runs once on first access and is cached for the process lifetime.
/// Adding a new structural keyword means creating a new file decorated with
/// <see cref="StructuralKeywordAttribute"/> — no edits to this registry or to
/// <see cref="OracleTokenizer"/> are required.
/// </summary>
public static class StructuralKeywordRegistry
{
  private static readonly Lazy<IReadOnlyDictionary<string, OracleToken>> _table =
    new(BuildTable, LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>
  /// Attempts to resolve a word to its structural-keyword token.
  /// </summary>
  /// <param name="text">The tokenized word; matched case-insensitively.</param>
  /// <param name="token">The matched token on success; <see cref="OracleToken.None"/> otherwise.</param>
  /// <returns><c>true</c> if the word is a registered structural keyword.</returns>
  public static bool TryGet(string text, out OracleToken token) =>
    _table.Value.TryGetValue(text, out token);

  /// <summary>
  /// Enumerates the discovered keyword set. Primarily for diagnostics and tests.
  /// </summary>
  public static IReadOnlyDictionary<string, OracleToken> Entries => _table.Value;

  private static IReadOnlyDictionary<string, OracleToken> BuildTable()
  {
    var table = new Dictionary<string, OracleToken>(StringComparer.OrdinalIgnoreCase);
    var assembly = typeof(StructuralKeywordRegistry).Assembly;

    foreach (var type in assembly.GetTypes())
    {
      var attributes = type.GetCustomAttributes<StructuralKeywordAttribute>(inherit: false);
      foreach (var keyword in attributes)
      {
        if (table.TryGetValue(keyword.Text, out var existing))
        {
          throw new InvalidOperationException(
            $"Duplicate structural keyword '{keyword.Text}' on {type.FullName}: "
              + $"already registered as {existing}, also requested as {keyword.Token}."
          );
        }
        table[keyword.Text] = keyword.Token;
      }
    }

    return table;
  }
}
