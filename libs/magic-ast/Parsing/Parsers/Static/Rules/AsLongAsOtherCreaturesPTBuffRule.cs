namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Leading-form conditional continuous P/T anthem: "As long as &lt;cond&gt;, other
/// creatures you control get +N/+M." (optionally prefixed by an ability word, Rule 207.2c —
/// Gavony Ironwright: "Fateful hour — As long as you have 5 or less life, other creatures
/// you control get +1/+4.").
///
/// <para>
/// CR 611.3: "Some continuous effects are... conditional continuous effects. Conditional
/// continuous effects... apply only while their condition is true." The condition clause is
/// parsed by <see cref="MagicAST.Parsing.ConditionParser"/> (here, a life-total threshold —
/// "you have 5 or less life" — resolving to a <see cref="QuantityComparisonCondition"/> over
/// <see cref="MagicAST.AST.Quantities.DerivedKind.LifeTotal"/>) and wrapped in an
/// <see cref="AsLongAsDuration"/>. The effect is a single <see cref="ModifyPTEffect"/>
/// targeting <see cref="ObjectReferenceKind.Each"/> creature the controller controls,
/// excluding the source itself (CR 109.5 "another" — the oracle "Other" qualifier maps to
/// <see cref="ObjectFilter.ExcludeSelf"/>).
/// </para>
///
/// <para>
/// A sibling of <see cref="AsLongAsPTAndKeywordGrantRule"/> (self-subject compound P/T +
/// keyword grant): this rule instead targets a FILTERED set of other creatures with a
/// plain P/T modifier only (no keyword grant), so the two do not collide. Also distinct
/// from <see cref="LordPTBuffRule"/>, which explicitly declines any clause containing
/// "as long as" and leaves that leading-condition form to this rule.
/// </para>
///
/// <para>
/// The ability-word prefix ("Fateful hour — ", Rule 207.2c, no rules meaning) is captured by
/// the classifier into <see cref="ClauseClassification.AbilityWord"/>; we record it as the
/// descriptive label on the produced <see cref="StaticAbility"/> and peel the em-dash prefix
/// before matching the leading "As long as" pattern. Anchored (^…$) so it cannot steal a
/// longer or differently-shaped clause.
/// </para>
/// </summary>
[StaticRule(Priority = 971)]
public sealed class AsLongAsOtherCreaturesPTBuffRule : IStaticRule
{
  // Leading form: "As long as <cond>, other creatures you control get +N/+M."
  // <cond> is everything between "As long as " and the comma. Both P/T sides require
  // an explicit sign (oracle uses signed notation).
  private static readonly Regex _pattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>[^,]+),\s*[Oo]ther\s+creatures\s+you\s+control\s+gets?\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Peel any ability-word em-dash prefix ("Fateful hour — ", Rule 207.2c) so the
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

    var match = _pattern.Match(bodyText);
    if (!match.Success)
    {
      return null;
    }

    var conditionText = match.Groups["cond"].Value.Trim();
    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var duration = new AsLongAsDuration
    {
      Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
    };

    return
    [
      new StaticAbility
      {
        AbilityWord = abilityWord,
        Effects =
        [
          new ModifyPTEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
                ExcludeSelf = true,
              },
            },
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            Duration = duration,
          },
        ],
      },
    ];
  }
}
