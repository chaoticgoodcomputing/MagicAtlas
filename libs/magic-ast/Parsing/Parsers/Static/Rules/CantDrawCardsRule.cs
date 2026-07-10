namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// Recognises the global draw-lock static "Players can't draw cards." (Maralen
/// of the Mornsong). A rules-of-the-game continuous effect (CR 611.1) written as
/// a plain static statement (CR 604.1) that stops a player or players from
/// drawing cards — see CR 120 (draw) for the drawing rule this restriction
/// overrides. Emits a single <see cref="StaticAbility"/> carrying one
/// <see cref="CantDrawCardsEffect"/>, scoped to
/// <see cref="ObjectReferenceKind.EachPlayer"/> for the symmetric "Players" case
/// — mirroring the <see cref="CantGainLifeRule"/> convention (the
/// <see cref="CantDrawCardsEffect.Player"/> field is left generalised so an
/// asymmetric variant such as "Your opponents can't draw cards" can reuse this
/// node with a different <see cref="ObjectReference"/> scope, should such a
/// pattern be added later).
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantDrawCardsRule : IStaticRule
{
  private static readonly Regex _playersPattern = new(
    @"^\s*Players\s+can'?t\s+draw\s+cards\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_playersPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantDrawCardsEffect
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
          },
        ],
      },
    ];
  }
}
