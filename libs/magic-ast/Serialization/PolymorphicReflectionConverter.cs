namespace MagicAST.Serialization;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>
/// A <see cref="JsonConverter{T}"/> that handles polymorphic dispatch for a
/// hierarchy rooted at <typeparamref name="TBase"/>, replacing the static
/// <see cref="JsonPolymorphicAttribute"/> + <see cref="JsonDerivedTypeAttribute"/>
/// approach with reflection-driven discovery.
///
/// <para>
/// Discovery rules at construction time:
/// </para>
/// <list type="bullet">
///   <item><typeparamref name="TBase"/> must carry <see cref="PolymorphicBaseAttribute"/>
///   to declare its discriminator JSON property name.</item>
///   <item>All non-abstract types in the same assembly that are assignable to
///   <typeparamref name="TBase"/> and carry a <see cref="PolymorphicTypeAttribute"/>
///   are registered with their discriminator value.</item>
///   <item>Duplicate discriminators or duplicate types throw at construction.</item>
/// </list>
///
/// <para>Strict-mode behavior preserved:</para>
/// <list type="bullet">
///   <item>Missing discriminator on read → <see cref="JsonException"/>.</item>
///   <item>Unknown discriminator value on read → <see cref="JsonException"/>.</item>
///   <item>Unmapped properties on the concrete type → <see cref="JsonException"/>
///   (inherited from the configured <see cref="JsonSerializerOptions"/>).</item>
/// </list>
/// </summary>
public sealed class PolymorphicReflectionConverter<TBase> : JsonConverter<TBase>
  where TBase : class
{
  private readonly string _discriminatorPropertyName;
  private readonly IReadOnlyDictionary<string, Type> _discriminatorToType;
  private readonly IReadOnlyDictionary<Type, string> _typeToDiscriminator;

  public PolymorphicReflectionConverter()
  {
    var baseAttr =
      typeof(TBase).GetCustomAttribute<PolymorphicBaseAttribute>(inherit: false)
      ?? throw new InvalidOperationException(
        $"{typeof(TBase).FullName} is missing [PolymorphicBase] — cannot build polymorphic converter."
      );
    _discriminatorPropertyName = baseAttr.DiscriminatorPropertyName;

    var discToType = new Dictionary<string, Type>(StringComparer.Ordinal);
    var typeToDisc = new Dictionary<Type, string>();

    foreach (var type in typeof(TBase).Assembly.GetTypes())
    {
      if (type.IsAbstract || !typeof(TBase).IsAssignableFrom(type))
      {
        continue;
      }

      var attr = type.GetCustomAttribute<PolymorphicTypeAttribute>(inherit: false);
      if (attr is null)
      {
        continue;
      }

      if (discToType.TryGetValue(attr.Discriminator, out var existing))
      {
        throw new InvalidOperationException(
          $"Duplicate discriminator '{attr.Discriminator}' for base {typeof(TBase).Name}: "
            + $"{existing.FullName} and {type.FullName}."
        );
      }

      discToType[attr.Discriminator] = type;
      typeToDisc[type] = attr.Discriminator;
    }

    _discriminatorToType = discToType;
    _typeToDiscriminator = typeToDisc;
  }

  public override TBase? Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  )
  {
    using var doc = JsonDocument.ParseValue(ref reader);
    var root = doc.RootElement;

    if (root.ValueKind != JsonValueKind.Object)
    {
      throw new JsonException(
        $"Expected JSON object for {typeof(TBase).Name}, got {root.ValueKind}."
      );
    }

    if (!root.TryGetProperty(_discriminatorPropertyName, out var discProp))
    {
      throw new JsonException(
        $"Missing discriminator property '{_discriminatorPropertyName}' for {typeof(TBase).Name}."
      );
    }

    var disc =
      discProp.GetString()
      ?? throw new JsonException(
        $"Discriminator '{_discriminatorPropertyName}' for {typeof(TBase).Name} must be a string."
      );

    if (!_discriminatorToType.TryGetValue(disc, out var concreteType))
    {
      throw new JsonException(
        $"Unknown {typeof(TBase).Name} discriminator '{disc}'. "
          + $"Known: {string.Join(", ", _discriminatorToType.Keys)}."
      );
    }

    // Build a mutable copy of the object without the discriminator, then
    // deserialize as the concrete type. Stripping is required because strict
    // mode would otherwise reject the discriminator as an unmapped property.
    var node =
      JsonObject.Create(root)
      ?? throw new JsonException(
        $"Failed to materialise JsonObject for {typeof(TBase).Name}."
      );
    node.Remove(_discriminatorPropertyName);

    return (TBase?)node.Deserialize(concreteType, options);
  }

  public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
  {
    var type = value.GetType();
    if (!_typeToDiscriminator.TryGetValue(type, out var disc))
    {
      throw new JsonException(
        $"No discriminator registered for {type.FullName} under base {typeof(TBase).Name}. "
          + $"Add a [PolymorphicType]-derived attribute to register it."
      );
    }

    // Serialize the concrete value to a JsonObject using the runtime's normal
    // (reflection-based) serialization, then prepend the discriminator property.
    // This avoids re-entering this converter for the value itself.
    var serialized =
      JsonSerializer.SerializeToNode(value, type, options) as JsonObject
      ?? throw new JsonException(
        $"Expected JSON object when serializing {type.FullName}."
      );

    var output = new JsonObject { [_discriminatorPropertyName] = disc };
    foreach (var kvp in serialized)
    {
      output[kvp.Key] = kvp.Value?.DeepClone();
    }

    output.WriteTo(writer, options);
  }
}
