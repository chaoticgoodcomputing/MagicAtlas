namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// Recognises the hidden-information-reveal static "Players play with their hands
/// revealed." (Revelation) and the asymmetric "Your opponents play with their
/// hands revealed." (Telepathy — the CR 701.20 rule text's own worked example of
/// this phrasing). A continuous public-information effect (CR 604.2, CR 701.20a)
/// that keeps the affected player(s)' hand face-up and visible to all players.
/// Emits a single <see cref="StaticAbility"/> carrying one
/// <see cref="PlayWithHandsRevealedEffect"/>, scoped to
/// <see cref="ObjectReferenceKind.EachPlayer"/> for the symmetric "Players" case
/// or <see cref="ObjectReferenceKind.EachOpponent"/> for the "Your opponents"
/// case — the same symmetric/asymmetric player-scope shape as the sibling
/// <see cref="CantGainLifeRule"/>. Both patterns are fully anchored (^…$) so
/// neither can match as a substring of a longer, more complex ability line.
/// </summary>
[StaticRule]
public sealed class PlayWithHandsRevealedRule : IStaticRule
{
  private static readonly Regex _playersPattern = new(
    @"^\s*Players\s+play\s+with\s+their\s+hands\s+revealed\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _opponentsPattern = new(
    @"^\s*Your\s+opponents\s+play\s+with\s+their\s+hands\s+revealed\.?\s*$",
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
          new PlayWithHandsRevealedEffect
          {
            Player = new ObjectReference { Kind = kind },
          },
        ],
      },
    ];
  }
}
