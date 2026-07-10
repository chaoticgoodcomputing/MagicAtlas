namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Your maximum hand size is ten." (The Ten Rings) and its symmetric
/// all-players sibling "Each player's maximum hand size is seven." — Rule
/// 402.2. The numeric-SET sibling of <see cref="NoMaxHandSizeRule"/>
/// ("You have no maximum hand size."): here the size is fixed to a printed
/// number rather than removed. Scopes
/// <see cref="MaxHandSizeEffect.Player"/> to
/// <see cref="ObjectReferenceKind.You"/> or
/// <see cref="ObjectReferenceKind.EachPlayer"/> depending on which subject
/// the clause names, mirroring <see cref="NoMaxHandSizeRule"/>'s subject
/// dispatch exactly.
/// </summary>
[StaticRule(Priority = 946)]
public sealed class MaxHandSizeRule : IStaticRule
{
  private static readonly IReadOnlyDictionary<string, int> NumberWords = new Dictionary<string, int>(
    StringComparer.OrdinalIgnoreCase)
  {
    ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
    ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    ["eleven"] = 11, ["twelve"] = 12,
  };

  private static readonly Regex _maxHandSizePattern = new(
    @"^\s*(?:(?<you>Your)|(?<players>Each\s+player's))\s+maximum\s+hand\s+size\s+is\s+(?<value>\d+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _maxHandSizePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var player = match.Groups["players"].Success
      ? new ObjectReference { Kind = ObjectReferenceKind.EachPlayer }
      : ObjectReference.You();

    var valueText = match.Groups["value"].Value;
    var value = NumberWords.TryGetValue(valueText, out var wordValue)
      ? wordValue
      : int.Parse(valueText);

    return
    [
      new StaticAbility
      {
        Effects = [new MaxHandSizeEffect { Player = player, Value = value }],
      },
    ];
  }
}
