namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers.Static;

/// <summary>
/// "As an additional cost to cast [color] [type] spells, you may pay N life.
/// Those spells cost {[colored]} less to cast if you paid life this way. This
/// effect reduces only the amount of [color] mana you pay." — the Defiler
/// cycle from March of the Machine (Defiler of Instinct and siblings). A
/// single continuous static ability (CR 604.1) that both grants an OPTIONAL
/// additional cost for a filtered class of the controller's future spells and
/// conditionally reduces those same spells' cost when the additional cost was
/// paid.
///
/// <para>
/// CR 601.2f (verbatim): "The player determines the total cost of the spell.
/// Usually this is just the mana cost. Some spells have additional or
/// alternative costs. ... The total cost is the mana cost or alternative cost
/// (as determined in rule 601.2b), plus all additional costs and cost
/// increases, and minus all cost reductions."
/// </para>
///
/// <para>
/// The three sentences are ONE oracle-text paragraph (<see cref="ClauseSplitter"/>
/// keeps a paragraph as a single clause), so this rule matches the whole span
/// with a single anchored (^…$) pattern rather than three separate rules. The
/// two structured halves: <see cref="GrantAdditionalCostEffect"/> (the "you
/// may pay N life" grant, carrying its own <c>AffectedSpells</c> filter per
/// <see cref="GrantAlternativeCostEffect"/> convention) and
/// <see cref="CostReductionEffect"/> (the "{colored} less … if you paid life
/// this way" reduction, gated by <see cref="AdditionalCostPaidCondition"/> and
/// carrying the colored-only reduction in <see cref="CostReductionEffect.ManaSymbols"/>
/// with a zero generic <c>Amount</c> — the third sentence, "This effect
/// reduces only the amount of [color] mana you pay," is exactly what putting
/// the reduction on <c>ManaSymbols</c> rather than a generic amount already
/// encodes, so it needs no separate node).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent false-positive substring matches inside longer
/// oracle lines — the #1 FAIL class per the vertical-slice contract. Color and
/// spell-class type are both captured so the rule generalises across the
/// cycle (Defiler of Instinct is red/permanent; siblings vary both axes).
/// </para>
/// </summary>
[StaticRule(Priority = 986)]
public sealed class AdditionalCostPayLifeReducesColoredCostRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var colorName = match.Groups["color"].Value;
    var typeNoun = match.Groups["type"].Value;
    var lifeAmount = int.Parse(match.Groups["life"].Value);
    var reductionManaLetter = match.Groups["mana"].Value;
    var reductionColorName = match.Groups["manacolor"].Value;

    if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
    {
      return null;
    }

    if (!_colorNameToCode.TryGetValue(reductionColorName, out var reductionColorCode)
        || !string.Equals(reductionColorCode, reductionManaLetter, StringComparison.OrdinalIgnoreCase))
    {
      // The closing sentence must name the same color as the {symbol} reduction —
      // an honest fallback if a future cycle entry breaks that invariant.
      return null;
    }

    if (!_manaLetterToSymbol.TryGetValue(reductionManaLetter, out var reductionSymbol))
    {
      return null;
    }

    if (!_typeNounToCardType.TryGetValue(typeNoun, out var cardType))
    {
      return null;
    }

    var affectedSpells = new ObjectFilter
    {
      CardTypes = ["spell", cardType],
      Colors = [colorCode],
      Controller = ControllerFilter.You,
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GrantAdditionalCostEffect
          {
            AdditionalCost = new AdditionalCost
            {
              Cost = new PayLifeCost { Amount = LiteralQuantity.Of(lifeAmount) },
              IsOptional = true,
            },
            AffectedSpells = affectedSpells,
          },
          new CostReductionEffect
          {
            // The whole reduction is the colored symbol; the generic component is 0.
            Amount = LiteralQuantity.Of(0),
            ManaSymbols = [reductionSymbol],
            Condition = new AdditionalCostPaidCondition(),
          },
        ],
        AffectedObjects = affectedSpells,
      },
    ];
  }

  // "As an additional cost to cast red permanent spells, you may pay 2 life.
  // Those spells cost {R} less to cast if you paid life this way. This effect
  // reduces only the amount of red mana you pay."
  // Anchored ^…$ to prevent substring false-positives; optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*As\s+an\s+additional\s+cost\s+to\s+cast\s+(?<color>White|Blue|Black|Red|Green)\s+"
      + @"(?<type>permanent|creature|artifact|enchantment|planeswalker|instant|sorcery|land|battle)\s+spells,\s+"
      + @"you\s+may\s+pay\s+(?<life>\d+)\s+life\.\s+"
      + @"Those\s+spells\s+cost\s+\{(?<mana>[WUBRG])\}\s+less\s+to\s+cast\s+if\s+you\s+paid\s+life\s+this\s+way\.\s+"
      + @"This\s+effect\s+reduces\s+only\s+the\s+amount\s+of\s+(?<manacolor>white|blue|black|red|green)\s+mana\s+you\s+pay\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["White"] = "W",
      ["Blue"] = "U",
      ["Black"] = "B",
      ["Red"] = "R",
      ["Green"] = "G",
    };

  private static readonly IReadOnlyDictionary<string, ManaSymbol> _manaLetterToSymbol =
    new Dictionary<string, ManaSymbol>(StringComparer.OrdinalIgnoreCase)
    {
      ["W"] = ManaSymbol.White,
      ["U"] = ManaSymbol.Blue,
      ["B"] = ManaSymbol.Black,
      ["R"] = ManaSymbol.Red,
      ["G"] = ManaSymbol.Green,
    };

  // Spell-class type nouns this cycle uses, mapped onto the CardTypes token
  // appended after the "spell" root (matching the established ["spell", type]
  // convention — e.g. ColoredCreatureSpellCostReductionRule's ["spell",
  // "creature"]). "permanent" is the codebase's established pseudo-type for
  // "any permanent-producing card type" (see Boomerang's target filter).
  private static readonly IReadOnlyDictionary<string, string> _typeNounToCardType =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["permanent"] = "permanent",
      ["creature"] = "creature",
      ["artifact"] = "artifact",
      ["enchantment"] = "enchantment",
      ["planeswalker"] = "planeswalker",
      ["instant"] = "instant",
      ["sorcery"] = "sorcery",
      ["land"] = "land",
      ["battle"] = "battle",
    };
}
