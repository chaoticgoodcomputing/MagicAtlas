namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// Recognises "You can't lose the game and your opponents can't win the game."
/// (Herald of Eternal Dawn) — a compound static lock pairing a blanket loss
/// exemption for the controller (CR 104.3: "There are several ways to lose the
/// game.") with a blanket win prohibition for the controller's opponents (CR
/// 104.2a: "A player still in the game wins the game if that player's opponents
/// have all left the game. This happens immediately and overrides all effects that
/// would preclude that player from winning the game."). Written as a plain static
/// statement (CR 604.1: "Static abilities do something all the time rather than
/// being activated or triggered. They are written as statements, and they're
/// simply true."). Emits a single <see cref="StaticAbility"/> carrying two effects:
/// a <see cref="CantLoseGameEffect"/> scoped to <see cref="ObjectReferenceKind.You"/>
/// and a <see cref="CantWinGameEffect"/> scoped to
/// <see cref="ObjectReferenceKind.EachOpponent"/> — mirroring how the timing and
/// effect of a single sentence can still decompose into two composable nodes.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantLoseGameOpponentsCantWinGameRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+can'?t\s+lose\s+the\s+game\s+and\s+your\s+opponents\s+can'?t\s+win\s+the\s+game\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantLoseGameEffect
          {
            Player = ObjectReference.You(),
          },
          new CantWinGameEffect
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
          },
        ],
      },
    ];
  }
}
