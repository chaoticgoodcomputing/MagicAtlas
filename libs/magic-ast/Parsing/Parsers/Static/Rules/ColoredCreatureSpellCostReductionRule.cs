namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Color] creature spells you cast cost {N} less to cast." — the Amonkhet Monument
/// cycle (Oketra's Monument, Bontu's Monument, Kefnet's Monument, Rhonas's Monument,
/// Hazoret's Monument). Each reduces the cost of spells that are both a specific colour
/// AND a creature spell.
///
/// <para>
/// CR 118.7: "What a player actually needs to do to pay a cost may be changed or reduced
/// by effects." CR 118.7a: "Effects that reduce a cost by an amount of generic mana affect
/// only the generic mana component of that cost."
/// </para>
///
/// <para>
/// Sits above <see cref="TypeSpellCostReductionRule"/> (priority 984) and
/// <see cref="ConjunctiveTypeSpellCostReductionRule"/> (priority 983) because the
/// two-word colour+type qualifier would otherwise never reach those rules. Anchored
/// (^…$) to prevent false-positive substring matches inside longer oracle lines.
/// </para>
/// </summary>
[StaticRule(Priority = 982)]
public sealed class ColoredCreatureSpellCostReductionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var colorName = match.Groups["color"].Value;
    var amount = int.Parse(match.Groups["amount"].Value);

    if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Resource.CostReductionEffect
          {
            Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          },
        ],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell", "creature"],
          Colors = [colorCode],
          Controller = ControllerFilter.You,
        },
      },
    ];
  }

  // "White creature spells you cast cost {1} less to cast."
  // Anchored ^…$ to prevent substring false-positives (CR 118.7a).
  // The color word is always a single capitalised English colour name.
  private static readonly Regex _pattern = new(
    @"^\s*(?<color>White|Blue|Black|Red|Green)\s+creature\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
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
}
