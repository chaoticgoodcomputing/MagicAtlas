namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// Static restriction: "This creature can only attack alone." The "only alone"
/// qualifier restricts the creature to attacking only as the sole attacker —
/// it cannot be declared as an attacker alongside other creatures.
/// </summary>
/// <remarks>
/// CR 508.1 (excerpt): "First, the active player declares attackers. This
/// turn-based action doesn't use the stack…" — "can only attack alone" means
/// the restriction applies whenever other creatures would also be declared as
/// attackers in the same combat. The creature is excluded from any multi-attacker
/// declaration.
///
/// <para>
/// This is the DUAL of "can't attack alone" (<see cref="CantAttackOrBlockAloneRule"/>).
/// Where <c>CantAttackEffect.Alone = true</c> lifts the restriction when other
/// creatures also attack (Loyal Pegasus), <c>CantAttackEffect.OnlyAlone = true</c>
/// applies the restriction precisely when other creatures are also attacking.
/// </para>
///
/// <para>
/// Priority 958: tried before <see cref="CantAttackOrBlockAloneRule"/> (Priority 957)
/// and before any generic "can't attack" rule, so this specific "only alone" form is
/// matched before the surface phrase could be misclassified by a broader pattern.
/// The pattern is anchored (^...$) so it cannot silently consume a text that only
/// partially matches.
/// </para>
/// </remarks>
[StaticRule(Priority = 958)]
public sealed class CanOnlyAttackAloneRule : IStaticRule
{
  private static readonly Regex _canOnlyAttackAlonePattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can\s+only\s+attack\s+alone\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_canOnlyAttackAlonePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Combat.CantAttackEffect { OnlyAlone = true },
        ],
      },
    ];
  }
}
