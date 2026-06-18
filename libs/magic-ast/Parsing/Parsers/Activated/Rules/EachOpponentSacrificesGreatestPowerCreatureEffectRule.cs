namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Each opponent sacrifices a creature with the greatest power among creatures that
/// player controls." — the Professor Onyx −3 loyalty ability shape.
///
/// <para>
/// The "with the greatest power among creatures that player controls" qualifier is a
/// comparative game-state predicate (no dedicated CR — comparative relative power among a player's
/// creatures) with no first-class ObjectFilter axis. It is carried as an
/// <see cref="OtherCharacteristic"/> residual on the sacrificed-creature filter per
/// the ADR 0001 free-text doctrine: the type-honest home for "not yet structured".
/// </para>
///
/// <para>
/// The sacrifice target is each opponent (<see cref="ObjectReferenceKind.EachOpponent"/>);
/// each opponent independently chooses which of their greatest-power creatures to
/// sacrifice (in the case of a tie, they choose among the tied creatures).
/// CR 701.21a (Sacrifice); CR 102.2 (opponents).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): prevents false positives against other
/// "each opponent sacrifices" patterns. Priority 998 — just below
/// <see cref="EachOpponentMayDiscardOtherwiseLoseLifeRepeatEffectRule"/> (999)
/// and above the generic effect rules that could partially match.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 998)]
public sealed class EachOpponentSacrificesGreatestPowerCreatureEffectRule : IActivatedEffectRule
{
  // Anchored: matches exactly the Professor Onyx -3 oracle text (post-colon, period-stripped).
  private static readonly Regex _pattern = new(
    @"^Each\s+opponent\s+sacrifices\s+a\s+creature\s+with\s+the\s+greatest\s+power\s+among\s+creatures\s+that\s+player\s+controls$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.EachOpponent,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics =
          [
            Characteristic.Other("with the greatest power among creatures that player controls"),
          ],
        },
      },
    };
  }
}
