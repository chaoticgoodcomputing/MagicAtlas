namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;

/// <summary>
/// "Add X mana in any combination of colors, where X is the greatest power among
/// creatures you control." — Selvala, Heart of the Wilds mana ability.
///
/// <para>
/// The X here is NOT a cost variable (CR 107.3): it is defined inline by the
/// "where X is …" clause as the current greatest power among creatures the
/// controller controls. MAST records this as a <see cref="CalculatedQuantity"/>
/// with <see cref="CalculatedQuantity.Expression"/> =
/// <c>"greatest power among creatures you control"</c> — a game-state query
/// (the maximum over a set of power values) with no structured analog in the
/// existing quantity hierarchy. Reference-not-resolution (ADR 0004): MAST
/// names the quantity; the engine evaluates it.
/// </para>
///
/// <para>
/// The mana produced is X units, freely distributed across any combination of
/// the five colors (<see cref="AddManaEffect.AnyCombinationOf"/> =
/// <c>["W","U","B","R","G"]</c>). Per CR 106.4: "When an effect instructs a
/// player to add mana, that mana goes into a player's mana pool."
/// </para>
///
/// <para>
/// Per CR 605.1a — "An activated ability is a mana ability if it meets all of
/// the following criteria: it doesn't require a target, it could add mana to a
/// player's mana pool when it resolves, and it's not a loyalty ability." — the
/// enclosing <c>{G}, {T}:</c> ability IS a mana ability; the enclosing parser
/// derives <c>IsManaAbility = true</c>.
/// </para>
///
/// <para>
/// Runs at Priority = 1002 (above <see cref="AddManaXOnePlusManaValueEffectRule"/>'s
/// 1001 and <see cref="AddManaEffectRule"/>'s 1000) so the Selvala "where X is …"
/// shape is claimed before the base rule's <c>UnmodeledManaClause</c> bail (or the
/// S3 <c>AnyCombination</c> regex's partial match of the "X mana in any combination
/// of colors" prefix).
/// </para>
///
/// <para>
/// The rule is anchored (<c>^…$</c>) to prevent a substring match against a
/// more-specific sibling.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1002)]
public sealed class AddManaXGreatestPowerEffectRule : IActivatedEffectRule
{
  // Anchored: matches the full add-mana sentence.
  // "Add X mana in any combination of colors, where X is the greatest power among
  //  creatures you control."
  private static readonly Regex _pattern = new(
    @"^Add\s+X\s+mana\s+in\s+any\s+combination\s+of\s+colors,\s+where\s+X\s+is\s+the\s+greatest\s+power\s+among\s+creatures\s+you\s+control\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      Amount = new CalculatedQuantity
      {
        Expression = "greatest power among creatures you control",
      },
      AnyCombinationOf = ["W", "U", "B", "R", "G"],
    };
  }
}
