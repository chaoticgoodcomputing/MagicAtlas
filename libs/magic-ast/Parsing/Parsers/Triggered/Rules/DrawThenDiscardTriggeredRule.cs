namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw a card, then discard a card." — a single ", then"-joined sentence that is
/// two mandatory sibling effects: draw one card, followed by discard one card. The
/// triggered-ability sibling of
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.DrawThenDiscardEffectRule"/>
/// (Teferi, Master of Time's +1 loyalty ability); the recurring surface also appears
/// as a GRANTED triggered ability's resolution ("Whenever this creature becomes
/// tapped, draw a card, then discard a card." — Unctus, Grand Metatect).
///
/// <para>
/// CR 121.1: "A player draws a card by putting the top card of their library into
/// their hand." CR 701.9a: "To discard a card, move it from its owner's hand to that
/// player's graveyard."
/// </para>
///
/// <para>
/// <see cref="ITriggeredRule.TryMatch"/> returns exactly ONE <see cref="Effect"/> per
/// the interface contract — unlike the Activated parser, the Triggered dispatcher has
/// no generic multi-effect-rule mechanism, so the two sibling effects are bundled in a
/// single <see cref="CompositeEffect"/> rather than left as flat siblings. This
/// mirrors the established "multi-effect triggered convention"
/// (<c>DestroyDefendingCreatureThenPutCounterRule</c>: "destroy … , then put a +1/+1
/// counter on [Name]." → one <see cref="CompositeEffect"/> of two sibling effects).
/// </para>
///
/// <para>
/// ANCHORED (^...$): matches exactly "draw a card, then discard a card" to prevent
/// substring collisions with longer draw-then chains handled by other rules.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class DrawThenDiscardTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^draw\s+a\s+card,\s*then\s+discard\s+a\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects =
      [
        new DrawCardsEffect { Count = LiteralQuantity.Of(1), Player = ObjectReference.You() },
        new DiscardCardsEffect { Count = LiteralQuantity.Of(1), Player = ObjectReference.You(), Random = false },
      ],
    };
    return true;
  }
}
