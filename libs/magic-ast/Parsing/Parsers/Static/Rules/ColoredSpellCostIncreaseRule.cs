namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Color] spells you cast cost {[colored]} more to cast." — the Nemesis "Leech" cycle
/// (Ruby Leech, Sapphire Leech, Jade Leech, Alabaster Leech, Andradite Leech) and Derelor.
/// Each self-taxes a colour-filtered class of spells the CONTROLLER casts, where the
/// increase is a specific COLORED mana symbol {W}/{U}/{B}/{R}/{G} — never generic.
///
/// <para>
/// A spell's total cost is "locked in" before payments are made (CR 601.2) and the caster
/// pays the mana component of that total (CR 118.7). Because colored ≠ generic (CR 601.2's
/// Altar's Reap example distinguishes {B} from {1}), the colored increase is carried in
/// <see cref="MagicAST.AST.Effects.Resource.CostIncreaseEffect.ManaSymbols"/> with a zero
/// generic Amount, rather than being flattened to {1}.
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent false-positive substring matches inside longer oracle lines.
/// The colour WORD and the {mana} LETTER are captured independently (they coincide on all
/// six corpus cards but the shape does not require it).
/// </para>
/// </summary>
[StaticRule(Priority = 982)]
public sealed class ColoredSpellCostIncreaseRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var colorName = match.Groups["color"].Value;
    var manaLetter = match.Groups["mana"].Value;

    if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
    {
      return null;
    }

    if (!_manaLetterToSymbol.TryGetValue(manaLetter, out var manaSymbol))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Resource.CostIncreaseEffect
          {
            // The whole increase is the colored symbol; the generic component is 0.
            Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(0),
            ManaSymbols = [manaSymbol],
          },
        ],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Colors = [colorCode],
          Controller = ControllerFilter.You,
        },
      },
    ];
  }

  // "Red spells you cast cost {R} more to cast."
  // Anchored ^…$ to prevent substring false-positives; optional trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*(?<color>White|Blue|Black|Red|Green)\s+spells\s+you\s+cast\s+cost\s+\{(?<mana>[WUBRG])\}\s+more\s+to\s+cast\.?\s*$",
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
}
