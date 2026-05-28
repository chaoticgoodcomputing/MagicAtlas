namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 990)]
public sealed class EnchantedLandIsSubtypeRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _enchantedLandIsSubtypePattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Normalise to oracle-capitalised form (Island, Forest, etc.).
    var rawSubtype = match.Groups["subtype"].Value.Trim();
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..].ToLowerInvariant();

    // Guard: only basic land subtypes are handled here. Any other word falls
    // through to the fallback so the gap is surfaced rather than silently
    // misclassified as a subtype change.
    if (!_basicLandTypes.Contains(subtype))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new ChangeSubtypeEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          Subtypes = [subtype],
        }],
      },
    ];
  }

  // "Enchanted land is a(n) <Subtype>." — single basic land subtype declaration.
  // The article group is non-capturing; only <subtype> is needed.
  private static readonly Regex _enchantedLandIsSubtypePattern = new(
    @"^\s*Enchanted\s+land\s+is\s+an?\s+(?<subtype>[A-Z][a-z]+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Exhaustive set of CR 305.6 basic land subtypes. Used as a whitelist so
  // non-subtype words (e.g., a future card like "Enchanted land is a Desert.")
  // don't silently match before the fallback can surface the gap.
  private static readonly HashSet<string> _basicLandTypes =
  [
    "Plains",
    "Island",
    "Swamp",
    "Mountain",
    "Forest",
  ];
}
