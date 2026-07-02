namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "shuffle it into its owner's library" — the resolution effect of the Future
/// Sight anti-mill family (Dread, Guile, Hostility, Purity, Vigor, Worldspine
/// Wurm, Serra Avatar). The trigger fires when the source is put into a graveyard
/// (see <see cref="SelfPutIntoGraveyardFromAnywhereConditionRule"/>); on resolution
/// the source is shuffled back into its owner's library so it can't be milled out.
///
/// <para>
/// "it" is the trigger subject — the source permanent/card itself — so this maps to
/// a <see cref="ShuffleIntoLibraryEffect"/> with <see cref="ObjectReferenceKind.Self"/>,
/// identical in shape to the spell-side self-shuffle
/// (<see cref="MagicAST.Parsing.Parsers.Spell.Rules.ShuffleSelfIntoLibraryRule"/>,
/// e.g. Beacon of Immortality). The owner-shuffles mechanic is an inherent part of the
/// zone change (CR 701.24).
/// </para>
///
/// <para>
/// Anchored ^…$ to match only the bare "shuffle it into its owner's library" effect
/// clause and not a substring of a broader sentence. Reuses the existing
/// <see cref="ShuffleIntoLibraryEffect"/> discriminator (no new effect node).
/// </para>
///
/// <para>
/// Rule citations: CR 701.24 (Shuffle — its own example is this exact family text:
/// "When Guile is put into a graveyard from anywhere, shuffle it into its owner's
/// library."), CR 603.2 (triggered-ability resolution).
/// </para>
/// </summary>
[TriggeredRule(Priority = 64)]
public sealed class ShuffleSelfIntoLibraryTriggeredRule : ITriggeredRule
{
  // "shuffle it into its owner's library" (optional trailing period).
  private static readonly Regex _pattern = new(
    @"^shuffle\s+it\s+into\s+its\s+owner's\s+library\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
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
