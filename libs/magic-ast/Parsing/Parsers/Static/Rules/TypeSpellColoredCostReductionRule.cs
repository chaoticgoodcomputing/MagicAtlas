namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.Parsing;

/// <summary>
/// "&lt;Type&gt; spells you cast cost {&lt;colored symbols&gt;} less to cast. This effect
/// reduces only the amount of colored mana you pay." — Edgewalker and its cycle
/// (each two-color Cleric lord from Onslaught reduces the cost of the shared creature
/// type by its own guild's colored pip pair, e.g. Edgewalker's {W}{B}). Unlike
/// <see cref="TypeSpellCostReductionRule"/> (a single GENERIC digit), the reduction here
/// is one or more COLORED mana symbols — CR 118.7: "What a player actually needs to do
/// to pay a cost may be changed or reduced by effects."
///
/// <para>
/// Reusing <see cref="StaticRuleHelpers.BuildTypeSpellFilter"/> for the type/subtype
/// filter half (Rule 205.3 subtypes fall to the catch-all branch, matching Ballyrush
/// Banneret's "Kithkin spells" encoding) and the established
/// <see cref="CostReductionEffect.ManaSymbols"/> convention for the colored-only
/// reduction half (zero generic <c>Amount</c> + explicit symbols — the same shape as
/// <see cref="MagicAST.Parsing.Parsers.Static.Rules.AdditionalCostPayLifeReducesColoredCostRule"/>'s
/// "This effect reduces only the amount of [color] mana you pay" sentence). The closing
/// "This effect reduces only the amount of colored mana you pay." sentence needs no
/// separate node: putting the whole reduction on <c>ManaSymbols</c> with a zero generic
/// component already IS "reduces only colored mana" by construction. The trailing
/// parenthetical worked example ("(For example, if you cast a Cleric spell with mana
/// cost {1}{W}, it costs {1} to cast.)") carries no additional rules content beyond what
/// the effect already states, so it is matched but not separately modeled (reminder-adjacent).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent false-positive substring matches inside longer oracle lines
/// — the #1 FAIL class per the vertical-slice contract.
/// </para>
/// </summary>
[StaticRule(Priority = 984)]
public sealed class TypeSpellColoredCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var manaGroup = match.Groups["mana"].Value;

    var symbols = new List<ManaSymbol>();
    foreach (Match letterMatch in _manaLetterPattern.Matches(manaGroup))
    {
      if (!_manaLetterToSymbol.TryGetValue(letterMatch.Value, out var symbol))
      {
        return null;
      }

      symbols.Add(symbol);
    }

    if (symbols.Count == 0)
    {
      return null;
    }

    var affected = StaticRuleHelpers.BuildTypeSpellFilter(filterText);
    if (affected is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CostReductionEffect
          {
            // The whole reduction is the colored symbols; the generic component is 0 —
            // exactly what "This effect reduces only the amount of colored mana you pay"
            // states.
            Amount = LiteralQuantity.Of(0),
            ManaSymbols = symbols,
          },
        ],
        AffectedObjects = affected,
      },
    ];
  }

  // "Cleric spells you cast cost {W}{B} less to cast. This effect reduces only the
  // amount of colored mana you pay. (For example, if you cast a Cleric spell with mana
  // cost {1}{W}, it costs {1} to cast.)"
  // Anchored ^…$ to prevent substring false-positives. The filter noun is a single
  // capitalised word (no internal spaces), matching TypeSpellCostReductionRule's scope.
  // The trailing parenthetical example is matched loosely (its content is illustrative,
  // not rules-bearing) so future printings with different example numbers still anchor.
  private static readonly Regex _pattern = new(
    @"^\s*(?<filter>[A-Z][A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+(?<mana>(?:\{[WUBRG]\})+)\s+less\s+to\s+cast\.\s+"
      + @"This\s+effect\s+reduces\s+only\s+the\s+amount\s+of\s+colored\s+mana\s+you\s+pay\.\s+"
      + @"\(For\s+example,.*\)\s*$",
    RegexOptions.Compiled
  );

  private static readonly Regex _manaLetterPattern = new(@"[WUBRG]", RegexOptions.Compiled);

  private static readonly IReadOnlyDictionary<string, ManaSymbol> _manaLetterToSymbol =
    new Dictionary<string, ManaSymbol>(StringComparer.OrdinalIgnoreCase)
    {
      ["W"] = ManaSymbol.White,
      ["U"] = ManaSymbol.Blue,
      ["B"] = ManaSymbol.Black,
      ["R"] = ManaSymbol.Red,
      ["G"] = ManaSymbol.Green,
    };
}
