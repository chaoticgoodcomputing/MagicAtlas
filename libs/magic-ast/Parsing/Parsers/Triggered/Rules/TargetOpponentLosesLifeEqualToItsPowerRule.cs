namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target opponent loses life equal to its power" — a leaves-the-battlefield (or
/// other) trigger whose life-loss amount is derived from the triggering permanent's
/// own power rather than a fixed number (Rapacious Guest: "When this creature leaves
/// the battlefield, target opponent loses life equal to its power.").
///
/// <para>
/// CR 608.2h: when a triggered ability that triggers on a permanent leaving the
/// battlefield needs information about that permanent, it uses the game state as it
/// existed immediately before the permanent left — so "its power" refers to the
/// leaving creature's last-known power. MAST records the reference descriptively
/// (the pronoun antecedent), not the resolved value (ADR 0004 reference-not-resolution),
/// matching the existing "it" convention on
/// <see cref="ItDealsDamageEqualToItsPowerToAnyTargetTriggeredRule"/>.
/// </para>
///
/// <para>
/// "Target opponent" is the targeted-opponent player reference —
/// <see cref="ObjectReferenceKind.Opponent"/> — matching the established convention
/// for this exact phrase (<see cref="LoseLifeDerivedRule"/>,
/// <see cref="TargetOpponentLosesHalfLifeRoundedUpRule"/>).
/// </para>
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that player's
/// life total is adjusted accordingly." CR 120.1 (power as a characteristic used in
/// a derived quantity).
/// </para>
///
/// <para>
/// Anchored (^…$) so this only matches the bare sentence, not a longer effect body
/// this phrase might appear inside.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TargetOpponentLosesLifeEqualToItsPowerRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^target\s+opponent\s+loses?\s+life\s+equal\s+to\s+its\s+power$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new LoseLifeEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.Power, Source = "it" },
    };
    return true;
  }
}
