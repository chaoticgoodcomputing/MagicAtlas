namespace MagicAST.Serialization;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that produces a
/// <see cref="PolymorphicReflectionConverter{TBase}"/> for any type marked with
/// <see cref="PolymorphicBaseAttribute"/>.
///
/// The set of recognised base types is discovered at startup by scanning the
/// MagicAST assembly. Adding a new polymorphic hierarchy means:
/// <list type="number">
///   <item>Decorate the abstract base with <see cref="PolymorphicBaseAttribute"/>.</item>
///   <item>Author a per-base <see cref="PolymorphicTypeAttribute"/> subclass
///   (e.g., <c>OracleAbilityAttribute</c>).</item>
///   <item>Decorate concrete derived types with that attribute.</item>
/// </list>
/// No edits to this factory, to <see cref="MagicASTJsonOptions"/>, or to any
/// existing base class are required.
/// </summary>
public sealed class PolymorphicConverterFactory : JsonConverterFactory
{
  private static readonly Lazy<HashSet<Type>> _polymorphicBases =
    new(DiscoverBases, LazyThreadSafetyMode.ExecutionAndPublication);

  public override bool CanConvert(Type typeToConvert) =>
    _polymorphicBases.Value.Contains(typeToConvert);

  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(PolymorphicReflectionConverter<>).MakeGenericType(typeToConvert);
    return (JsonConverter)Activator.CreateInstance(converterType)!;
  }

  private static HashSet<Type> DiscoverBases()
  {
    var bases = new HashSet<Type>();
    var assembly = typeof(PolymorphicConverterFactory).Assembly;
    foreach (var type in assembly.GetTypes())
    {
      if (type.GetCustomAttribute<PolymorphicBaseAttribute>(inherit: false) is not null)
      {
        bases.Add(type);
      }
    }
    return bases;
  }
}
