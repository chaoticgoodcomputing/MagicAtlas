namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;

/// <summary>
/// Static restriction: "This creature can't attack a player it has already
/// attacked this turn." Narrows the attack restriction to a specific player —
/// the named object may still attack any player (or planeswalker/battle) it
/// has NOT already attacked earlier in the current turn.
/// </summary>
/// <remarks>
/// CR 508.1 (declare-attackers step; attacking restrictions constrain the set
/// of legal attacker declarations the active player can make; the active
/// player checks each creature for restrictions when declaring attackers).
///
/// <para>
/// The qualifier is a structured boolean, <see cref="CantAttackEffect.AlreadyAttackedThisTurn"/>,
/// mirroring how <see cref="CantAttackAloneRule"/> and <see cref="CanOnlyAttackAloneRule"/>
/// de-string the fixed "alone" idiom onto the same effect node rather than a
/// free-text qualifier string. The pronoun "it" is reflexive (the source
/// object bearing the ability), so <see cref="CantAttackEffect.Target"/> stays
/// null (the default "this creature" subject).
/// </para>
///
/// <para>
/// Pattern is anchored (^...$) so it cannot match as a substring of a longer
/// or differently-qualified "can't attack" clause.
/// </para>
/// </remarks>
[StaticRule(Priority = 60)]
public sealed class CantAttackAlreadyAttackedThisTurnRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*This\s+creature\s+can'?t\s+attack\s+a\s+player\s+it\s+has\s+already\s+attacked\s+this\s+turn\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CantAttackEffect { AlreadyAttackedThisTurn = true }],
      },
    ];
  }
}
