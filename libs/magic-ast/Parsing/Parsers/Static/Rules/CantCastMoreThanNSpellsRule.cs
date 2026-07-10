namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// Recognises the spell-cast-cap static "Each player can't cast more than one
/// spell each turn." (Eidolon of Rhetoric, Arcane Laboratory) and its
/// single-player-scoped variant "You can't cast more than one spell each turn."
/// (Moderation). CR 601.3a: a player who wants to cast a spell follows the steps
/// of casting it, and a continuous effect such as this one restricts whether
/// that's legal (CR 611.1: a continuous effect "affects players or the rules of
/// the game, for a fixed or indefinite period") — NOT a per-spell "can't be cast"
/// restriction. Emits a single <see cref="StaticAbility"/> carrying one
/// <see cref="CantCastMoreThanNSpellsEffect"/>; the cap, scope, and counting
/// window are structured fields, never free text.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantCastMoreThanNSpellsRule : IStaticRule
{
  private static readonly Regex _capPattern = new(
    @"^\s*(?<subject>Each\s+player|You)\s+can'?t\s+cast\s+more\s+than\s+(?<count>one|two|three|\d+)\s+spell(?:s)?\s+each\s+turn\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _capPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var count = ParseCount(match.Groups["count"].Value);
    if (count is null)
    {
      return null;
    }

    var player = match.Groups["subject"].Value.Trim().Equals("You", StringComparison.OrdinalIgnoreCase)
      ? ObjectReference.You()
      : new ObjectReference { Kind = ObjectReferenceKind.EachPlayer };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantCastMoreThanNSpellsEffect
          {
            Player = player,
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
