namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// Rings of Brighthearth's conditional-pay copy-the-triggering-ability effect:
/// "you may pay {COST}. If you do, copy that ability. You may choose new targets
/// for the copy."
///
/// <para>
/// Decomposes as the canonical "you may pay [cost]. If you do, [Y]" shape
/// (Nim Deathmantle pattern):
/// <list type="bullet">
///   <item><see cref="OptionalEffect"/> — the "you may" wrapper.</item>
///   <item><see cref="ConditionalPayEffect"/> as <c>Inner</c> — the mana cost the
///     controller may optionally pay.</item>
///   <item><see cref="CopyEffect"/> as <c>IfYouDo</c> — the copy of the triggering
///     ability. <c>Target</c> is <see cref="ObjectReferenceKind.TriggeringAbility"/>
///     ("that ability" — the activated/triggered ability on the stack named by the
///     <see cref="MagicAST.AST.Triggers.TriggerEvent.AbilityActivated"/> trigger,
///     CR 113), and <c>MayChooseNewTargets = true</c> ("You may choose new targets
///     for the copy", CR 707.10b).</item>
/// </list>
/// </para>
///
/// <para>Priority 90: must run BEFORE the generic
/// <see cref="ConditionalPayTriggeredRule"/> (priority 80), whose consequent
/// delegation cannot structure "copy that ability" into the
/// <see cref="ObjectReferenceKind.TriggeringAbility"/> reference and would leave a
/// residual. This rule is more specific (it owns the full anchored clause).</para>
///
/// <para>ANCHORED (<c>^…$</c>): the clause is matched in full so no sibling
/// conditional-pay text is mislabeled.</para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CopyTriggeringAbilityConditionalPayTriggeredRule : ITriggeredRule
{
  // Full effect text (trailing period stripped by the dispatcher):
  // "you may pay {2}. If you do, copy that ability. You may choose new targets for the copy"
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\."
    + @"\s*If\s+you\s+do,\s*copy\s+that\s+ability\."
    + @"\s*You\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copy\.?$",
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

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ConditionalPayEffect { Cost = manaCost },
      IfYouDo = new CopyEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.TriggeringAbility },
        MayChooseNewTargets = true,
      },
    };
    return true;
  }
}
