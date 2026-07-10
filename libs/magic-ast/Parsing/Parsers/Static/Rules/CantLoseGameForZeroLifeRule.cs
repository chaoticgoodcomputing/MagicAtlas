namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// Recognises "You don't lose the game for having 0 or less life." (Lich's Tomb) —
/// a static replacement that overrides the zero-or-less-life state-based loss
/// condition (CR 704.5a: a player with 0 or less life loses the game) for the
/// controller. Emits a single <see cref="StaticAbility"/> carrying one
/// <see cref="CantLoseGameForZeroLifeEffect"/> scoped to
/// <see cref="ObjectReferenceKind.You"/>.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantLoseGameForZeroLifeRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+don'?t\s+lose\s+the\s+game\s+for\s+having\s+0\s+or\s+less\s+life\.?\s*$",
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
          new CantLoseGameForZeroLifeEffect
          {
            Player = ObjectReference.You(),
          },
        ],
      },
    ];
  }
}
