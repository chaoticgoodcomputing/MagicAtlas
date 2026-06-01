namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Copy target instant or sorcery spell[, then return it to its owner's hand]. You may
/// choose new targets for the copy." — the spell-side copy of an instant-or-sorcery on
/// the stack (Reiterate; Narset's Reversal adds the bounce).
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a
/// copy of it onto the stack; a copy of a spell isn't cast and a copy of an activated
/// ability isn't activated." The copy source is a targeted spell on the stack — modelled
/// as an <see cref="ObjectReferenceKind.Target"/> reference whose filter pins the
/// instant-or-sorcery card types in <see cref="Zone.Stack"/>. This is the spell-cast
/// analog of <c>CopyTargetSpellTriggeredRule</c> (Dualcaster Mage's ETB trigger).
/// </para>
///
/// <para>
/// "You may choose new targets for the copy" is the structured retarget permission
/// carried on <see cref="CopyEffect.MayChooseNewTargets"/> — a rules-meaningful flag, not
/// free text. Narset's Reversal's "then return it to its owner's hand" bounces the copied
/// (originally targeted) spell and is emitted as a second sibling
/// <see cref="ReturnToHandEffect"/> via <see cref="IMultiSpellRule.TryMatchMulti"/>; its
/// <c>Target</c> is the same instant-or-sorcery spell reference the copy is taken from.
/// </para>
/// </summary>
[SpellRule(Priority = 75)]
public sealed class CopyTargetSpellRule : ISpellRule, IMultiSpellRule
{
  // "Copy target instant or sorcery spell[, then return it to its owner's hand][. You may
  // choose new targets for the copy]". The trailing terminal period is stripped by the
  // dispatcher; the inter-sentence period before "You may choose new targets" is preserved
  // so the retarget permission rides the copy rather than splitting off as a stray effect.
  private static readonly Regex _pattern = new(
    @"^copy\s+target\s+instant\s+or\s+sorcery\s+spell"
      + @"(?<bounce>,\s*then\s+return\s+it\s+to\s+its\s+owner's\s+hand)?"
      + @"(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static ObjectReference SpellTarget() =>
    new()
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter
      {
        CardTypes = ["instant", "sorcery"],
        Zone = Zone.Stack,
      },
    };

  private static CopyEffect BuildCopy(Match m) =>
    new()
    {
      Target = SpellTarget(),
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };

  // -------------------------------------------------------------------------
  // ISpellRule — single CopyEffect when there is no "then return it" bounce.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success || m.Groups["bounce"].Success)
    {
      return false;
    }

    effect = BuildCopy(m);
    return true;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — [CopyEffect, ReturnToHandEffect] when the copied spell is
  // also bounced ("then return it to its owner's hand").
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success || !m.Groups["bounce"].Success)
    {
      return false;
    }

    effects = new List<Effect>
    {
      BuildCopy(m),
      // "then return IT" is a back-reference to the already-targeted spell, not a
      // second target (CR 707.10 — one targeted spell). ObjectReferenceKind.It,
      // per the Act of Treason "do X to target, then do Y to it" precedent.
      new ReturnToHandEffect { Target = new ObjectReference { Kind = ObjectReferenceKind.It } },
    };
    return true;
  }
}
