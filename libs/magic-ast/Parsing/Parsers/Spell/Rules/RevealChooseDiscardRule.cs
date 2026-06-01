namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Targeted reveal-choose-discard — the Thoughtseize / Coercion / Thought Erasure family:
/// "Target opponent reveals their hand. You choose a nonland card from it. That player
/// discards that card."
///
/// <para>
/// These three sentences are ONE coupled game action, not three independent effects: the
/// card "you choose" from the revealed hand is the very card "that player discards". So
/// this is a single <see cref="DiscardCardsEffect"/> capturing all three structured axes
/// rather than a decomposed sentence-bundle —
/// <list type="bullet">
///   <item><c>Player</c> (the discarder): <c>Target</c> opponent — mirrors
///     <see cref="DiscardTargetPlayerRule"/>'s "target opponent discards" encoding;</item>
///   <item><c>Chooser</c>: "you" (<c>ObjectReferenceKind.You</c>) — distinct from the
///     discarder;</item>
///   <item><c>Filter</c>: "a nonland card" → <c>CardTypes=["card"]</c> +
///     <c>ExcludedCardTypes=["land"]</c>;</item>
///   <item><c>RevealHand=true</c>: the opponent reveals their hand first.</item>
/// </list>
/// </para>
///
/// <para>
/// CR 701.9a (verbatim): "To discard a card, move it from its owner's hand to that player's
/// graveyard."
/// </para>
/// </summary>
[SpellRule]
public sealed class RevealChooseDiscardRule : ISpellRule
{
  // The dispatcher hands us the line trimmed of its trailing period; the inner
  // sentence-boundary periods remain. Match the whole three-sentence instruction.
  private static readonly Regex _pattern = new(
    @"^Target\s+opponent\s+reveals\s+their\s+hand\.\s+"
      + @"You\s+choose\s+a\s+nonland\s+card\s+from\s+it\.\s+"
      + @"That\s+player\s+discards\s+that\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["opponent"] },
      },
      Chooser = new ObjectReference { Kind = ObjectReferenceKind.You },
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        ExcludedCardTypes = ["land"],
      },
      RevealHand = true,
      Random = false,
    };
    return true;
  }
}
