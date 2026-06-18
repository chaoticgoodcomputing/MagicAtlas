namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

/// <summary>
/// "This creature has [keyword] during your turn." — the SUFFIX-condition form of a
/// conditional self-keyword ability (CR 702.7 for first strike, etc.). The condition
/// phrase "during your turn" follows the keyword statement rather than preceding it,
/// which differs from the PREFIX form handled by <see cref="ConditionalSelfKeywordRule"/>
/// ("During your turn, this creature has [keyword]").
///
/// <para>
/// Razorkin Needlehead is the canonical example: "This creature has first strike
/// during your turn." produces the same AST as Fresh-Faced Recruit's prefix form
/// "During your turn, this creature has first strike." — a <see cref="StaticAbility"/>
/// with <c>KeywordSource</c> set and an <see cref="MagicAST.AST.Abilities.OtherCondition"/>
/// carrying "During your turn" (PB-7 structured-condition bucket; the turn-phase
/// condition is entangled with the reference-not-resolution engine contract).
/// </para>
///
/// <para>
/// CR 702.7a: "First strike is a static ability that modifies the rules for the
/// combat damage step." The condition "during your turn" limits when this static
/// ability is in effect.
/// </para>
///
/// <para>
/// ANCHOR note: the regex is anchored (^…$) — the subject phrase "this creature"
/// / "this permanent" cannot appear as a substring inside a more-specific sibling
/// trigger or effect clause. The condition phrase "during your turn" is equally
/// anchored to the END of the clause after the keyword, preventing false positives
/// on cost-reduction clauses like "this spell costs {1} less to cast during your
/// turn" (which start with "this spell costs", a distinct prefix).
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class SelfKeywordDuringTurnSuffixRule : IStaticRule
{
  // "This creature has first strike during your turn."
  // "This permanent has vigilance during your turn."
  // Anchored: ^ and $ prevent substring matches.
  private static readonly Regex _pattern = new(
    @"^\s*[Tt]his\s+(?:creature|permanent)\s+has\s+(?<kw>\w+(?:\s+\w+)?)\s+(?<cond>during\s+your\s+turn)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var kw = match.Groups["kw"].Value.Trim();
    // Capitalise "During" so the condition text matches the prefix-form convention
    // (ConditionalSelfKeywordRule normalises to "During your turn").
    var conditionText = "During your turn";

    var keywordAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (keywordAbility is null)
    {
      return null;
    }

    return
    [
      keywordAbility with
      {
        Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
      },
    ];
  }
}
