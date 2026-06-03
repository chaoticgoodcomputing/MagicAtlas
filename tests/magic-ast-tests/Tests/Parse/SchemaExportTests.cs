namespace MagicAST.Tests.Tests;

using MagicAST.Schema;

/// <summary>
/// Keeps the committed schema export (<c>libs/magic-ast/schema/ast-schema.json</c>) honest: it must
/// equal what reflection produces from the current node model. A stale artifact fails the build —
/// the loud-drift guarantee of magic-ast ADR-0008. Regenerate with the [Explicit] test below or
/// <c>nx run magic-ast:schema</c>.
/// </summary>
[TestFixture]
public class SchemaExportTests
{
  private static string SchemaPath =>
    Path.Combine(RepoRoot(), "libs", "magic-ast", "schema", "ast-schema.json");

  [Test]
  public void Committed_schema_export_is_current()
  {
    Assert.That(
      File.Exists(SchemaPath),
      Is.True,
      $"Missing schema export at {SchemaPath}. Run `nx run magic-ast:schema` (or the Regenerate test)."
    );

    var expected = SchemaExport.Serialize(SchemaExport.Build());
    var actual = File.ReadAllText(SchemaPath);

    Assert.That(
      Normalize(actual),
      Is.EqualTo(Normalize(expected)),
      "ast-schema.json is stale. Regenerate it: `nx run magic-ast:schema`."
    );
  }

  [Test]
  public void Schema_has_a_content_hash_and_the_full_discriminator_vocabulary()
  {
    var schema = SchemaExport.Build();
    Assert.Multiple(() =>
    {
      Assert.That(schema.SchemaHash, Is.Not.Null);
      Assert.That(schema.SchemaHash!, Has.Length.EqualTo(64));
      Assert.That(schema.DiscriminatorKeys, Does.Contain("EffectType"));
      Assert.That(schema.DiscriminatorKeys, Does.Contain("CharacteristicType"));
      Assert.That(
        schema.UnparsedDiscriminators.Select(u => $"{u.Key}:{u.Value}"),
        Does.Contain("EffectType:unparsed")
      );
    });
  }

  [Test, Explicit("Writes the schema export to the source tree.")]
  public void Regenerate_schema_export()
  {
    Directory.CreateDirectory(Path.GetDirectoryName(SchemaPath)!);
    File.WriteAllText(SchemaPath, SchemaExport.Serialize(SchemaExport.Build()));
    TestContext.Out.WriteLine($"Wrote {SchemaPath}");
  }

  private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd();

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above test dir).");
  }
}
