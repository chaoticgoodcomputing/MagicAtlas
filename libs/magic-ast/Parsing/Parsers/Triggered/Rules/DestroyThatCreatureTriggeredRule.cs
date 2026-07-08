namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "destroy that creature" — the destroy resolution of the combat/damage back-reference
/// family (Engulfing Slagwurm, Sylvan Basilisk, Voracious Cobra, Horobi, …). The target
/// is the creature named by the trigger condition (the blocking/blocked/damaged creature),
/// encoded as <see cref="ObjectReferenceKind.ThatCreature"/> — the creature analogue of
/// "that player". Sibling of <see cref="ThisCreatureDealsDamageToThatCreatureTriggeredRule"/>
/// (same "that creature" anaphora, destroy instead of damage).
///
/// <para>
/// Anchored to the bare "destroy that creature" fragment (<c>^…$</c>). More-specific siblings
/// carry trailing qualifiers — "destroy that creature at end of combat", "… and this creature",
/// "… It can't be regenerated" (a separate sentence), "… unless its controller …" — which do
/// NOT match this anchor and fall through to their own handling (CR 701.8 destroy always leaves
/// the "can't be regenerated" clause as a distinct sentence, so it never rides here).
/// </para>
///
/// <para>
/// CR 701.8 (Destroy): "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard." CR 603.2: triggered abilities (When/Whenever/At) — the anaphoric "that creature"
/// back-references the object named by this ability's own trigger event.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class DestroyThatCreatureTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^destroy\s+that\s+creature\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new DestroyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
    };
    return true;
  }
}
