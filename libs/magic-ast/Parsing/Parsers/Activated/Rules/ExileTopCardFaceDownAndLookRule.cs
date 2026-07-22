namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// "Exile the top card of your library face down and look at it." — moves the
/// top card of the controller's library to the exile zone face down, then allows
/// the controller to look at it (CR 406.3: a player instructed to exile a card
/// face down may look at it).
///
/// <para>
/// This is a combined effect: the exile is face-down (<see cref="ExileEffect.IsFaceDown"/> = true)
/// and is followed by a <see cref="LookAtCardsEffect"/> targeting the just-exiled
/// card in exile. CR 406.3 (verbatim): "Cards 'exiled face down' can't be examined by
/// any player except when instructions allow it. However, if a player is instructed to
/// look at a card and then exile it face down, or once a player is allowed to look at
/// a card exiled face down, that player may continue to look at that card until it
/// leaves the exile zone."
/// </para>
///
/// <para>
/// Fully anchored (^…$): the surface phrase "face down and look at it" must not
/// match as a substring of a more-specific sibling (no other activated-effect
/// sentence contains this exact phrasing). Priority 981 — one below
/// <see cref="ExileTopCardOfLibraryEffectRule"/> (982) so the simpler plain-exile
/// rule is tried first and falls through naturally.
/// </para>
///
/// <para>CR 406.3: exiled-face-down visibility rules.</para>
/// </summary>
[ActivatedEffectRule(Priority = 981)]
public sealed class ExileTopCardFaceDownAndLookRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Exile\s+the\s+top\s+card\s+of\s+your\s+library\s+face\s+down\s+and\s+look\s+at\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc />
  public Effect? TryMatch(string effectText) => null; // always multi

  /// <inheritdoc />
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!_pattern.IsMatch(effectText))
    {
      return false;
    }

    var exileTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Designated,
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        Zone = Zone.Library,
        Controller = ControllerFilter.You,
        // "the top card of your library" — positional designation (CR 401.1), the
        // merged ordered-zone axis; replaces the old Other("top") free-text residual.
        LibraryPosition = new LibraryPosition { Position = ZonePosition.Top },
      },
    };

    effects =
    [
      new ExileEffect
      {
        Target = exileTarget,
        IsFaceDown = true,
      },
      new LookAtCardsEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Zone = Zone.Exile,
      },
    ];
    return true;
  }
}
