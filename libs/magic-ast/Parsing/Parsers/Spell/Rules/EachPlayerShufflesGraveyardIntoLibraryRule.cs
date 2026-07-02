namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Each player shuffles their graveyard into their library." — symmetric
/// whole-zone recycle (Mnemonic Nexus).
///
/// <para>
/// CR 400.12: "Some effects instruct a player to do something to a zone (such
/// as 'Shuffle your hand into your library'). That action is performed on all
/// cards in that zone. The zone itself is not affected." CR 701.24a: "To
/// shuffle a library or a face-down pile of cards, randomize the cards within
/// it so that no player knows their order."
/// </para>
///
/// <para>
/// Anchored (^ … $) single-sentence match. Emits a
/// <see cref="ShuffleGraveyardIntoLibraryEffect"/> whose
/// <see cref="ShuffleGraveyardIntoLibraryEffect.Player"/> carries
/// <see cref="ObjectReferenceKind.EachPlayer"/> — matching the "each player"
/// subject in oracle text.
/// </para>
/// </summary>
[SpellRule]
public sealed class EachPlayerShufflesGraveyardIntoLibraryRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Each\s+player\s+shuffles\s+their\s+graveyard\s+into\s+their\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    effect = new ShuffleGraveyardIntoLibraryEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
    };
    return true;
  }
}
