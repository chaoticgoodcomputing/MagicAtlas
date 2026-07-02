namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "You may reveal an artifact card you own from outside the game or choose a
/// face-up artifact card you own in exile. Put that card into your hand." —
/// Karn, the Great Creator's −2 loyalty ability. The controller optionally
/// retrieves an artifact card they own from outside the game (sideboard,
/// CR 400.11) or from face-up exile (CR 406) and puts it into their hand.
///
/// <para>
/// Modelled as an <see cref="OptionalEffect"/> whose
/// <see cref="OptionalEffect.Inner"/> is a <see cref="WishEffect"/> (ADR 0005:
/// "You may" is the wrapper's presence, not a bool). The
/// <see cref="WishEffect"/> records the two retrieval sources
/// (<see cref="WishSource.OutsideGame"/> and <see cref="WishSource.Exile"/>),
/// the <see cref="ObjectFilter"/> scoping the card to "artifact" owned by you,
/// and <c>FaceUpOnly=true</c> for the exile branch (only face-up exiled cards
/// qualify). Putting the card into hand is the inherent outcome of the Wish
/// action and is recorded in the <see cref="WishEffect"/> node itself — the
/// separate "Put that card into your hand." sentence is consumed as part of the
/// same semantic unit (CR 400.11).
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> because the two
/// oracle sentences ("You may … or choose …" and "Put that card into your hand.")
/// form a single semantic action. <see cref="TryMatch"/> always returns null so
/// the single-sentence path never fires. <see cref="TryMatchMulti"/> matches the
/// full two-sentence form anchored at both ends.
/// </para>
///
/// <para>
/// CR 400.11: cards brought into the game from outside the game remain until the
/// game ends, the owner leaves, or a rule removes them.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class KarnWishEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored (^…$) two-sentence form. The first sentence covers the "You may …
  // or choose …" selection; the second covers "Put that card into your hand."
  // The dot between sentences is consumed by the regex literal period.
  // Accepts optional trailing period on the second sentence.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+reveal\s+an\s+artifact\s+card\s+you\s+own\s+from\s+outside\s+the\s+game\s+or\s+choose\s+a\s+face-up\s+artifact\s+card\s+you\s+own\s+in\s+exile\.\s+Put\s+that\s+card\s+into\s+your\s+hand\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces a single
  /// OptionalEffect, but is matched via the multi-sentence path.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim();

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    // CR 400.11: bringing a card from outside the game (sideboard) into hand.
    // CR 406: exile zone; face-up qualifier applies to the exile branch.
    var wishEffect = new WishEffect
    {
      CardFilter = new ObjectFilter
      {
        CardTypes = ["artifact"],
        Owner = ControllerFilter.You,
      },
      Sources = [WishSource.OutsideGame, WishSource.Exile],
      FaceUpOnly = true,
    };

    effects = [new OptionalEffect { Inner = wishEffect }];
    return true;
  }
}
