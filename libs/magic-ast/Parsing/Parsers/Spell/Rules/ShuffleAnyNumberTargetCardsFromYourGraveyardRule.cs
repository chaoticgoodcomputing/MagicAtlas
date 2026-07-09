namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Shuffle any number of target [filter] cards from your graveyard into your
/// library." (Piper's Melody). The controller-performed, unbounded-subset
/// graveyard-to-library recycle keyword action (CR 701.24) — the imperative
/// "you shuffle your own graveyard" sibling of the targeted-player template
/// spelled out in CR 701.24's own worked example (Loaming Shaman: "When this
/// creature enters, target player shuffles any number of target cards from their
/// graveyard into their library"). Here there is no targeted player: the
/// controller (<see cref="ObjectReferenceKind.You"/>) performs the shuffle of
/// their own library (<see cref="ShuffleCardsFromGraveyardIntoLibraryEffect.Player"/>),
/// while the cards are a separately-targeted, unbounded selection from that
/// controller's own graveyard (<see cref="ShuffleCardsFromGraveyardIntoLibraryEffect.Cards"/>,
/// <see cref="ControllerFilter.You"/> recording "your graveyard", and
/// <see cref="AnyAmountQuantity"/> recording the unbounded "any number of"
/// choice per CR 601.2c — the word "target" appears once, so the card subset is
/// the sole target).
///
/// <para>
/// Anchored (<c>^Shuffle …$</c>) to avoid shadowing the bounded "up to N"
/// <see cref="ShuffleTargetCardsFromGraveyardRule"/> sibling and the whole-zone
/// <see cref="ShuffleGraveyardIntoLibraryEffect"/>.
/// </para>
/// </summary>
[SpellRule]
public sealed class ShuffleAnyNumberTargetCardsFromYourGraveyardRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Shuffle\s+any\s+number\s+of\s+target\s+(?:(?<filter>creature|permanent|artifact|enchantment|land|instant|sorcery|planeswalker)\s+)?cards?\s+from\s+your\s+graveyard\s+into\s+your\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var filterText = m.Groups["filter"].Success
      ? m.Groups["filter"].Value.ToLowerInvariant()
      : "card";

    var cardTypes = new List<string> { filterText };

    effect = new ShuffleCardsFromGraveyardIntoLibraryEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.You },
      Cards = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
        Quantity = new AnyAmountQuantity(),
      },
    };
    return true;
  }
}
