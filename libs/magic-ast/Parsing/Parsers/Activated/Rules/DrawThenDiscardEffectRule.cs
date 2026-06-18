namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Draw a card, then discard a card." — Teferi, Master of Time +1 loyalty ability.
/// A single ", then"-joined sentence that is two sibling effects: draw one card
/// followed by discard one card.
///
/// <para>
/// CR 121.1: "A player draws a card by putting the top card of their library into
/// their hand." CR 701.9a: "To discard a card, move it from its owner's hand to
/// that player's graveyard."
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as
/// a flat sibling pair on <c>Effects</c> (the "draw then" multi-sentence convention),
/// not nested under a CompositeEffect. <see cref="TryMatch"/> always returns null so
/// the single-effect path (which would greedily claim the "draw" and silently drop
/// the discard) never fires.
/// </para>
///
/// <para>
/// ANCHORED (^...$): matches exactly "Draw a card, then discard a card" to prevent
/// substring matches against longer draw-then chains (e.g. "Draw a card, then put
/// this artifact on top of its owner's library."). Priority 952 — above generic
/// DrawCardsEffectRule (998) which would claim the sentence, below DrawThenSelf
/// (950) which is more-specific (so the more-specific sibling fires first when the
/// text is "Draw a card, then put this artifact...").
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 952)]
public sealed class DrawThenDiscardEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Draw\s+a\s+card,\s*then\s+discard\s+a\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape always produces two sibling effects, so it is
  /// served exclusively via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effects = new List<Effect>
    {
      new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = ObjectReference.You(),
      },
      new DiscardCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = ObjectReference.You(),
        Random = false,
      },
    };
    return true;
  }
}
