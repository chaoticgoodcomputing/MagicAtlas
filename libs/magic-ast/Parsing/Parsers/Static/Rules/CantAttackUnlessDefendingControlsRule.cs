namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;

[StaticRule(Priority = 956)]
public sealed class CantAttackUnlessDefendingControlsRule : IStaticRule
{
  private static readonly Regex _cantAttackUnlessDefendingControlsPattern = new(
    @"^\s*(?:This\s+creature|\S+(?:\s+\S+)*?)\s+can'?t\s+attack\s+unless\s+defending\s+player\s+controls\s+an?\s+(?<land>[A-Z][a-z]+)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _cantAttackUnlessDefendingControlsPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var landType = match.Groups["land"].Value;
    // Normalise capitalisation: oracle text always capitalises basic land type names
    // (Island, Forest, Swamp, Mountain, Plains) — pass through verbatim so the filter
    // Subtypes value matches the oracle-text spelling (e.g. "Island" not "island").
    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Combat.CantAttackEffect
          {
            UnlessDefendingControls = new EvasionCondition
            {
              ConditionType = EvasionConditionType.DefendingPlayerControls,
              PermanentFilter = new ObjectFilter { Subtypes = [landType] },
            },
          },
        ],
      },
    ];
  }
}
