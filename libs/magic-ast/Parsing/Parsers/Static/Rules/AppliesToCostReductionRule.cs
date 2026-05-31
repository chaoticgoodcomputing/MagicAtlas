namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Cost reductions that apply to a <i>class of other abilities/spells</i> the
/// enchanted permanent is the target of — Strong Back's
/// "Equip abilities you activate that target enchanted creature cost {3} less to
/// activate." and "Aura spells you cast that target enchanted creature cost {3}
/// less to cast." (ADR 0003 follow-up 1: the surviving keyword identity is what
/// a reference filter matches on).
///
/// <para>
/// Distinct from <see cref="TypeSpellCostReductionRule"/> (self/spell-on-stack
/// reductions carried via <c>AffectedObjects</c>): here the reduction targets a
/// referenced <i>ability or spell</i> via <see cref="CostReductionEffect.AppliesTo"/>,
/// a typed <see cref="AbilityReference"/>. The "that target enchanted creature"
/// clause becomes <see cref="AbilityReference.TargetsObject"/> =
/// <c>EnchantedOrEquipped</c>.
/// </para>
/// </summary>
[StaticRule(Priority = 990)]
public sealed class AppliesToCostReductionRule : IStaticRule
{
  // "Equip abilities you activate that target enchanted creature cost {N} less to activate."
  private static readonly Regex _equipAbilities = new(
    @"^\s*Equip\s+abilities\s+you\s+activate\s+that\s+target\s+enchanted\s+creature\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+activate\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "<Subtype> spells you cast that target enchanted creature cost {N} less to cast."
  private static readonly Regex _typeSpells = new(
    @"^\s*(?<filter>[A-Z][A-Za-z]+)\s+spells\s+you\s+cast\s+that\s+target\s+enchanted\s+creature\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var equipMatch = _equipAbilities.Match(clause.RawText);
    if (equipMatch.Success)
    {
      return Build(
        int.Parse(equipMatch.Groups["amount"].Value),
        new ActivatedAbilityReference
        {
          Keyword = KeywordAbility.Equip,
          Controller = ControllerFilter.You,
          TargetsObject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        }
      );
    }

    var spellMatch = _typeSpells.Match(clause.RawText);
    if (spellMatch.Success)
    {
      var filter = StaticRuleHelpers.BuildTypeSpellFilter(spellMatch.Groups["filter"].Value.Trim());
      if (filter is null)
      {
        return null;
      }
      // BuildTypeSpellFilter roots the filter at CardTypes:["spell"]; the
      // SpellReference is itself a spell reference, so strip the redundant
      // "spell" card type and carry only the discriminating subtype + controller.
      var spellFilter = new ObjectFilter
      {
        Subtypes = filter.Subtypes,
        Controller = filter.Controller,
      };
      return Build(
        int.Parse(spellMatch.Groups["amount"].Value),
        new SpellReference
        {
          Filter = spellFilter,
          TargetsObject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        }
      );
    }

    return null;
  }

  private static IReadOnlyList<Ability> Build(int amount, AbilityReference appliesTo) =>
  [
    new StaticAbility
    {
      Effects = [new CostReductionEffect
      {
        Amount = LiteralQuantity.Of(amount),
        AppliesTo = appliesTo,
      }],
    },
  ];
}
