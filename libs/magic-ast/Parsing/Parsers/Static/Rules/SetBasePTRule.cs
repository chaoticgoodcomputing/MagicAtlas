namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "(Enchanted|Equipped) creature has base power and toughness N/M." — a layer-7b
/// continuous effect (CR 613.4: "Layer 7b: Effects that set power and/or toughness
/// to a specific number or value are applied.") from an Aura's or Equipment's own
/// static ability that overwrites the attached creature's base P/T box with a fixed
/// value. Anchored tightly (no trailing "until end of turn"/"and gains ..." clause,
/// no "target creature" subject) so it only recognises the plain always-on Aura/
/// Equipment shape — corpus-swept against every "has base power and toughness"
/// surface (Reduce in Stature, Illusory Wrappings, Belt of Giant Strength, and the
/// Wicked half of Wicked // Cursed all share this exact anchored line; every other
/// "base power and toughness" card in the corpus carries additional clauses/subjects
/// this pattern deliberately does not match).
/// </summary>
[StaticRule(Priority = 974)]
public sealed class SetBasePTRule : IStaticRule
{
  // "(Enchanted|Equipped) creature has base power and toughness N/M."
  private static readonly Regex _setBasePTPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+has\s+base\s+power\s+and\s+toughness\s+(?<p>\d+)/(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _setBasePTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new SetBasePTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          Power = LiteralQuantity.Of(power),
          Toughness = LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
