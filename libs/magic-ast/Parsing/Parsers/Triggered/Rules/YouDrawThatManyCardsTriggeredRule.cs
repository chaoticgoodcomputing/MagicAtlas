namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you draw that many cards" — draw effect where the count is derived from the
/// triggering event (CR 120: damage dealing; "that many" is an anaphoric reference
/// to the amount of damage dealt). Backed by <see cref="DerivedKind.DamageDealt"/>.
///
/// <para>
/// Covers Niv-Mizzet, Visionary and similar cards whose trigger fires on damage
/// dealt and whose effect draws cards equal to the damage amount.
/// </para>
///
/// <para>
/// CR 121.1: "A player draws a card by putting the top card of their library
/// into their hand." The derived count records <i>how many</i> cards are drawn;
/// the "that many" antecedent is the damage dealt in the triggering event
/// (<see cref="DerivedKind.DamageDealt"/>), resolved by the engine (ADR 0004
/// reference-not-resolution).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class YouDrawThatManyCardsTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+draw\s+that\s+many\s+cards?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
