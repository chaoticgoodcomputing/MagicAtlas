namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you create a Treasure token for each opponent dealt damage" — the Malcolm,
/// Keen-Eyed Navigator effect pattern. The controller creates one Treasure token
/// per opponent who was dealt damage by the triggering event.
///
/// <para>
/// The quantity "for each opponent dealt damage" is modelled as a
/// <see cref="CalculatedQuantity"/> with
/// <c>Expression = "for each opponent dealt damage"</c> — an anaphoric reference
/// to the set of opponents hit in the triggering
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.DealsDamageToOpponents"/> event.
/// There is no structured <c>CountQuantity</c> for "opponents dealt damage" (that
/// set is a dynamic game-state query outside <see cref="ObjectFilter"/> scope);
/// the free-text expression is the type-honest residual per ADR 0001.
/// </para>
///
/// <para>
/// The predefined Treasure token is as specified in CR 111.10a: "A Treasure token
/// is a colorless Treasure artifact token with '{T}, Sacrifice this token: Add
/// one mana of any color.'" Rule 111.1: "A token is a marker used to represent
/// any permanent that isn't represented by a card." Rule 603.2: the triggering
/// event fires the ability; the effect clause creates tokens equal to the number
/// of opponents dealt damage.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full effect clause is matched to prevent this rule from
/// misfiring inside a broader effect sentence.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class CreateTreasureForEachOpponentDealtDamageRule : ITriggeredRule
{
  // "you create a Treasure token for each opponent dealt damage[.]"
  private static readonly Regex _pattern = new(
    @"^you\s+create\s+a\s+Treasure\s+token\s+for\s+each\s+opponent\s+dealt\s+damage\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = new CalculatedQuantity { Expression = "for each opponent dealt damage" },
      Token = TokenDefinition.Treasure(),
    };
    return true;
  }
}
