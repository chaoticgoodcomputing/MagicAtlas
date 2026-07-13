namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// Mirari's conditional-pay copy-the-triggering-spell effect:
/// "you may pay {COST}. If you do, copy that spell. You may choose new targets
/// for the copy."
///
/// <para>
/// Decomposes as the canonical "you may pay [cost]. If you do, [Y]" shape
/// (Nim Deathmantle pattern), the spell-side twin of
/// <see cref="CopyTriggeringAbilityConditionalPayTriggeredRule"/> (Rings of
/// Brighthearth's "copy that ability"):
/// <list type="bullet">
///   <item><see cref="OptionalEffect"/> — the "you may" wrapper.</item>
///   <item><see cref="ConditionalPayEffect"/> as <c>Inner</c> — the mana cost the
///     controller may optionally pay.</item>
///   <item><see cref="CopyEffect"/> as <c>IfYouDo</c> — the copy of the triggering
///     spell. <c>Target</c> is <see cref="ObjectReferenceKind.It"/> — the anaphoric
///     back-reference to the triggering spell named by the enclosing
///     <see cref="MagicAST.AST.Triggers.TriggerEvent.SpellCast"/> trigger (the MAST
///     pronoun convention: "that spell" ≡ "it", as in
///     <see cref="CopyTriggeringSpellTriggeredRule"/>'s "copy it" and
///     <see cref="CounterThatSpellTriggeredRule"/>'s "counter that spell"), and
///     <c>MayChooseNewTargets = true</c> ("You may choose new targets for the copy",
///     CR 707.10b).</item>
/// </list>
/// </para>
///
/// <para>Priority 90: must run BEFORE the generic
/// <see cref="ConditionalPayTriggeredRule"/> (priority 80), whose consequent
/// delegation list does not structure "copy that spell" and would leave a
/// residual. This rule is more specific (it owns the full anchored clause).</para>
///
/// <para>ANCHORED (<c>^…$</c>): the clause is matched in full so no sibling
/// conditional-pay text is mislabeled. The "copy that spell" surface here is
/// distinct from the Rings "copy that ability" surface owned by the twin rule.</para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CopyTriggeringSpellConditionalPayTriggeredRule : ITriggeredRule
{
  // Full effect text (trailing period stripped by the dispatcher):
  // "you may pay {3}. If you do, copy that spell. You may choose new targets for the copy"
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\."
    + @"\s*If\s+you\s+do,\s*copy\s+that\s+spell\."
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
        Target = new ObjectReference { Kind = ObjectReferenceKind.It },
        MayChooseNewTargets = true,
      },
    };
    return true;
  }
}
