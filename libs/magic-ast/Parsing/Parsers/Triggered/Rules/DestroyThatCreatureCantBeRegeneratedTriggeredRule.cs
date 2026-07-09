namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "destroy that creature. It can't be regenerated." — the regeneration-denial
/// variant of the combat/damage back-reference destroy family (Toxin Sliver,
/// Phage the Untouchable, Dripping Dead, Grotesque Hybrid, Merieke Ri Berit, …).
/// The target is the creature named by the trigger condition (the damaged/blocked
/// creature), encoded as <see cref="ObjectReferenceKind.ThatCreature"/>; the
/// trailing "It can't be regenerated." sentence rides as
/// <see cref="DestroyEffect.CantBeRegenerated"/> = true (a modifier on the destroy,
/// NOT a separate effect).
///
/// <para>
/// Sibling of <see cref="DestroyThatCreatureTriggeredRule"/> (same "that creature"
/// anaphora and destroy, but no regeneration clause). That rule anchors on the bare
/// "destroy that creature" fragment (<c>^…$</c>) and therefore does NOT match this
/// two-sentence form; this rule anchors the whole "destroy that creature. It can't
/// be regenerated" body so the two are mutually exclusive by surface. Mirrors the
/// spell-side <see cref="Spell.Rules.DestroyTargetCantBeRegeneratedRule"/> and
/// <see cref="Spell.Rules.DestroyAllRule"/> handling — the "can't be regenerated"
/// clause is not broken apart into a sibling effect.
/// </para>
///
/// <para>
/// CR 701.8 (Destroy): to destroy a permanent, move it from the battlefield to its
/// owner's graveyard. CR 701.19 (Regenerate): regeneration is a replacement effect;
/// "can't be regenerated" turns off that shield for this destruction. CR 603.2: the
/// anaphoric "that creature" back-references the object named by this ability's own
/// trigger event.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class DestroyThatCreatureCantBeRegeneratedTriggeredRule : ITriggeredRule
{
  // Anchored to the full two-sentence body: "destroy that creature. It can't be
  // regenerated" (optional trailing period). The internal sentence break and the
  // regeneration-denial clause are both required, so this cannot collide with the
  // bare "destroy that creature" sibling.
  private static readonly Regex _pattern = new(
    @"^destroy\s+that\s+creature\.\s+it\s+can't\s+be\s+regenerated\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
      CantBeRegenerated = true,
    };
    return true;
  }
}
