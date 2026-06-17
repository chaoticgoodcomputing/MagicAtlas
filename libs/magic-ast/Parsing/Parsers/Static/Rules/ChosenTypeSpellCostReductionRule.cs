namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// "Spells you cast of the chosen type cost {N} less to cast." — a cost-reduction static
/// ability whose affected set is filtered by the card type chosen as this permanent entered
/// (Cloud Key shape).
///
/// <para>This is the consumer half of a CR 607 linked ability pair: the producer is the
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseCardTypeEffect"/> under
/// <c>StaticAbility.When = AsThisEnters</c> (Cloud Key, line 1). The "of the chosen type"
/// clause is encoded as <see cref="ObjectFilter.ChosenCharacteristic"/> =
/// <see cref="ChosenCharacteristicKind.CardType"/>, never free-text.</para>
///
/// <para>Distinct from <see cref="TypeSpellCostReductionRule"/> (a fixed printed card type
/// like "Creature spells you cast cost {1} less") and from
/// <see cref="ChosenTypeAnthemModifyPTRule"/> (which reads a chosen creature type for a P/T
/// buff, not a chosen card type for a cost reduction). The pattern anchors on "of the chosen
/// type" to distinguish it from the fixed-type family.</para>
/// </summary>
[StaticRule(Priority = 986)]
public sealed class ChosenTypeSpellCostReductionRule : IStaticRule
{
  // "Spells you cast of the chosen type cost {N} less to cast."
  private static readonly Regex _chosenTypeSpellCostReductionPattern = new(
    @"^\s*Spells\s+you\s+cast\s+of\s+the\s+chosen\s+type\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chosenTypeSpellCostReductionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.You,
          ChosenCharacteristic = ChosenCharacteristicKind.CardType,
        },
      },
    ];
  }
}
