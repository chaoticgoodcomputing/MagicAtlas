namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "That player may copy this spell and may choose a new target for that copy."
///
/// <para>
/// Chain of Smog's chain mechanic: the targeted player (that player) gets the
/// option to copy the spell itself (Self), with permission to redirect the
/// copy to a new target. CR 707.10: "To copy a spell … means to put a copy of
/// it onto the stack; a copy of a spell isn't cast and a copy of an activated
/// ability isn't activated. … If the spell has any targets, you may choose new
/// targets for the copy." The "may copy" wraps the whole thing in an
/// <see cref="OptionalEffect"/> (ADR 0005 — "you may" is composition, not a flag).
/// </para>
///
/// <para>
/// The <c>Player</c> field on <see cref="CopyEffect"/> records who creates the
/// copy — "that player" (<see cref="ObjectReferenceKind.ThatPlayer"/>), the
/// targeted player from the preceding <c>DiscardCardsEffect</c>. The copy
/// target is the spell itself (<see cref="ObjectReferenceKind.Self"/>).
/// <see cref="CopyEffect.MayChooseNewTargets"/> captures "may choose a new
/// target for that copy" (CR 707.10). CR 701.9a governs the preceding discard;
/// this clause governs the optional propagation of the chain.
/// </para>
/// </summary>
[SpellRule]
public sealed class ThatPlayerMayCopyThisSpellRule : ISpellRule
{
  // Anchored full-sentence match. The pattern is specific enough that substring
  // collision with other triggers is not possible — "that player may copy this
  // spell" does not appear in any shared helper surface.
  private static readonly Regex _pattern = new(
    @"^That\s+player\s+may\s+copy\s+this\s+spell\s+and\s+may\s+choose\s+a\s+new\s+target\s+for\s+that\s+copy\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new CopyEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
        MayChooseNewTargets = true,
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      },
    };
    return true;
  }
}
