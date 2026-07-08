namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;

/// <summary>
/// Activated-ability effect shape "Prevent all combat damage that would be dealt
/// this turn." — the Fog-effect prevention shield activated from a permanent
/// (e.g. Kami of False Hope: "Sacrifice this creature: Prevent all combat damage
/// that would be dealt this turn."). The cost half (the sacrifice) is parsed
/// independently; this rule only recognizes the post-colon effect body.
///
/// This is the activated-ability sibling of the spell rule
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.PreventAllCombatDamageThisTurnRule"/>
/// (Fog), producing an identical <see cref="PreventDamageEffect"/>: a blanket
/// (no target), combat-only, until-end-of-turn prevention shield.
///
/// CR 615.1 (cited verbatim from rules-structure.json): "Some continuous effects
/// are prevention effects. Like replacement effects (see rule 614), prevention
/// effects apply continuously as events happen-they aren't locked in ahead of
/// time. Such effects watch for a damage event that would happen and completely
/// or partially prevent the damage that would be dealt. They act like \"shields\"
/// around whatever they're affecting."
///
/// ANCHORED (^...$): the surface phrase is matched whole so it cannot be consumed
/// as a substring of a longer clause, and cannot claim a substring of a more
/// specific sibling ("Prevent the next N damage …", "The next time a [color]
/// source …"). Those siblings are structurally disjoint from this blanket phrase,
/// but explicit anchoring is the project standard.
/// </summary>
[ActivatedEffectRule(Priority = 980)]
public sealed class PreventAllCombatDamageThisTurnActivatedEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Prevent\s+all\s+combat\s+damage\s+that\s+would\s+be\s+dealt\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new PreventDamageEffect
    {
      All = true,
      CombatOnly = true,
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
