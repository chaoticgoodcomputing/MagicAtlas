namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// Parses the Ashling, Flame Dancer "You don't lose unspent [color] mana as
/// steps and phases end." oracle template: a permanent static ability that
/// exempts the controller's unspent mana of one named color from the
/// turn-based mana-emptying action.
///
/// <para>
/// CR 500.4 (verbatim): "As a step or phase begins, if there are effects that
/// last until that step or phase, those effects expire." (The mana pool
/// normally empties as steps/phases end; this ability overrides that for the
/// named color.)
/// </para>
///
/// <para>
/// Generalised over all five colors (only red and green are known printings —
/// Ashling, Flame Dancer and its green analogues — but the template is
/// color-parametric, so the rule accepts any of the five color words).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): matches the full clause sentence so this cannot fire
/// as a substring of a broader clause and cannot claim a substring of a
/// more-specific sibling.
/// </para>
/// </summary>
[StaticRule(Priority = 966)]
public sealed class RetainUnspentManaStaticRule : IStaticRule
{
  private static readonly IReadOnlyDictionary<string, ManaColor> _colorWords =
    new Dictionary<string, ManaColor>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = ManaColor.White,
      ["blue"] = ManaColor.Blue,
      ["black"] = ManaColor.Black,
      ["red"] = ManaColor.Red,
      ["green"] = ManaColor.Green,
    };

  private static readonly Regex _pattern = new(
    @"^\s*You\s+don'?t\s+lose\s+unspent\s+(?<color>white|blue|black|red|green)\s+mana\s+as\s+steps\s+and\s+phases\s+end\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    if (!_colorWords.TryGetValue(m.Groups["color"].Value, out var color))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new RetainUnspentManaEffect { Color = color },
        ],
      },
    ];
  }
}
