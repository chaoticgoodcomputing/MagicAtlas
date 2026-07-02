namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "You may have this creature assign its combat damage as though it weren't
/// blocked." (Pride of Lions). Rule 510.1c: a blocked creature normally assigns
/// its combat damage to the creatures blocking it; this static grants the
/// optional substitution to assign all combat damage as though unblocked.
/// Distinct from Trample (excess only) — see
/// <see cref="MagicAST.AST.Effects.Combat.AssignDamageAsUnblockedEffect"/>.
/// </summary>
[StaticRule(Priority = 957)]
public sealed class AssignDamageAsUnblockedRule : IStaticRule
{
  // The "You may have" prefix marks the optional form (Pride of Lions). The
  // unconditional form ("This creature assigns its combat damage as though it
  // weren't blocked.") is matched without the prefix; both arms share the same
  // assign-as-though-unblocked tail.
  private static readonly Regex _optionalPattern = new(
    @"^\s*You\s+may\s+have\s+this\s+(?:creature|permanent)\s+assign\s+its\s+combat\s+damage\s+as\s+though\s+it\s+weren'?t\s+blocked\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _unconditionalPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+assigns\s+its\s+combat\s+damage\s+as\s+though\s+it\s+weren'?t\s+blocked\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (_optionalPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.AssignDamageAsUnblockedEffect
            {
              IsOptional = true,
            },
          ],
        },
      ];
    }

    if (_unconditionalPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Combat.AssignDamageAsUnblockedEffect()],
        },
      ];
    }

    return null;
  }
}
