namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Combined attached-P/T anthem + combat-damage-assignment substitution:
/// "[Enchanted|Equipped] creature gets +N/+M and assigns combat damage equal to
/// its toughness rather than its power." e.g. Gauntlets of Light — "Enchanted
/// creature gets +0/+2 and assigns combat damage equal to its toughness rather
/// than its power."
///
/// <para>
/// Emits ONE <see cref="StaticAbility"/> carrying TWO effects in its list (the
/// MAST multi-effect-per-clause encoding): a <see cref="ModifyPTEffect"/>
/// (+N/+M) and an <see cref="AssignDamageAsToughnessEffect"/> (CR 510.1a —
/// "Each attacking creature and each blocking creature assigns combat damage
/// equal to its power." — the Doran, the Siege Tower / High Alert combat-damage
/// substitution), BOTH targeting the same attached subject "Enchanted/Equipped
/// creature". One oracle clause generates one continuous effect from one
/// static ability, so the two effects share a single ability rather than being
/// split into two abilities (CR 611.3 — "A continuous effect may be generated
/// by the static ability of an object.").
/// </para>
///
/// <para>
/// Sibling of <see cref="AttachedModifyPTAndDoesntUntapRule"/> and
/// <see cref="AttachedModifyPTAndGoadedRule"/> — same compound shape, different
/// rider effect. Reuses the existing <see cref="AssignDamageAsToughnessEffect"/>
/// node (the Doran/High Alert family) rather than inventing a new discriminator;
/// its <c>AppliesTo</c> is set to the attached subject since the printed object
/// here (an Aura) is not itself the affected creature.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full oracle sentence is matched exactly so this rule
/// cannot fire on a substring of a more specific sibling clause, and cannot
/// itself swallow a broader sentence.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class AttachedModifyPTAndAssignDamageAsToughnessRule : IStaticRule
{
  // "[Enchanted|Equipped] creature gets +N/+M and assigns combat damage equal
  // to its toughness rather than its power." Both P/T sides require an
  // explicit sign (anthems use signed notation).
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+assigns\s+combat\s+damage\s+equal\s+to\s+its\s+toughness\s+rather\s+than\s+its\s+power\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["psign"].Value + match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["tsign"].Value + match.Groups["t"].Value);

    // A fresh ObjectReference is built per effect (records are immutable; the
    // two targets are value-equal).
    ObjectReference Subject() => new() { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ModifyPTEffect
          {
            Target = Subject(),
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
          },
          new AssignDamageAsToughnessEffect { AppliesTo = Subject() },
        ],
      },
    ];
  }
}
