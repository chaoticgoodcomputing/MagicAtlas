namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 958)]
public sealed class EnchantedCantAttackOrBlockRule : IStaticRule
{
  private const string PermanentTypeAlternation =
    @"(?:creature|artifact|planeswalker|enchantment|land|permanent)";

  private static readonly Regex _enchantedArrestPattern = new(
    $@"^\s*(?:Enchanted|Equipped)\s+{PermanentTypeAlternation}\s+can'?t\s+attack\s+or\s+block,\s+and\s+its\s+activated\s+abilities\s+can'?t\s+be\s+activated\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _enchantedCantAttackOrBlockPattern = new(
    $@"^\s*(?:Enchanted|Equipped)\s+{PermanentTypeAlternation}\s+can'?t\s+attack\s+or\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _enchantedCantAttackOnlyPattern = new(
    $@"^\s*(?:Enchanted|Equipped)\s+{PermanentTypeAlternation}\s+can'?t\s+attack\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    // Extended Arrest form: "... can't attack or block, and its activated abilities can't be activated."
    if (_enchantedArrestPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantAttackEffect { Target = target},
            new MagicAST.AST.Effects.Combat.CantBlockEffect { Target = target},
            new MagicAST.AST.Effects.Timing.CantActivateAbilitiesEffect { Target = target},
          ],
        },
      ];
    }

    // Basic Pacifism form: "... can't attack or block."
    if (_enchantedCantAttackOrBlockPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantAttackEffect { Target = target},
            new MagicAST.AST.Effects.Combat.CantBlockEffect { Target = target},
          ],
        },
      ];
    }

    // Bare attack-only form: "Enchanted creature can't attack." (no "or block").
    // Cessation (ULG) / Weight of Conscience (MOR) shape — the Aura restricts
    // attacking but leaves blocking unrestricted.
    if (_enchantedCantAttackOnlyPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new MagicAST.AST.Effects.Combat.CantAttackEffect { Target = target},
          ],
        },
      ];
    }

    return null;
  }
}
