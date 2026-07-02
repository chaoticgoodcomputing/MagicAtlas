namespace MagicAST;

using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAST.Serialization;

/// <summary>
/// Shared JSON serialization options for MagicAST types.
/// Uses .NET 10's strict preset for robust validation.
/// </summary>
public static class MagicASTJsonOptions
{
  /// <summary>
  /// Default options using .NET 10's strict validation preset.
  /// - Disallows duplicate JSON properties
  /// - Disallows unmapped members
  /// - Respects nullable annotations
  /// - Respects required constructor parameters
  /// </summary>
  public static JsonSerializerOptions Strict { get; } = CreateStrictOptions();

  /// <summary>
  /// Options for lenient parsing (e.g., reading external data sources).
  /// Uses web defaults with camelCase naming.
  /// </summary>
  public static JsonSerializerOptions Web { get; } = JsonSerializerOptions.Web;

  private static JsonSerializerOptions CreateStrictOptions()
  {
    // Start with the .NET 10 strict preset
    var options = new JsonSerializerOptions(JsonSerializerOptions.Strict)
    {
      WriteIndented = false,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // Register the reflection-driven polymorphic converter factory. The factory
    // discovers all [PolymorphicBase]-tagged abstract bases in the MagicAST
    // assembly and produces a per-base converter on demand. This replaces the
    // static [JsonPolymorphic] + [JsonDerivedType] lists previously declared on
    // each base class.
    options.Converters.Add(new PolymorphicConverterFactory());

    return options;
  }
}
