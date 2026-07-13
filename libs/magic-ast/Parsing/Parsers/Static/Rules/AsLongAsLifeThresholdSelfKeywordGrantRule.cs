namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "As long as your life total is less than or equal to half your starting life total,
/// [CardName] has [keyword]." — the God-template life-threshold conditional keyword grant
/// (Bane, Lord of Darkness: "As long as your life total is less than or equal to half your
/// starting life total, Bane has indestructible.").
///
/// <para>
/// CR 611.2 (conditional continuous effect): "Some continuous effects are conditional...
/// Such effects... apply only while the condition is true." CR 702.12a: "Indestructible is
/// a static ability." The leading condition clause is parsed by
/// <see cref="MagicAST.Parsing.ConditionParser"/> into a <see cref="QuantityComparisonCondition"/>
/// (a <see cref="MagicAST.AST.Quantities.DerivedQuantity"/> keyed on
/// <see cref="MagicAST.AST.Quantities.DerivedKind.LifeTotal"/> compared against half a
/// <see cref="MagicAST.AST.Quantities.DerivedKind.StartingLifeTotal"/>) and wrapped in an
/// <see cref="AsLongAsDuration"/>.
/// </para>
///
/// <para>
/// The subject "Bane" is the card referring to itself by its own name (CR 201.5: "A card's
/// name in its own text refers to that object"), so it resolves to <see cref="ObjectReference.Self"/>
/// — the literal name never rides into the AST. Distinct from the sibling
/// <see cref="AsLongAsStaticGrantRule"/>, whose leading-form sub-parsers require the subject
/// to be the pronoun "it" or "this creature"/"this permanent"; a card-name subject does not
/// match those patterns, so this dedicated rule is needed rather than extending that one.
/// </para>
///
/// <para>
/// Fully anchored (^…$) on the specific "life total is less than or equal to half [...]
/// starting life total" condition surface, so it cannot collide with any other "As long as
/// [condition], [Name] has [keyword]" shape using a different condition.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class AsLongAsLifeThresholdSelfKeywordGrantRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>your\s+life\s+total\s+is\s+(?:less|greater)\s+than(?:\s+or\s+equal\s+to)?\s+half\s+your\s+starting\s+life\s+total),\s*(?<name>[A-Z][^,]*?)\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var conditionText = match.Groups["cond"].Value.Trim();
    var keyword = match.Groups["kw"].Value.Trim();

    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(keyword);
    if (grantedAbility is null)
    {
      return null;
    }

    var duration = new AsLongAsDuration { Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText) };

    return
    [
      new StaticAbility
      {
        Effects =
        [
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
