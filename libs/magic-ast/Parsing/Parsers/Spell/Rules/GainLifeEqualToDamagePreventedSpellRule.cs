namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain life equal to the damage prevented this way." — controller life-gain
/// whose amount is derived from the damage prevented by a preceding
/// <see cref="MagicAST.AST.Effects.Damage.PreventDamageEffect"/> in the same spell
/// (the prevent-and-gain shape). Representative cards: Intervention Pact and Reverse
/// Damage ("The next time a source of your choice would deal damage to you this turn,
/// prevent that damage. You gain life equal to the damage prevented this way."), Awe
/// Strike, Hallow, Candles' Glow, Chant of Vitu-Ghazi — each pairs a prevention
/// sentence with this life-gain sentence; the SpellAbilityParser's sentence splitter
/// dispatches this second sentence here after the prevention sentence is matched.
///
/// <para>
/// The amount is a <see cref="DerivedQuantity"/> keyed on
/// <see cref="DerivedKind.DamagePrevented"/> — reference-not-resolution (ADR 0004):
/// MAST records the textual link to "the damage prevented this way", not the runtime
/// value (CR 119.3: "If an effect causes a player to gain life … that player's life
/// total is adjusted accordingly."). This is the prevention-linked sibling of
/// <see cref="GainLifeSpellRule"/>'s "…equal to the life lost this way" (Blood Tithe)
/// branch, which keys on <see cref="DerivedKind.LifeLost"/> instead.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the exact prevented-amount sentence. The "life lost this
/// way" and "N life" siblings carry different tokens after "gain" and so cannot match
/// this phrase; the anchor keeps a future broader "You gain …" pattern from consuming
/// this sentence and silently losing the derived-quantity link.
/// </para>
/// </summary>
[SpellRule(Priority = 90)]
public sealed class GainLifeEqualToDamagePreventedSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^You\s+gain\s+life\s+equal\s+to\s+the\s+damage\s+prevented\s+this\s+way$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new GainLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.DamagePrevented },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
