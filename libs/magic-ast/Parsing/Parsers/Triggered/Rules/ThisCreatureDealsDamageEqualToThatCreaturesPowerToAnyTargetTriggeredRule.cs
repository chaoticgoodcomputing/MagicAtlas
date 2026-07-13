namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "this creature deals damage equal to that creature's power to any target." — the
/// resolution half of the "Whenever another creature you control enters, …" trigger
/// family (Terror of the Peaks). The source is the ability's own permanent (CR 109 —
/// "this creature" is a self-reference); the damage amount is derived from the power
/// of the creature named by the trigger condition's Filter ("that creature" —
/// CR 603.2, the entering creature), not the source's own power; the recipient is any
/// legal target (CR 115.4: "any target" may be a creature, player, planeswalker, or
/// battle).
///
/// <para>
/// Rule 603.1: triggered abilities have a trigger condition and an effect (When/
/// Whenever/At [condition], [effect]). Rule 120.1: an object that deals damage is
/// the source of that damage.
/// </para>
///
/// <para>
/// Sibling of <see cref="ItDealsDamageEqualToItsPowerToAnyTargetTriggeredRule"/>
/// (same "damage equal to … power to any target" tail, but sourced from the trigger's
/// OWN power via the "it"/"its" pronoun, i.e. subject and amount-source are the same
/// object) and of <see cref="ThisCreatureDealsDamageToThatCreatureTriggeredRule"/>
/// (same "this creature" subject and "that creature" back-reference, but a literal
/// N-damage amount to that creature rather than a derived power-based amount to any
/// target). Here the subject ("this creature") and the amount-source ("that creature")
/// are two DIFFERENT objects, so neither sibling regex can match this text: anchored
/// (^…$) on the literal "this creature deals damage equal to that creature's power to
/// any target" surface.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThisCreatureDealsDamageEqualToThatCreaturesPowerToAnyTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^this\s+(?:creature|permanent|artifact|enchantment)\s+deals?\s+damage\s+equal\s+to\s+that\s+creature's\s+power\s+to\s+any\s+target\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Source = ObjectReference.Self(),
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "that creature",
      },
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
