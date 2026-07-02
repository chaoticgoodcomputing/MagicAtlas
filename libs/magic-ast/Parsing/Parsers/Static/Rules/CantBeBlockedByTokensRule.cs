namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Filtered evasion by token status: "This creature can't be blocked by
/// creature tokens." — Rule 509.1b (the defending player checks each
/// creature they control for restrictions on blocking; a restriction being
/// disobeyed makes the blocker declaration illegal).
/// </summary>
/// <remarks>
/// A "token" is not a card type (Rule 111.1: "A token is a marker used to
/// represent any permanent that isn't represented by a card") — it is a
/// separate predicate axis on the object, so "creature tokens" is encoded as
/// <see cref="ObjectFilter.CardTypes"/> = ["creature"] combined with
/// <see cref="ObjectFilter.IsToken"/> = true, never as a card type or
/// subtype string. New file (not an edit to the sibling
/// <see cref="CantBeBlockedRule"/>) to stay collision-free: that rule's
/// subtype arm requires a single capitalized word and cannot match the
/// lowercase two-word "creature tokens" phrase.
/// </remarks>
[StaticRule(Priority = 956)]
public sealed class CantBeBlockedByTokensRule : IStaticRule
{
  private static readonly Regex _cantBeBlockedByTokensPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+by\s+creature\s+tokens\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Token-restricted variant: "This creature can't be blocked by creature
    // tokens." — Rule 509.1b. The filter subject is creatures that are
    // tokens (Rule 111.1), so IsToken = true is combined with the implicit
    // "creature" card type, mirroring the color/subtype arms of the sibling
    // CantBeBlockedRule.
    if (_cantBeBlockedByTokensPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantBeBlockedEffect
            {
              BlockedByFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                IsToken = true,
              },
            },
          ],
        },
      ];
    }

    return null;
  }
}
