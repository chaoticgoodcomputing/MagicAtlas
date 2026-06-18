namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You create a number of [PredefinedToken] tokens equal to the result." —
/// creates predefined artifact tokens (Treasure, Food, Clue, Blood) in a count
/// equal to the result of a preceding die roll (CR 706.4).
///
/// <para>
/// CR 706.4: "Some abilities that instruct a player to roll one or more dice do
/// not include a results table. The text of those abilities will indicate how to
/// use the results of the die rolls, if at all." This sentence is the "how to
/// use" clause: the count of created tokens equals the die-roll result, modelled
/// as <see cref="DieRollResultQuantity"/> (reference-not-resolution, ADR 0004).
/// </para>
///
/// <para>
/// CR 111.10a: "A Treasure token is a colorless Treasure artifact token with
/// '{T}, Sacrifice this token: Add one mana of any color.'"
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the "equal to the result" tail is distinctive enough
/// to not match siblings, but anchoring is applied per the standing contract.
/// Priority 72: above the generic <see cref="CreateCopyOnCombatDamageTriggeredRule"/>
/// (70) and above the generic CreateTokenRule (50) so this more-specific pattern
/// is tried first.
/// </para>
/// </summary>
[TriggeredRule(Priority = 72)]
public sealed class CreateTokensEqualToDieResultTriggeredRule : ITriggeredRule
{
  // "You create a number of Treasure tokens equal to the result"
  // Handles the four canonical predefined artifact token types.
  // Terminal period stripped by the dispatcher before dispatch.
  private static readonly Regex _pattern = new(
    @"^you\s+create\s+a\s+number\s+of\s+(?<token>Treasure|Food|Clue|Blood)\s+tokens?\s+equal\s+to\s+the\s+result$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var tokenKind = m.Groups["token"].Value;
    var token = tokenKind.ToLowerInvariant() switch
    {
      "treasure" => TokenDefinition.Treasure(),
      "food"     => TokenDefinition.Food(),
      "clue"     => TokenDefinition.Clue(),
      "blood"    => TokenDefinition.Blood(),
      _          => null,
    };

    if (token is null)
    {
      return false;
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count  = new DieRollResultQuantity(),
      Token  = token,
    };
    return true;
  }
}
