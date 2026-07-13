namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "Copy target spell. You may choose new targets for the copy." — the copy of ANY
/// spell on the stack (See Double's first mode), as opposed to the instant-or-sorcery
/// restricted form handled by <see cref="CopyTargetSpellRule"/>.
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a
/// copy of it onto the stack; a copy of a spell isn't cast." The copy source is a
/// targeted spell on the stack — an <see cref="ObjectReferenceKind.Target"/> reference
/// whose filter pins the bare <c>spell</c> card type in <see cref="Zone.Stack"/>. This
/// is the any-spell analog of <see cref="CopyTargetSpellRule"/>; its regex requires
/// <c>spell</c> immediately after <c>target</c>, so it is mutually exclusive with the
/// "instant or sorcery spell" surface (anchored both ways — neither rule can swallow
/// the other's phrase).
/// </para>
///
/// <para>
/// "You may choose new targets for the copy" is the structured retarget permission on
/// <see cref="CopyEffect.MayChooseNewTargets"/> — a rules-meaningful flag, not free
/// text. The two-sentence body is matched as one effect so the permission rides the
/// copy rather than splitting off as a stray second effect.
/// </para>
/// </summary>
[SpellRule]
public sealed class CopyTargetAnySpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^copy\s+target\s+spell(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["spell"],
          Zone = Zone.Stack,
        },
      },
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };
    return true;
  }
}
