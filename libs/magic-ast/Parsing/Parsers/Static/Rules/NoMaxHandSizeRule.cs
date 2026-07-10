namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "You have no maximum hand size." (Graceful Adept) and its symmetric
/// all-players sibling "Players have no maximum hand size." (Price of
/// Knowledge) — Rule 402.2. Both share the same effect node
/// (<see cref="MagicAST.AST.Effects.Keyword.NoMaxHandSizeEffect"/>); this rule
/// scopes <see cref="MagicAST.AST.Effects.Keyword.NoMaxHandSizeEffect.Player"/>
/// to <see cref="ObjectReferenceKind.You"/> or
/// <see cref="ObjectReferenceKind.EachPlayer"/> depending on which subject the
/// clause names.
/// </summary>
[StaticRule(Priority = 947)]
public sealed class NoMaxHandSizeRule : IStaticRule
{
  private static readonly Regex _noMaxHandSizePattern = new(
    @"^\s*(?<subject>You|Players)\s+have\s+no\s+maximum\s+hand\s+size\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _noMaxHandSizePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subject = match.Groups["subject"].Value;
    var player = subject.Equals("Players", StringComparison.OrdinalIgnoreCase)
      ? new ObjectReference { Kind = ObjectReferenceKind.EachPlayer }
      : ObjectReference.You();

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Keyword.NoMaxHandSizeEffect { Player = player }],
      },
    ];
  }
}
