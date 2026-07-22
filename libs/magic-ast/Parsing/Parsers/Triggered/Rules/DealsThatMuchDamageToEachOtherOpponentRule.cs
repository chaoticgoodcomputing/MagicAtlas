namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals that much damage to each other opponent." — reflexive combat-damage
/// spread (Grenzo's Ruffians pattern). Paired with a DealsCombatDamageToPlayer
/// trigger; the "that much" refers to the amount of combat damage dealt to the
/// trigger's original recipient, and "each other opponent" is every opponent
/// other than that recipient.
///
/// Rule 120.1: a source deals damage. Rule 510 (Combat Damage Step): the trigger
/// fires when combat damage is dealt to an opponent. "Each other opponent" is the
/// set of opponents excluding the one already hit; this is recorded as an EachOpponent
/// reference whose filter carries the relational <see cref="ObjectFilter.Excludes"/>
/// axis pointing at ThatPlayer (the opponent just dealt combat damage, CR 109.5). The
/// exclusion's runtime resolution (relative to the trigger's opponent) is engine
/// territory per the descriptive-not-engine doctrine.
///
/// Rule 702.121 (Melee) lists Grenzo's Ruffians as a canonical example of this
/// pattern. The rule encodes "that much" as a CalculatedQuantity with
/// Expression="that much" and Operation="match", matching the convention
/// established for Chatterfang's "that many" token-augmentation replacement.
/// </summary>
[TriggeredRule]
public sealed class DealsThatMuchDamageToEachOtherOpponentRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    effect = new DealDamageEffect
    {
      Amount = new CalculatedQuantity
      {
        Expression = "that much",
        Operation = "match",
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.EachOpponent,
        Filter = new MagicAST.AST.References.ObjectFilter
        {
          // "each OTHER opponent" — excludes the opponent just dealt combat damage
          // (that player, established by the trigger), CR 109.5.
          Excludes = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        },
      },
    };
    return true;
  }

  private static readonly Regex _pattern = new(
    @"^it\s+deals?\s+that\s+much\s+damage\s+to\s+each\s+other\s+opponent\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
