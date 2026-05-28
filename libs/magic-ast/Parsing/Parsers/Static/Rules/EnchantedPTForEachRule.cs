namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 973)]
public sealed class EnchantedPTForEachRule : IStaticRule
{
  // "(Enchanted|Equipped) creature gets +N/+M for each <filter> you control."
  // Mirrors _selfPTForEachPattern but matches the Enchanted/Equipped subject prefix
  // instead of "This creature". The filter capture includes the "you control" suffix.
  private static readonly Regex _enchantedPTForEachPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>.+?\s+you\s+control)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _enchantedPTForEachPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;

    // Only handle multiplier-1 increments — cards with higher per-item
    // increments are a distinct shape and should fall through to the fallback.
    if (Math.Abs(power) > 1 || Math.Abs(toughness) > 1)
    {
      return null;
    }

    var filterPhrase = match.Groups["filter"].Value.Trim();

    MagicAST.AST.Quantities.Quantity powerModifier = power == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = filterPhrase };

    MagicAST.AST.Quantities.Quantity toughnessModifier = toughness == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = filterPhrase };

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          PowerModifier = powerModifier,
          ToughnessModifier = toughnessModifier,
        }],
      },
    ];
  }
}
