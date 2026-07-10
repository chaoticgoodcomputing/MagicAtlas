namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Equip [cost]. This ability costs {N} less to activate for each other
/// Equipment you control." (Plate Armor) — the Equip activated ability (CR
/// 702.6a) plus a self-scoped, scaling cost reduction on that same ability.
/// Both sentences sit in ONE oracle-text paragraph (no line break between
/// them), so <see cref="MagicAST.Parsing.ClauseSplitter"/> hands the whole
/// thing to this rule as a single clause; the rule returns BOTH abilities the
/// paragraph describes.
///
/// <para>
/// CR 118.7 (cost reduction): "What a player actually needs to do to pay a
/// cost may be changed or reduced by effects." Unlike
/// <see cref="EquipAbilityYouActivateCostReductionRule"/> (Éowyn's
/// unqualified "Equip abilities you activate", which reduces EVERY Equip
/// ability the controller activates, including other Equipment's), "This
/// ability" refers only to the Equip ability defined earlier in the SAME
/// sentence — a self-scoped reduction. Per <see cref="CostReductionEffect"/>'s
/// docs, the self-only case leaves <c>AppliesTo</c> null (mirrors
/// <c>PrimevalProtector</c>'s "This spell costs {1} less to cast for each
/// creature your opponents control.", modeled as its own <c>StaticAbility</c>
/// with no <c>AppliesTo</c>) rather than referencing
/// <see cref="ActivatedAbilityReference"/> by keyword class.
/// </para>
///
/// <para>
/// "For each other Equipment you control" (CR 109.5, "another"/"other" excludes
/// the source) is <see cref="ObjectFilter.ExcludeSelf"/> combined with the
/// Equipment subtype and <see cref="ControllerFilter.You"/> — the same shape
/// used for other self-excluding count filters (e.g. "another creature you
/// control").
/// </para>
///
/// <para>
/// Both abilities drop their trailing/embedded reminder text (Rule 207.2),
/// matching the sibling convention that bare Equip reminders are not carried
/// into the gold AST (e.g. BrambleArmor's "Equip {4} (reminder)").
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000),
/// which would otherwise match just the "Equip {cost}" prefix (its combinator
/// invocation does not require consuming the clause to end) and silently drop
/// the trailing cost-reduction sentence. Mirrors <see cref="ReconfigureStaticRule"/>'s
/// documented reason for the same priority bump.
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class EquipCostReductionPerOtherEquipmentRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Equip\s+(?<equipcost>(?:\{[^}]+\})+)\.\s*"
      + @"This\s+ability\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+activate\s+"
      + @"for\s+each\s+other\s+Equipment\s+you\s+control\.?"
      + @"\s*(?:\([^)]*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ManaCostParser _manaCostParser = new();

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    ManaCost equipCost;
    try
    {
      var parsed = _manaCostParser.Parse(match.Groups["equipcost"].Value);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      equipCost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    var reductionAmount = int.Parse(match.Groups["amount"].Value);

    var equipAbility = new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Equip,
      Costs = [equipCost],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
    };

    var costReductionAbility = new StaticAbility
    {
      Effects =
      [
        new CostReductionEffect
        {
          Amount = LiteralQuantity.Of(reductionAmount),
          PerObject = new ObjectFilter
          {
            Subtypes = ["Equipment"],
            Controller = ControllerFilter.You,
            ExcludeSelf = true,
          },
        },
      ],
    };

    return [equipAbility, costReductionAbility];
  }
}
