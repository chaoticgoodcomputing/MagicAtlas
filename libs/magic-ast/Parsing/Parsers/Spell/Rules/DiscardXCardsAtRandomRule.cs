namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player discards X cards at random." (Mind Shatter) and the symmetric
/// "Target opponent discards X cards at random." shape — the variable-count,
/// random-selection sibling of <see cref="DiscardTargetPlayerRule"/>.
///
/// <para>
/// The discarder is the targeted player/opponent (CR 115.1 — "target" creates a
/// targeting requirement); the count is the game-defined variable X, most often
/// bound to an {X} cost paid on cast (CR 107.3). CR 701.9a: "To discard a card,
/// move it from its owner's hand to that player's graveyard." A trailing
/// "at random" makes the selection of which cards leave the hand random rather
/// than the discarding player's choice, recorded on <see cref="DiscardCardsEffect.Random"/>.
/// </para>
///
/// <para>
/// Anchored (<c>^…$</c>) and disjoint from <see cref="DiscardTargetPlayerRule"/>:
/// that rule's count group only recognises the small-word/digit vocabulary (via
/// <see cref="SpellRuleHelpers.TryParseSmallWord"/>, which has no "X" case) and its
/// pattern ends right after "card(s)" with no room for a trailing "at random"
/// clause, so neither rule can match the other's surface text.
/// </para>
/// </summary>
[SpellRule]
public sealed class DiscardXCardsAtRandomRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+(?<subject>player|opponent)\s+discards?\s+X\s+cards?\s+at\s+random\.?$",
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

    var isOpponent = m.Groups["subject"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);

    var player = isOpponent
      ? new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["opponent"] },
        }
      : new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["player"] },
        };

    effect = new DiscardCardsEffect
    {
      Count = VariableQuantity.X,
      Player = player,
      Random = true,
    };
    return true;
  }
}
