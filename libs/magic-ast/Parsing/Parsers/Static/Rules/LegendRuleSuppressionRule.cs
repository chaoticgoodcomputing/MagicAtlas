namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// "The 'legend rule' doesn't apply." (Mirror Gallery, unscoped) / "The
/// 'legend rule' doesn't apply to creatures you control." (Council of Reeds,
/// scoped to creatures) / "The 'legend rule' doesn't apply to permanents you
/// control." (Mirror Box, scoped to permanents). Rule 704.5j: "If two or more
/// legendary permanents with the same name are controlled by the same player,
/// that player chooses one of them, and the rest are put into their owners'
/// graveyards. This is called the 'legend rule.'" All three surfaces emit a
/// <see cref="MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect"/>;
/// the optional trailing "to &lt;noun&gt; you control" clause populates that
/// effect's <c>Target</c> with a "&lt;noun&gt; you control" reference (reusing
/// the established <c>Kind=Each, Filter={CardTypes:[…],
/// Controller:You}</c> shape), narrowing the suppression's scope. The noun maps
/// to the corresponding card type — "creatures" → <c>creature</c>, "permanents"
/// → <c>permanent</c> (the permanent pseudo-type, CR 110.4a). Absence of the
/// clause leaves <c>Target</c> null — the original unscoped form. ANCHORED (^…$)
/// so no surface can match as a substring of a broader clause.
/// </summary>
[StaticRule(Priority = 981)]
public sealed class LegendRuleSuppressionRule : IStaticRule
{
  private static readonly Regex _legendRuleSuppressionPattern = new(
    @"^\s*The\s+[""""""]legend\s+rule[""""""]\s+doesn'?t\s+apply(?:\s+to\s+(?<noun>creatures|permanents)\s+you\s+control)?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _legendRuleSuppressionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // The scoped variants narrow the suppression to a filtered set of the
    // controller's permanents; the noun ("creatures" / "permanents") selects the
    // card type. Unscoped leaves Target null (Mirror Gallery's broad form).
    var cardType = match.Groups["noun"].Success
      ? match.Groups["noun"].Value.ToLowerInvariant() == "permanents" ? "permanent" : "creature"
      : null;

    var target = cardType is not null
      ? new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = [cardType], Controller = ControllerFilter.You },
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
