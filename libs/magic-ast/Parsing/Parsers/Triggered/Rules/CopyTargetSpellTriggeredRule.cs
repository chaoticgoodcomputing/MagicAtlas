namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "copy target instant or sorcery spell. You may choose new targets for the copy."
/// — the resolution clause of Dualcaster Mage's ETB trigger (and the broader
/// "copy a spell on the stack" family).
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to
/// put a copy of it onto the stack; a copy of a spell isn't cast and a copy of an
/// activated ability isn't activated." The copy source is a targeted spell on the
/// stack — modelled as an <see cref="ObjectReferenceKind.Target"/> reference whose
/// filter pins the instant-or-sorcery card types in <see cref="Zone.Stack"/>.
/// </para>
///
/// <para>
/// The optional second sentence "You may choose new targets for the copy" is the
/// structured retarget permission carried on
/// <see cref="CopyEffect.MayChooseNewTargets"/> — a rules-meaningful flag, not free
/// text. The whole two-sentence body is matched as one effect so the retarget
/// permission rides the copy rather than splitting into a spurious second effect.
/// </para>
/// </summary>
[TriggeredRule(Priority = 75)]
public sealed class CopyTargetSpellTriggeredRule : ITriggeredRule
{
  // "copy target instant or sorcery spell[. You may choose new targets for the copy]"
  // The trailing terminal period is stripped by the dispatcher before TryMatch; the
  // inter-sentence period is preserved so we match the whole two-sentence clause.
  private static readonly Regex _pattern = new(
    @"^copy\s+target\s+instant\s+or\s+sorcery\s+spell(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
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
          CardTypes = ["instant", "sorcery"],
          Zone = Zone.Stack,
        },
      },
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };
    return true;
  }
}
