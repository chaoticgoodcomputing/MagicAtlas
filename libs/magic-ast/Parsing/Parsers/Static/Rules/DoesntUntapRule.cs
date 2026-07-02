namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 994)]
public sealed class DoesntUntapRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Self-reference form: "This [type] doesn't untap during [your|its controller's] untap step."
    var selfMatch = Regex.Match(
      clause.RawText,
      @"^\s*This\s+(?:permanent|creature|artifact|enchantment|land)\s+doesn'?t\s+untap\s+during\s+(?<possessive>your|its\s+controller'?s)\s+untap\s+step\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (selfMatch.Success)
    {
      var possessive = selfMatch.Groups["possessive"].Value.Trim();
      if (possessive.Equals("your", StringComparison.OrdinalIgnoreCase))
      {
        possessive = "your";
      }
      return
      [
        new StaticAbility
        {
          Effects = [new DoesntUntapEffect
          {
            WhoseUntapStep = possessive,
          }],
        },
      ];
    }

    // Aura form: "Enchanted [type] doesn't untap during its controller's untap step."
    // Targets the attached permanent via EnchantedOrEquipped.
    var enchantedMatch = Regex.Match(
      clause.RawText,
      @"^\s*(?:Enchanted|Equipped)\s+\w+\s+doesn'?t\s+untap\s+during\s+its\s+controller'?s\s+untap\s+step\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (enchantedMatch.Success)
    {
      return
      [
        new StaticAbility
        {
          Effects = [new DoesntUntapEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            WhoseUntapStep = "its controller's",
          }],
        },
      ];
    }

    return null;
  }
}
