namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// "Copy target instant or sorcery spell you control. You may choose new targets for the copy."
/// — Twinning Staff's activated-ability copy-a-spell-you-control effect (Commander 2021, C21:219).
///
/// <para>
/// CR 707.10: "To copy a spell, activated ability, or triggered ability means to put a copy of
/// it onto the stack; a copy of a spell isn't cast…" The copy source is a targeted instant or
/// sorcery spell on the stack controlled by you — modelled as an
/// <see cref="ObjectReferenceKind.Target"/> reference with <see cref="Zone.Stack"/>, the
/// instant/sorcery card types, and <see cref="ControllerFilter.You"/> on the filter.
/// </para>
///
/// <para>
/// "You may choose new targets for the copy" is the structured retarget permission
/// carried on <see cref="CopyEffect.MayChooseNewTargets"/>.
/// </para>
///
/// <para>
/// Distinguished from <c>CopyTargetSpellTriggeredRule</c> (triggered ability) and
/// <c>CopyTargetSpellRule</c> (spell ability) by the "you control" controller restriction,
/// which appears uniquely in activated-ability spell-copy contexts and must not be silently
/// dropped. The higher priority (74, below the triggered rule's 75) ensures this rule fires
/// first when the text includes "you control", preventing the more-general triggered variant
/// from claiming it.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 74)]
public sealed class CopyTargetSpellYouControlEffectRule : IActivatedEffectRule
{
  // "copy target instant or sorcery spell you control[. You may choose new targets for the copy]"
  // Anchored: must match the full effect text (no substring match into a sibling rule).
  private static readonly Regex _pattern = new(
    @"^copy\s+target\s+instant\s+or\s+sorcery\s+spell\s+you\s+control(?:\.\s+(?<newtargets>you\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy))?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    return new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["instant", "sorcery"],
          Zone = Zone.Stack,
          Controller = ControllerFilter.You,
        },
      },
      MayChooseNewTargets = m.Groups["newtargets"].Success ? true : null,
    };
  }
}
