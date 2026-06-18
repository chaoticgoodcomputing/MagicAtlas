namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create that many Treasure tokens." — token creation where the quantity is
/// derived from the damage dealt by the triggering combat-damage event (The Reaver
/// Cleaver: "Whenever this creature deals combat damage to a player or planeswalker,
/// create that many Treasure tokens.").
///
/// <para>
/// "That many" is an anaphoric reference to the amount of combat damage dealt in the
/// triggering event (CR 510.1 — combat damage step). Modelled as
/// <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.DamageDealt"/>, the
/// same derived quantity used by "you gain that much life" lifelink analogs. The
/// predefined Treasure token is as specified in CR 111.10a: "A Treasure token is a
/// colorless Treasure artifact token with '{T}, Sacrifice this token: Add one mana
/// of any color.'"
/// </para>
///
/// <para>
/// Rule 111.1: "A token is a marker used to represent any permanent that isn't
/// represented by a card." Rule 603.2: the triggering event fires the ability; the
/// effect clause creates tokens equal to the damage dealt.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full effect clause is matched to prevent this rule from
/// misfiring inside a broader effect sentence. Runs as a standard <c>[TriggeredRule]</c>
/// — lower priority than the predefined-token patterns in
/// <see cref="CreateTokenRule"/> but named uniquely so it does not collide with the
/// creature-token "that many" shape in <see cref="CreateThatManyTokensRule"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class CreateThatManyTreasureTokensRule : ITriggeredRule
{
  // "create that many Treasure tokens[.]"
  private static readonly Regex _pattern = new(
    @"^create\s+that\s+many\s+Treasure\s+tokens?\.?$",
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
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Token = TokenDefinition.Treasure(),
    };
    return true;
  }
}
