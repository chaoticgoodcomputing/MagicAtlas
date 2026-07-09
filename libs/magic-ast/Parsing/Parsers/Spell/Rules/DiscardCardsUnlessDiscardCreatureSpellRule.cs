namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Then ]discard [N] cards unless you discard a creature card." — the discard-tax
/// tail of the "Draw three cards. Then discard two cards unless you discard a creature
/// card." loot-with-alternative pattern (Mystic Meditation). The leading "Then" is the
/// sentence-bundle connective linking this clause to the preceding draw; it carries no
/// extra semantics and is peeled optionally so this rule fires on the split-out sentence.
///
/// <para>
/// The "unless you discard a creature card" gate is a cost-or-consequence: discarding
/// the N cards happens unless its controller chooses to pay the stated cost, and paying
/// a cost is never automatic. CR 118.1: "A cost is an action or payment necessary to
/// take another action or to stop another action from taking place." Here the stated
/// cost — discarding one creature card — is the action that stops the N-card discard
/// from taking place. CR 701.9a: "To discard a card, move it from its owner's hand to
/// that player's graveyard."
/// </para>
///
/// <para>
/// Produces a <see cref="PreventableEffect"/> whose Inner is a
/// <see cref="DiscardCardsEffect"/> (Player = You, Count = N) and whose
/// <see cref="UnlessClause"/> Player is You and Cost is a one-card
/// <see cref="DiscardCost"/> filtered to a creature card — structurally identical to
/// the ETB sacrifice-tax (<c>SacrificeItUnlessDiscardRandomRule</c>, Balduvian Horde)
/// except the inner effect is a discard and the unless-cost is a filtered discard.
/// </para>
///
/// <para>
/// Representative card: Mystic Meditation (MMQ). Rule citations: 118.1 (a cost stops an
/// action from taking place), 701.9a (Discard).
/// </para>
/// </summary>
[SpellRule]
public sealed class DiscardCardsUnlessDiscardCreatureSpellRule : ISpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  private static readonly Regex _pattern = new(
    $@"^(?:Then\s+)?discard\s+(?<count>{CountTokens})\s+cards?\s+unless\s+you\s+discard\s+a\s+creature\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var count = SpellRuleHelpers.ParseSmallWord(m.Groups["count"].Value);

    var inner = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      Random = false,
    };

    var unless = new UnlessClause
    {
      Player = ObjectReference.You(),
      Cost = new DiscardCost
      {
        Filter = new ObjectFilter { CardTypes = ["creature"] },
        Quantity = LiteralQuantity.Of(1),
      },
    };

    effect = EffectWrap.Preventable(inner, unless);
    return true;
  }
}
