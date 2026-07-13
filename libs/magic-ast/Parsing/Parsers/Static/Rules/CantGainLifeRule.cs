namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// Recognises the global life-gain lock static "Players can't gain life." (Giant
/// Cindermaw) and the asymmetric "Your opponents can't gain life." (Knight of
/// Dusk's Shadow). A rules-of-the-game continuous effect (CR 611.1) written as a
/// plain static statement (CR 604.1) that stops a player or players from gaining
/// life — see CR 119.7 for the authoritative can't-gain-life rule. Emits a single
/// <see cref="StaticAbility"/> carrying one <see cref="CantGainLifeEffect"/>, scoped
/// to <see cref="ObjectReferenceKind.EachPlayer"/> for the symmetric "Players" case
/// or <see cref="ObjectReferenceKind.EachOpponent"/> for the "Your opponents" case.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantGainLifeRule : IStaticRule
{
  private static readonly Regex _playersPattern = new(
    @"^\s*Players\s+can'?t\s+gain\s+life\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _opponentsPattern = new(
    @"^\s*Your\s+opponents\s+can'?t\s+gain\s+life\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    ObjectReferenceKind kind;
    if (_playersPattern.IsMatch(clause.RawText))
    {
      kind = ObjectReferenceKind.EachPlayer;
    }
    else if (_opponentsPattern.IsMatch(clause.RawText))
    {
      kind = ObjectReferenceKind.EachOpponent;
    }
    else
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantGainLifeEffect
          {
            Player = new ObjectReference { Kind = kind },
          },
        ],
      },
    ];
  }
}
