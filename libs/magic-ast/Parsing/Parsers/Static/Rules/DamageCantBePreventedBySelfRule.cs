namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// Recognises the source-scoped damage-prevention lock static "Damage that would
/// be dealt by this creature can't be prevented." (Excruciator) — a rules-of-
/// the-game continuous effect (CR 611.1) written as a plain static statement
/// (CR 604.1), that nullifies prevention effects (CR 615.1) applicable to damage
/// dealt specifically by this creature. Emits a single <see cref="StaticAbility"/>
/// carrying one <see cref="DamageCantBePreventedEffect"/> with
/// <c>Source = ObjectReference.Self()</c>.
///
/// Distinct from <see cref="CantPreventDamageRule"/> (Leyline of Punishment's
/// unconditional, global "Damage can't be prevented.", which names no source),
/// this rule requires the "that would be dealt by this creature" source clause and
/// is matched by a separate, disjoint anchored pattern.
///
/// ANCHORED (^...$): the full-line template is matched in its entirety so this
/// cannot fire as a substring of a broader clause, and cannot claim a substring of
/// the unscoped global sibling.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class DamageCantBePreventedBySelfRule : IStaticRule
{
  // "Damage that would be dealt by this creature can't be prevented."
  // ANCHORED (^...$) to prevent substring matching inside broader clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Damage\s+that\s+would\s+be\s+dealt\s+by\s+this\s+creature\s+can'?t\s+be\s+prevented\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new DamageCantBePreventedEffect { Source = ObjectReference.Self() },
        ],
      },
    ];
  }
}
