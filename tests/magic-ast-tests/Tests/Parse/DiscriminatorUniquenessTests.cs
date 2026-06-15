namespace MagicAST.Tests.Tests;

using MagicAST.Schema;

/// <summary>
/// Belt-and-braces guard for alignment initiative 02 (schema vocabulary governance): every
/// polymorphic discriminator value must be unique <b>within its base</b>. Two concrete types in the
/// same hierarchy claiming the same discriminator string is a serialization collision — the
/// polymorphic converter can't disambiguate them.
///
/// Uniqueness is deliberately PER-BASE, not global: cross-base reuse is legitimate and common in
/// this codebase (<c>untap</c> is an Effect, a Cost, AND a ReplacementEvent; <c>sacrifice</c>/<c>tap</c>/
/// <c>exile</c> are both Cost and Effect — disambiguated by the base context at deserialization). A
/// global-uniqueness assertion would fail on the current green tree.
///
/// The bash lint (<c>libs/magic-ast/scripts/lint-discriminators.py</c>) enforces the same invariant
/// inside the TDD loop, where <c>dotnet</c> may be unavailable in a worktree; this test catches
/// anything that reaches <c>nx run mast:test</c> (e.g. a manual commit outside the loop).
/// </summary>
[TestFixture]
public class DiscriminatorUniquenessTests
{
  [Test]
  public void Discriminators_are_unique_within_each_base()
  {
    var schema = SchemaExport.Build();

    var collisions = new List<string>();
    foreach (var b in schema.Bases)
    {
      var byValue = b.Types
        .GroupBy(t => t.Discriminator)
        .Where(g => g.Count() > 1);

      foreach (var g in byValue)
      {
        var clrTypes = string.Join(", ", g.Select(t => t.Type));
        collisions.Add($"[{b.DiscriminatorKey}] \"{g.Key}\" declared by {g.Count()} types: {clrTypes}");
      }
    }

    Assert.That(
      collisions,
      Is.Empty,
      "Duplicate discriminator(s) within a base — rename one (per-base uniqueness, initiative 02):\n  "
        + string.Join("\n  ", collisions)
    );
  }
}
