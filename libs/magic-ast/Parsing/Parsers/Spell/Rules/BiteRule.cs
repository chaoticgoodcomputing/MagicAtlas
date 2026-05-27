namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises "bite" spell oracle text — a one-directional fight where only the
/// controlled creature deals damage:
/// <list type="bullet">
///   <item>"Target creature you control deals damage equal to its power to target creature you don't control."</item>
///   <item>"Target creature you control deals damage equal to its power to target creature or planeswalker you don't control."</item>
/// </list>
///
/// Both phrasings emit a <see cref="DealDamageEffect"/> where:
/// <list type="bullet">
///   <item>Source is the controlled target creature (Controller: You).</item>
///   <item>Amount is <see cref="DerivedQuantity"/> Power sourced from "it".</item>
///   <item>Target is an opponent-controlled creature (or creature/planeswalker for the broader form).</item>
/// </list>
///
/// Distinct from <see cref="FightRule"/> (CR 701.14), which produces a symmetric
/// <see cref="MagicAST.AST.Effects.Combat.FightEffect"/> — both creatures deal damage to each other.
/// Bite is asymmetric: only the source creature deals damage; the target does not retaliate.
/// </summary>
[SpellRule]
public sealed class BiteRule : ISpellRule
{
  // Matches:
  //   "Target creature you control deals damage equal to its power to target creature you don't control"
  //   "Target creature you control deals damage equal to its power to target creature or planeswalker you don't control"
  // The optional "or planeswalker" group is captured to determine CardTypes on the damage target.
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+you\s+control\s+deals\s+damage\s+equal\s+to\s+its\s+power\s+to\s+target\s+creature(?:\s+or\s+(?<extra>planeswalker))?\s+you\s+don't\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var cardTypes = m.Groups["extra"].Success
      ? new[] { "creature", m.Groups["extra"].Value.ToLowerInvariant() }
      : new[] { "creature" };

    effect = new DealDamageEffect
    {
      Source = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Power,
        Source = "it",
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
