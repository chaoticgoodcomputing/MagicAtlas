namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals damage equal to its power to any target." — a death (or other) trigger
/// whose damage amount is derived from the source's own power rather than a fixed
/// number, e.g. "When this creature dies, it deals damage equal to its power to
/// any target."
///
/// Rule 603.1: triggered abilities have a trigger condition and an effect (When/
/// Whenever/At [condition], [effect]). Rule 120.1: an object that deals damage is
/// the source of that damage. Rule 115.4: "any target" may be a creature, player,
/// planeswalker, or battle.
///
/// This is the derived-Power sibling of <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/>,
/// which handles the literal N-damage form ("it deals N damage to any target").
/// The two are disjoint: that rule requires a numeric/word amount token between
/// "deals" and "damage", while this rule requires the literal phrase "damage equal
/// to its power" — neither regex can match the other's text.
/// </summary>
[TriggeredRule]
public sealed class ItDealsDamageEqualToItsPowerToAnyTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^it\s+deals?\s+damage\s+equal\s+to\s+its\s+power\s+to\s+any\s+target\.?$",
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
      Source = ObjectReference.It(),
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "it",
      },
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
