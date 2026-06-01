namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// Recognises the spell-cast-cap static "Each player can't cast more than one
/// spell each turn." (Eidolon of Rhetoric, Arcane Laboratory). A rules-of-the-game
/// continuous effect (CR 611.1) that caps cast events (CR 601.2) per player per
/// turn — NOT a per-spell "can't be cast" restriction. Emits a single
/// <see cref="StaticAbility"/> carrying one
/// <see cref="CantCastMoreThanNSpellsEffect"/>; the cap, scope, and counting
/// window are structured fields, never free text.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantCastMoreThanNSpellsRule : IStaticRule
{
  private static readonly Regex _eachPlayerCapPattern = new(
    @"^\s*Each\s+player\s+can'?t\s+cast\s+more\s+than\s+(?<count>one|two|three|\d+)\s+spell(?:s)?\s+each\s+turn\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _eachPlayerCapPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var count = ParseCount(match.Groups["count"].Value);
    if (count is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantCastMoreThanNSpellsEffect
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
            MaxPerPeriod = count.Value,
            Period = SpellCastLimitPeriod.EachTurn,
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses the cap token, which is printed as a number word ("one") on the live
  /// cards in this family but tolerated as a digit for robustness.
  /// </summary>
  private static int? ParseCount(string token) =>
    token.ToLowerInvariant() switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.TryParse(token, out var n) ? n : null,
    };
}
