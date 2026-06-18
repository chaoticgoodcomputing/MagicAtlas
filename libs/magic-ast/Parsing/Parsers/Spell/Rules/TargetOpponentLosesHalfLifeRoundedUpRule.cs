namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target opponent loses half their life, rounded up." — the targeted half-life-loss
/// spell pattern (Blood Tribute). The opponent is targeted; the amount is half their
/// life total, rounded up.
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that player's life
/// total is adjusted accordingly." The amount is a <see cref="CalculatedQuantity"/> with
/// <c>Operation="half"</c>, <c>Rounding="up"</c>, and a <see cref="DerivedQuantity"/>
/// base keyed on <see cref="DerivedKind.LifeTotal"/> (the target's own life total is
/// implicit from the <see cref="LoseLifeEffect.Player"/> reference).
/// </para>
///
/// <para>
/// The player reference is <see cref="ObjectReferenceKind.Opponent"/> (the oracle text
/// says "Target opponent"). "Target opponent" is a targeted reference to an opponent —
/// represented as <c>Kind = Opponent</c> in MAST (see Bargain, HighwayRobber precedents).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent substring matches inside multi-sentence spell clauses
/// (e.g. the full Blood Tribute clause bundles this sentence with the kicked gain-life).
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetOpponentLosesHalfLifeRoundedUpRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+opponent\s+loses?\s+half\s+their\s+life,?\s+rounded\s+up$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    // Half the target opponent's life total, rounded up.
    // DerivedQuantity(LifeTotal) is the target's life total; the Player on
    // LoseLifeEffect carries who the target is (Opponent).
    var halfLifeRoundedUp = new CalculatedQuantity
    {
      Operation = "half",
      BaseQuantity = new DerivedQuantity { DerivedFrom = DerivedKind.LifeTotal },
      Rounding = "up",
    };

    effect = new LoseLifeEffect
    {
      Amount = halfLifeRoundedUp,
      Player = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
    };
    return true;
  }
}
