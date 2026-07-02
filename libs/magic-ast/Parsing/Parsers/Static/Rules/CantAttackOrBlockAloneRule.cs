namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// Static restriction: "This creature can't attack or block alone." The "alone"
/// qualifier conditions both the attack and the block restriction on this object
/// being the sole attacker / sole blocker — the restriction is lifted whenever
/// at least one other creature is also declared.
/// </summary>
/// <remarks>
/// CR 508.1 (excerpt): "First, the active player declares attackers. This
/// turn-based action doesn't use the stack…" CR 509.1 (excerpt): "First, the
/// defending player declares blockers. This turn-based action doesn't use the
/// stack…" "alone" means the restriction applies unless another creature also
/// attacks/blocks.
///
/// <para>
/// This is ONE restriction whose "alone" qualifier covers both halves. Per the
/// multi-effect-per-clause doctrine (and matching <c>EnchantedCantAttackOrBlockRule</c>),
/// the clause yields a <c>CantAttackEffect</c> and a <c>CantBlockEffect</c>, each
/// carrying <c>Alone = true</c>. The qualifier is a structured boolean on those
/// nodes, never a free-text "alone" string.
/// </para>
/// </remarks>
[StaticRule(Priority = 957)]
public sealed class CantAttackOrBlockAloneRule : IStaticRule
{
  private static readonly Regex _cantAttackOrBlockAlonePattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+attack\s+or\s+block\s+alone\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_cantAttackOrBlockAlonePattern.IsMatch(clause.RawText))
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
          new MagicAST.AST.Effects.Combat.CantBlockEffect { Alone = true },
        ],
      },
    ];
  }
}
