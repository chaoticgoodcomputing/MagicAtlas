namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// Recognises the land-play lock static "You can't play lands." (Aggressive
/// Mining) and its symmetric/asymmetric siblings "Players can't play lands." /
/// "Your opponents can't play lands." CR 305.1: "A player who has priority may
/// play a land card from their hand during a main phase of their turn when the
/// stack is empty. Playing a land is a special action; it doesn't use the
/// stack." CR 116.2a describes the default once-per-turn special action this
/// restriction overrides entirely for the scoped player(s). Emits a single
/// <see cref="StaticAbility"/> carrying one <see cref="CantPlayLandsEffect"/>,
/// scoped via <see cref="ObjectReferenceKind.You"/> / <see cref="ObjectReferenceKind.EachPlayer"/> /
/// <see cref="ObjectReferenceKind.EachOpponent"/> — mirroring the
/// <see cref="CantDrawCardsRule"/> / <see cref="CantGainLifeRule"/> convention.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantPlayLandsRule : IStaticRule
{
  private static readonly Regex _youPattern = new(
    @"^\s*You\s+can'?t\s+play\s+lands\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _playersPattern = new(
    @"^\s*Players\s+can'?t\s+play\s+lands\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _opponentsPattern = new(
    @"^\s*Your\s+opponents\s+can'?t\s+play\s+lands\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    ObjectReferenceKind kind;
    if (_youPattern.IsMatch(clause.RawText))
    {
      kind = ObjectReferenceKind.You;
    }
    else if (_playersPattern.IsMatch(clause.RawText))
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
          new CantPlayLandsEffect
          {
            Player = new ObjectReference { Kind = kind },
          },
        ],
      },
    ];
  }
}
