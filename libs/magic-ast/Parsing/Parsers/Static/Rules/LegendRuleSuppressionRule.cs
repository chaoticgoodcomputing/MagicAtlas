namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// "The 'legend rule' doesn't apply." (Mirror Gallery, unscoped) / "The
/// 'legend rule' doesn't apply to creatures you control." (Council of Reeds,
/// scoped). Rule 704.5j: "If two or more legendary permanents with the same
/// name are controlled by the same player, that player chooses one of them,
/// and the rest are put into their owners' graveyards. This is called the
/// 'legend rule.'" Both surfaces emit a
/// <see cref="MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect"/>;
/// the optional trailing "to creatures you control" clause populates that
/// effect's <c>Target</c> with a "creatures you control" reference (reusing
/// the established <c>Kind=Each, Filter={CardTypes:["creature"],
/// Controller:You}</c> shape), narrowing the suppression's scope. Absence of
/// the clause leaves <c>Target</c> null — the original unscoped form.
/// ANCHORED (^…$) so neither surface can match as a substring of a broader
/// clause.
/// </summary>
[StaticRule(Priority = 981)]
public sealed class LegendRuleSuppressionRule : IStaticRule
{
  private static readonly Regex _legendRuleSuppressionPattern = new(
    @"^\s*The\s+[""""""]legend\s+rule[""""""]\s+doesn'?t\s+apply(?:\s+to\s+(?<scope>creatures\s+you\s+control))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _legendRuleSuppressionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var target = match.Groups["scope"].Success
      ? new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      }
      : null;

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect { Target = target }],
      },
    ];
  }
}
