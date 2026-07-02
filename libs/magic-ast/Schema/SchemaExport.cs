namespace MagicAST.Schema;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.Serialization;

/// <summary>
/// Builds <see cref="AstSchema"/> by reflecting over the MagicAST assembly, reusing the SAME
/// discovery rule as <see cref="PolymorphicReflectionConverter{TBase}"/>: a base carries
/// <see cref="PolymorphicBaseAttribute"/>; a concrete assignable type carries a
/// <see cref="PolymorphicTypeAttribute"/>. Sharing the rule is what guarantees the export agrees
/// with serialization rather than drifting from it (magic-ast ADR-0008).
/// </summary>
public static class SchemaExport
{
  private static readonly JsonSerializerOptions FileOptions =
    new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

  private static readonly JsonSerializerOptions CanonicalOptions =
    new() { WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

  /// <summary>Reflects the current node model into a schema, including its content hash.</summary>
  public static AstSchema Build()
  {
    var allTypes = typeof(SchemaExport).Assembly.GetTypes();

    var bases = new List<SchemaBase>();
    var discriminatorKeys = new SortedSet<string>(StringComparer.Ordinal);
    var unparsed = new List<UnparsedDiscriminator>();

    foreach (var baseType in allTypes)
    {
      var baseAttr = baseType.GetCustomAttribute<PolymorphicBaseAttribute>(inherit: false);
      if (baseAttr is null)
        continue;

      var key = baseAttr.DiscriminatorPropertyName;
      discriminatorKeys.Add(key);

      var types = new List<SchemaType>();
      foreach (var type in allTypes)
      {
        if (type.IsAbstract || !baseType.IsAssignableFrom(type))
          continue;
        var typeAttr = type.GetCustomAttribute<PolymorphicTypeAttribute>(inherit: false);
        if (typeAttr is null)
          continue;

        var isUnparsed = typeof(IUnparsed).IsAssignableFrom(type);
        if (isUnparsed)
          unparsed.Add(new UnparsedDiscriminator { Key = key, Value = typeAttr.Discriminator });

        types.Add(
          new SchemaType
          {
            Type = type.Name,
            Discriminator = typeAttr.Discriminator,
            IsUnparsed = isUnparsed,
            Fields = SerializedFieldNames(type),
          }
        );
      }

      bases.Add(
        new SchemaBase
        {
          Type = baseType.Name,
          DiscriminatorKey = key,
          Types = types.OrderBy(t => t.Discriminator, StringComparer.Ordinal).ToArray(),
        }
      );
    }

    var schema = new AstSchema
    {
      DiscriminatorKeys = discriminatorKeys.ToArray(),
      UnparsedDiscriminators = unparsed
        .OrderBy(u => u.Key, StringComparer.Ordinal)
        .ThenBy(u => u.Value, StringComparer.Ordinal)
        .ToArray(),
      Bases = bases.OrderBy(b => b.Type, StringComparer.Ordinal).ToArray(),
    };

    return schema with { SchemaHash = ComputeHash(schema) };
  }

  /// <summary>Serializes a schema to the committed-artifact form (indented, stable, diff-friendly).</summary>
  public static string Serialize(AstSchema schema) => JsonSerializer.Serialize(schema, FileOptions);

  private static IReadOnlyList<string> SerializedFieldNames(Type type)
  {
    var names = new SortedSet<string>(StringComparer.Ordinal);
    foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
      if (p.GetMethod is null)
        continue;
      if (p.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
        continue;
      names.Add(p.Name);
    }
    return names.ToArray();
  }

  private static string ComputeHash(AstSchema schema)
  {
    // Hash a canonical (compact, hash-excluded) projection so the handle is independent of file
    // formatting; Build() already sorts every collection deterministically.
    var canonical = JsonSerializer.Serialize(schema with { SchemaHash = null }, CanonicalOptions);
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}
