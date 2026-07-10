namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Target opponent exiles a creature or planeswalker they control with the greatest
/// mana value among creatures and planeswalkers they control." — the edict-style forced
/// exile shape (Blot Out, End of the Hunt).
///
/// <para>
/// This is an <em>edict</em>, not targeted removal: the only target is the OPPONENT
/// (CR 115.1); the exiled permanent is not itself targeted. The targeted opponent then
/// chooses one of their own creatures/planeswalkers tied for the greatest mana value
/// (CR 202.3) and moves it to exile (CR 701.13a — "To exile an object, move it to the
/// exile zone from wherever it is"). Modeled as an <see cref="ExileEffect"/> with two
/// distinct axes:
/// <list type="bullet">
///   <item><see cref="ExileEffect.Player"/> — the acting player, "target opponent"
///   (<see cref="ObjectReferenceKind.Target"/> + <c>CardTypes = ["opponent"]</c>),
///   mirroring the edict-actor convention of
///   <see cref="MagicAST.AST.Effects.CardFlow.DiscardCardsEffect.Player"/>;</item>
///   <item><see cref="ExileEffect.Target"/> — the exiled object, an indefinite
///   controller-choice reference (<see cref="ObjectReferenceKind.Any"/>: the opponent
///   picks, no "target" keyword on it) scoped to "a creature or planeswalker they
///   control" (<c>Controller = Target</c>, i.e. the same targeted opponent).</item>
/// </list>
/// The "greatest mana value among creatures and planeswalkers they control" superlative
/// is structured as an <see cref="ExtremeStatCharacteristic"/> (<c>Stat = ManaValue</c>,
/// <c>Extreme = Greatest</c>) — the "MaxStatFilter" the free-text whitelist named as
/// missing for the sibling greatest-power sacrifice edict
/// (<c>EachOpponentSacrificesGreatestPowerCreatureEffectRule</c>). The "among creatures
/// and planeswalkers they control" population equals this object's own filter, so the
/// characteristic's <c>Scope</c> is left null (it defaults to the enclosing filter).
/// </para>
///
/// <para>
/// Fully anchored (<c>^…$</c>): matches only this exact whole-clause sentence, so it
/// cannot substring-capture the shorter siblings that a more-specific rule should own —
/// "Target opponent exiles a creature they control and their graveyard." (Strategic
/// Betrayal), the modal "• Target opponent exiles a creature they control." (Doomfall,
/// Debt to the Kami), and the "Each opponent exiles a creature with the greatest power
/// …" broadcast (Olórin's Searing Light) all fail the anchor.
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetOpponentExilesGreatestManaValuePermanentRule : ISpellRule
{
  // The dispatcher trims whitespace and strips the trailing period before TryMatch.
  private static readonly Regex Pattern = new(
    @"^Target\s+opponent\s+exiles\s+a\s+creature\s+or\s+planeswalker\s+they\s+control\s+"
    + @"with\s+the\s+greatest\s+mana\s+value\s+among\s+creatures\s+and\s+planeswalkers\s+they\s+control$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ExileEffect
    {
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["opponent"] },
      },
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature", "planeswalker"],
          Controller = ControllerFilter.Target,
          Characteristics =
          [
            new ExtremeStatCharacteristic
            {
              Stat = RelativeCharacteristic.ManaValue,
              Extreme = StatExtreme.Greatest,
            },
          ],
        },
      },
    };
    return true;
  }
}
