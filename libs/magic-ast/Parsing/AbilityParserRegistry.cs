namespace MagicAST.Parsing;

using System.Reflection;
using MagicAST.AST.Abilities;
using MagicAST.Diagnostics;
using MagicAST.Parsing.Parsers;

/// <summary>
/// Discovers <see cref="IAbilityParser"/> implementations decorated with
/// <see cref="OracleAbilityParserAttribute"/> and maps each
/// <see cref="AbilityKind"/> to its parser. Discovery runs once on first
/// access and is cached for the process lifetime.
///
/// Adding a new ability-kind parser means creating a new file with the
/// attribute and the interface implementation; no edits to this registry,
/// to <see cref="OracleParser"/>, or to any other existing file are required.
///
/// Kinds with no registered parser (today: <see cref="AbilityKind.Modal"/>,
/// <see cref="AbilityKind.Spell"/>, and the fallback path) resolve to a
/// built-in fallback that produces a generic <see cref="UnparsedAbility"/>.
/// </summary>
public sealed class AbilityParserRegistry
{
  private static readonly Lazy<IReadOnlyDictionary<AbilityKind, IAbilityParser>> _parsers =
    new(BuildTable, LazyThreadSafetyMode.ExecutionAndPublication);

  private static readonly Lazy<IAbilityParser> _fallback =
    new(() => new DefaultFallbackParser(), LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>
  /// Returns the parser registered for the given <see cref="AbilityKind"/>,
  /// or a generic fallback parser if none is registered.
  /// </summary>
  public IAbilityParser GetParser(AbilityKind kind) =>
    _parsers.Value.TryGetValue(kind, out var parser) ? parser : _fallback.Value;

  /// <summary>
  /// Enumerates the kind -> parser registrations. Primarily for diagnostics.
  /// </summary>
  public IReadOnlyDictionary<AbilityKind, IAbilityParser> Registrations => _parsers.Value;

  private static IReadOnlyDictionary<AbilityKind, IAbilityParser> BuildTable()
  {
    var table = new Dictionary<AbilityKind, IAbilityParser>();
    var assembly = typeof(AbilityParserRegistry).Assembly;

    foreach (var type in assembly.GetTypes())
    {
      var attr = type.GetCustomAttribute<OracleAbilityParserAttribute>(inherit: false);
      if (attr is null)
      {
        continue;
      }

      if (!typeof(IAbilityParser).IsAssignableFrom(type))
      {
        throw new InvalidOperationException(
          $"{type.FullName} has [OracleAbilityParser] but does not implement IAbilityParser."
        );
      }

      if (table.TryGetValue(attr.Kind, out var existing))
      {
        throw new InvalidOperationException(
          $"Duplicate parser registration for {attr.Kind}: "
            + $"{existing.GetType().FullName} and {type.FullName}."
        );
      }

      var instance =
        (IAbilityParser?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException(
          $"Failed to instantiate {type.FullName} (parameterless constructor required)."
        );

      table[attr.Kind] = instance;
    }

    return table;
  }

  /// <summary>
  /// Built-in fallback used when no parser is registered for a given kind.
  /// Produces an <see cref="UnparsedAbility"/> with a generic
  /// "<c>{kind} ability parser not yet implemented</c>" diagnostic.
  /// </summary>
  private sealed class DefaultFallbackParser : IAbilityParser
  {
    private readonly FallbackParser _fallback = new();

    public IReadOnlyList<Ability> Parse(
      OracleClause clause,
      ClauseClassification classification
    )
    {
      return
      [
        _fallback.Parse(
          clause,
          classification,
          $"{classification.Kind} ability parser not yet implemented"
        ),
      ];
    }
  }
}
