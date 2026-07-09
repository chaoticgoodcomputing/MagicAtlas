namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// Activated-ability effect shape "Prevent all combat damage that would be dealt
/// by target creature this turn." — the source-scoped Fog-effect prevention shield
/// activated from a permanent (e.g. Safeguard: "{2}{W}: Prevent all combat damage
/// that would be dealt by target creature this turn."). The mana cost half is parsed
/// independently; this rule only recognizes the post-colon effect body.
///
/// The "by target creature" qualifier scopes the shield to a single damage SOURCE
/// (the targeted creature) rather than being blanket. It therefore maps to the
/// <see cref="PreventDamageEffect.Source"/> reference — a
/// <see cref="ObjectReferenceKind.Target"/> filtered to <c>CardTypes=["creature"]</c>
/// — and NOT to <see cref="PreventDamageEffect.Target"/> (which is the damage
/// recipient). This is the source-scoped sibling of the blanket
/// <see cref="PreventAllCombatDamageThisTurnActivatedEffectRule"/>, producing the same
/// combat-only, until-end-of-turn, all-instances (<see cref="PreventDamageEffect.All"/>)
/// prevention shield.
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
/// specific sibling. The "by target creature" segment sits between "dealt" and
/// "this turn", so the blanket sibling's regex (which requires "dealt this turn"
/// contiguously) never matches this text and vice versa — the two are disjoint.
/// </summary>
[ActivatedEffectRule(Priority = 980)]
public sealed class PreventAllCombatDamageByTargetCreatureThisTurnEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Prevent\s+all\s+combat\s+damage\s+that\s+would\s+be\s+dealt\s+by\s+target\s+creature\s+this\s+turn$",
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
      Source = ObjectReference.Target(new ObjectFilter { CardTypes = ["creature"] }),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
