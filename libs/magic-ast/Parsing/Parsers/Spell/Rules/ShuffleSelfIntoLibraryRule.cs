namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Shuffle [CardName] into its owner's library." — the self-referential library
/// shuffle used on cards like Beacon of Immortality. The card refers to itself by
/// name, instructing that it be shuffled into its owner's library on resolution.
///
/// <para>
/// Rule 701.24c: "If an effect would cause a player to shuffle one or more specific
/// objects into a library, that library is shuffled even if none of those objects
/// are in the zone they're expected to be in." Rule 701.24 governs shuffle.
/// </para>
///
/// <para>
/// MAST maps the card-name self-reference to <see cref="ObjectReferenceKind.Self"/>
/// (the card itself; CR 201.5). The owner-shuffles mechanic is an inherent part of
/// the zone-change (CR 701.24). Emits a single <see cref="ShuffleIntoLibraryEffect"/>
/// with <c>Target = Self</c>.
/// </para>
///
/// <para>
/// GUARD: fully anchored (^ … $). Matches ONLY "Shuffle [CapitalizedName] into its
/// owner's library" — the name must begin with a capital letter and end before
/// "into its owner's library". Does NOT match "Its owner shuffles it into their
/// library" (handled by <see cref="ShuffleIntoLibraryRule"/>) or any substring
/// of a broader clause.
/// </para>
/// </summary>
[SpellRule]
public sealed class ShuffleSelfIntoLibraryRule : ISpellRule
{
  // "Shuffle [SomeName] into its owner's library"
  // The card name starts with an uppercase letter and may contain letters,
  // digits, spaces, hyphens, apostrophes, and commas.
  private static readonly Regex SelfShufflePattern = new(
    @"^Shuffle\s+(?<name>[A-Z][A-Za-z0-9 ',\-]*?)\s+into\s+its\s+owner's\s+library$",
    RegexOptions.Compiled
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = SelfShufflePattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new ShuffleIntoLibraryEffect
    {
      Target = ObjectReference.Self(),
    };
    return true;
  }
}
