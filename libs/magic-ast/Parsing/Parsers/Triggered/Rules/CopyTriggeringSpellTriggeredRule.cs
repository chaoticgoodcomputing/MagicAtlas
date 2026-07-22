namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "copy it. You may choose new targets for the copy."
/// — the resolution clause of triggers that copy the triggering spell
/// (e.g. Echoes of Eternity: "Whenever you cast a colorless spell, copy it.
/// You may choose new targets for the copy.").
///
/// <para>
/// Distinct from <see cref="CopyTargetSpellTriggeredRule"/> (which handles
/// "copy target instant or sorcery spell") — here the copy target is "it":
/// a pronoun referring to the colorless spell named by the trigger condition
/// (i.e. the spell that caused the trigger to fire). The reference is
/// <see cref="ObjectReferenceKind.It"/> rather than an explicit targeted filter.
/// </para>
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to
/// put a copy of it onto the stack." The reminder text "(A copy of a permanent
/// spell becomes a token.)" is engine semantics carried on the enclosing
/// <see cref="MagicAST.AST.Abilities.TriggeredAbility.Reminder"/>, not modeled
/// in the effect node.
/// </para>
///
/// <para>
/// "You may choose new targets for the copy" is the structured retarget permission
/// carried on <see cref="CopyEffect.MayChooseNewTargets"/> — a rules-meaningful
/// flag, not free text. The whole two-sentence body is matched as one effect so
/// the retarget permission rides the copy rather than splitting into a spurious
/// second effect.
/// </para>
/// </summary>
[TriggeredRule(Priority = 74)]
public sealed class CopyTriggeringSpellTriggeredRule : ITriggeredRule
{
  // "copy it[. You may choose new targets for the copy]" and the equivalent
  // "copy that spell or ability[. …]" surface used when the trigger fires on EITHER
  // a spell OR an activated ability (Bill Potts: "Whenever you cast an instant or
  // sorcery spell that targets only Bill Potts or activate an ability that targets
  // only Bill Potts, copy that spell or ability."). "that spell or ability" is the
  // same It back-reference to the triggering object as the bare "it".
  // The trailing terminal period is stripped by the dispatcher before TryMatch.
  private static readonly Regex _pattern = new(
    @"^copy\s+(?:it|that\s+spell\s+or\s+ability)(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
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
        Kind = ObjectReferenceKind.It,
      },
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };
    return true;
  }
}
