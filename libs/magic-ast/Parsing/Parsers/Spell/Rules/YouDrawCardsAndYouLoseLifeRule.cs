namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You draw N cards and you lose N life." — the controller draw-and-drain
/// spell shape (Ambition's Cost, Ancient Craving, Succumb to Temptation,
/// Rowan's Grim Search, Feed the Infection, Mordor Muster).
///
/// <para>
/// A single "… and you …"-joined sentence that expands to two sibling effects:
/// <see cref="DrawCardsEffect"/> (you draw N) and <see cref="LoseLifeEffect"/>
/// (you lose N). Both clauses reference the controller ("you"), so both effects
/// carry <see cref="ObjectReferenceKind.You"/>.
/// </para>
///
/// <para>
/// CR 121.1 (draw): "A player draws a card by putting the top card of their
/// library into their hand." CR 119.3 (lose life): "If an effect causes a player
/// to gain life or lose life, that player's life total is adjusted accordingly."
/// </para>
///
/// <para>
/// Distinct from the targeted <see cref="TargetPlayerDrawsLosesLifeRule"/>
/// ("Target player draws … and loses …") and from the activated-path
/// <c>YouDrawCardAndLoseLifeEffectRule</c> ("You draw … and lose …", without the
/// second "you"). The mandatory second "you" ("and <b>you</b> lose") disambiguates
/// this spell shape.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the full sentence is matched end-to-end so no broader
/// sibling can consume it and silently drop the lose-life conjunct. Emits a flat
/// <c>[DrawCardsEffect, LoseLifeEffect]</c> list via
/// <see cref="IMultiSpellRule.TryMatchMulti"/>; the single-effect
/// <see cref="ISpellRule.TryMatch"/> always returns false.
/// </para>
/// </summary>
[SpellRule]
public sealed class YouDrawCardsAndYouLoseLifeRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  private static readonly Regex Pattern = new(
    $@"^You\s+draw\s+(?<draw>{CountTokens})\s+cards?\s+and\s+you\s+lose\s+(?<lose>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Single-effect path intentionally disabled — this shape always yields two siblings.
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var you = ObjectReference.You();
    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["draw"].Value)),
        Player = you,
      },
      new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["lose"].Value)),
        Player = you,
      },
    };
    return true;
  }
}
