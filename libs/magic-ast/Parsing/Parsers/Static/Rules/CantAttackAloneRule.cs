namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// Static restriction: "This creature can't attack alone." The "alone"
/// qualifier conditions the attack restriction on this object being the sole
/// declared attacker — the restriction is lifted whenever at least one other
/// creature is also declared as an attacker.
/// </summary>
/// <remarks>
/// CR 508.1 (excerpt): "First, the active player declares attackers. This
/// turn-based action doesn't use the stack. To declare attackers, the active
/// player follows the steps below, in order. If at any point during the
/// declaration of attackers, the active player is unable to comply with any
/// of the steps listed below, the declaration is illegal; the game returns to
/// the moment before the declaration (see rule 733, "Handling Illegal
/// Actions"). Example: A player controls two creatures, each with a
/// restriction that states "This creature can't attack alone." It's legal to
/// declare both as attackers." CR 508.1c (excerpt): "The active player checks
/// each creature they control to see whether it's affected by any
/// restrictions (effects that say a creature can't attack, or that it can't
/// attack unless some condition is met). If any restrictions are being
/// disobeyed, the declaration of attackers is illegal."
///
/// <para>
/// This is the single-effect, no-"or block" sibling of
/// <see cref="CantAttackOrBlockAloneRule"/>. The qualifier is a structured
/// boolean on <c>CantAttackEffect</c>, never a free-text "alone" string.
/// </para>
///
/// <para>
/// Priority 959: tried after <see cref="CanOnlyAttackAloneRule"/> (958) and
/// <see cref="CantAttackOrBlockAloneRule"/> (957), keeping this rule ordered
/// alongside the rest of the "alone" family band. The pattern is anchored
/// (^...$) and the "can'?t" alternation cannot match "can only", so it never
/// collides with <see cref="CanOnlyAttackAloneRule"/>'s "can only attack
/// alone" surface phrase.
/// </para>
/// </remarks>
[StaticRule(Priority = 959)]
public sealed class CantAttackAloneRule : IStaticRule
{
  private static readonly Regex _cantAttackAlonePattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+attack\s+alone\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_cantAttackAlonePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Combat.CantAttackEffect { Alone = true },
        ],
      },
    ];
  }
}
