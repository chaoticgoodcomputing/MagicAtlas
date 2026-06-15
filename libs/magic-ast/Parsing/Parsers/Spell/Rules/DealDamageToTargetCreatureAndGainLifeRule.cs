namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to target creature and you gain N life." — burn+lifegain pattern
/// targeting a creature specifically. Emits a flat [DealDamageEffect, GainLifeEffect] list
/// via <see cref="IMultiSpellRule.TryMatchMulti"/>. Sibling of
/// <see cref="DealDamageToAnyTargetAndGainLifeRule"/>, which covers the "any target" form.
///
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and players.
/// This is generally detrimental to the object or player that receives that damage.
/// An object that deals damage is the source of that damage."
///
/// CR 119.3: "If an effect causes a player to gain life or lose life, that player's life
/// total is adjusted accordingly."
///
/// CR 115.1: "Some spells and abilities require their controller to choose one or more targets
/// for them. The targets are object(s) and/or player(s) the spell or ability will affect.
/// These targets are declared as part of the process of putting the spell or ability on the
/// stack."
///
/// Representative card: Last Kiss ({2}{B}, Instant).
/// </summary>
[SpellRule]
public sealed class DealDamageToTargetCreatureAndGainLifeRule : ISpellRule, IMultiSpellRule
{
  private static readonly Regex _pattern = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<damageAmount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+creature\s+and\s+you\s+gain\s+(?<lifeAmount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat [DealDamageEffect, GainLifeEffect] list.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return false;
    }

    var damageAmount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["damageAmount"].Value));
    var lifeAmount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["lifeAmount"].Value));

    effects = new List<Effect>
    {
      new DealDamageEffect
      {
        Amount = damageAmount,
        Source = ObjectReference.Self(),
        Target = ObjectReference.Target(ObjectFilter.Creature()),
      },
      new GainLifeEffect
      {
        Amount = lifeAmount,
        Player = ObjectReference.You(),
      },
    };
    return true;
  }
}
