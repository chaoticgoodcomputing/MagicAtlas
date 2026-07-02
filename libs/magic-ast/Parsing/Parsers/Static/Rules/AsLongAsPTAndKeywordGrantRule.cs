namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Leading-form conditional continuous effect bundling a P/T modifier AND a keyword
/// grant under one shared <see cref="AsLongAsDuration"/>:
/// "As long as &lt;cond&gt;, it/this creature gets +N/+M and has &lt;keyword&gt;."
///
/// <para>
/// Both effects live on a single <see cref="StaticAbility"/> because they originate
/// from one oracle clause governed by one "as long as" condition (Rule 611 — the
/// continuous effect is created by the static ability and applies for as long as the
/// condition holds). Modelling them as one ability with two effects keeps the
/// condition single-sourced rather than duplicating the static ability per effect.
/// </para>
///
/// <para>
/// Sits at a higher priority than <see cref="AsLongAsStaticGrantRule"/> (Priority 968)
/// so the compound "gets +N/+M and has &lt;keyword&gt;" clause is recognised before
/// that rule's single-effect sub-parsers, whose end-anchored regexes decline the
/// compound text and would otherwise let the clause fall through to the fallback.
/// </para>
///
/// <para>
/// An ability-word prefix ("Threshold — …", Rule 207.2c) carries no rules meaning;
/// the classifier captures it into <see cref="ClauseClassification.AbilityWord"/> and
/// we record it as the descriptive label on the produced <see cref="StaticAbility"/>,
/// peeling the em-dash prefix before matching the leading pattern.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class AsLongAsPTAndKeywordGrantRule : IStaticRule
{
  // Leading form: "As long as <cond>, it/this creature gets +N/+M and has <keyword>."
  // <cond> is everything between "As long as " and the comma. The effect clause is the
  // self-referential subject ("it" / "this creature" / "this permanent") followed by
  // the signed P/T modifier, the conjunction "and has", and the keyword name. Both P/T
  // sides require an explicit '+' sign (oracle uses signed notation even for zero
  // modifiers). The keyword runs to end-of-clause (letters and spaces only).
  private static readonly Regex _asLongAsPtAndKeywordPattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>[^,]+),\s*(?:it|this\s+(?:creature|permanent))\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s+and\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Peel any ability-word em-dash prefix ("Threshold — ", Rule 207.2c) so the
    // leading pattern can anchor on "^\s*As\s+long\s+as". The classifier has already
    // captured the word into classification.AbilityWord; we record it on the produced
    // ability as a descriptive label only — it has no rules meaning.
    string? abilityWord = classification.AbilityWord;
    string bodyText = clause.RawText;
    if (abilityWord is not null)
    {
      var emDashIdx = bodyText.IndexOf('—');
      if (emDashIdx >= 0)
      {
        bodyText = bodyText[(emDashIdx + 1)..].TrimStart();
      }
    }

    var match = _asLongAsPtAndKeywordPattern.Match(bodyText);
    if (!match.Success)
    {
      return null;
    }

    var keyword = match.Groups["kw"].Value.Trim();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(keyword);
    if (grantedAbility is null)
    {
      // Recognised the compound shape but the keyword is not yet supported; decline so
      // the clause falls through to an honest fallback rather than emitting a partial buff.
      return null;
    }

    var condition = match.Groups["cond"].Value.Trim();
    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    // One duration instance shared by reference across both effects — they are gated
    // by the same condition (Rule 611.2 — a single continuous effect's set of
    // characteristics may include several modifications under one timestamp/condition).
    var duration = new AsLongAsDuration { Condition = MagicAST.Parsing.ConditionParser.Parse(condition) };

    return
    [
      new StaticAbility
      {
        AbilityWord = abilityWord,
        Effects =
        [
          new ModifyPTEffect
          {
            Target = ObjectReference.Self(),
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            Duration = duration,
          },
          new GainAbilityEffect
          {
            Target = ObjectReference.Self(),
            GainedAbility = grantedAbility,
            Duration = duration,
          },
        ],
      },
    ];
  }
}
