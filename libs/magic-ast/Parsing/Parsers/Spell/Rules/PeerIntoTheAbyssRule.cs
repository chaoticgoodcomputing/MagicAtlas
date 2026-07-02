namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player draws cards equal to half the number of cards in their library and loses half
/// their life. Round up each time." — the Peer into the Abyss draw-half-library-then-lose-half-life
/// pattern.
///
/// <para>
/// Both quantities are <see cref="CalculatedQuantity"/> with <c>Operation="half"</c> and
/// <c>Rounding="up"</c>. The draw count is based on a <see cref="CountQuantity"/> of cards in the
/// target player's library (Zone=Library, Controller=Target). The life-loss amount is based on a
/// <see cref="DerivedQuantity"/> with <c>DerivedFrom=LifeTotal</c> (the target player's life total
/// is implicit from the <see cref="LoseLifeEffect.Player"/> reference).
/// </para>
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that player's life total
/// is adjusted accordingly."
/// </para>
///
/// <para>
/// The trailing "Round up each time." sentence is a printed instruction clarifying rounding
/// direction for both effects; it is encoded structurally as <c>Rounding="up"</c> on each
/// <see cref="CalculatedQuantity"/> rather than as a separate effect.
/// </para>
/// </summary>
[SpellRule(Priority = 75)]
public sealed class PeerIntoTheAbyssRule : ISpellRule, IMultiSpellRule
{
  /// <summary>
  /// Matches the full oracle text (TrimEnd('.')) of Peer into the Abyss.
  /// The "Round up each time" trailing sentence is consumed by the pattern and
  /// encoded structurally as Rounding="up" on each CalculatedQuantity.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^Target\s+player\s+draws\s+cards\s+equal\s+to\s+half\s+the\s+number\s+of\s+cards\s+in\s+their\s+library\s+and\s+loses\s+half\s+their\s+life\.\s+Round\s+up\s+each\s+time$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled; always multi.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat [DrawCardsEffect, LoseLifeEffect].
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var targetPlayer = ObjectReference.Target(ObjectFilter.Player());

    // Draw count: half the number of cards in the target player's library, rounded up.
    var libraryCount = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["card"],
        Zone = Zone.Library,
        Controller = ControllerFilter.Target,
      },
    };
    var halfLibraryRoundedUp = new CalculatedQuantity
    {
      Operation = "half",
      BaseQuantity = libraryCount,
      Rounding = "up",
    };

    // Life loss: half the target player's life total, rounded up.
    var lifeTotal = new DerivedQuantity
    {
      DerivedFrom = DerivedKind.LifeTotal,
    };
    var halfLifeRoundedUp = new CalculatedQuantity
    {
      Operation = "half",
      BaseQuantity = lifeTotal,
      Rounding = "up",
    };

    effects =
    [
      new DrawCardsEffect
      {
        Count = halfLibraryRoundedUp,
        Player = targetPlayer,
      },
      new LoseLifeEffect
      {
        Amount = halfLifeRoundedUp,
        Player = targetPlayer,
      },
    ];
    return true;
  }
}
