namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "you win the game." — a triggered ability's effect fragment stating that the
/// controller wins outright (CR 104.2b: "An effect may state that a player wins
/// the game."). Mirrors the spell-context precedent
/// (<see cref="MagicAST.Parsing.Parsers.Spell.Rules.ApproachOfTheSecondSunRule"/>)
/// but as a standalone <see cref="ITriggeredRule"/> so the bare sentence is
/// recognised whenever it appears as the (possibly sole) effect of a triggered
/// ability — e.g. Near-Death Experience: "At the beginning of your upkeep, if you
/// have exactly 1 life, you win the game."
///
/// <para>
/// Parameterless beyond the winning player; mirrors <see cref="TakeInitiativeRule"/>
/// doctrine (a fixed, parameter-free idiom recognised by anchored regex rather than
/// decomposed further). Anchored (^…$) so it can never match as a substring of a
/// longer, more specific sibling effect sentence.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class WinTheGameTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+win\s+the\s+game$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new WinTheGameEffect { Player = ObjectReference.You() };
    return true;
  }
}
