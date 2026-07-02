namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player shuffles up to (one|two|three|…) target [filter] cards from
/// their graveyard into their library." The bounded, targeted-subset shuffle
/// keyword action (CR 701.24) — Krosan Reclamation is the "up to two" variant
/// of the template spelled out in CR 701.24's own worked example (Loaming
/// Shaman: "target player shuffles any number of target cards from their
/// graveyard into their library"). Two independent targets are recorded per
/// CR 601.2c (the word "target" appears twice): the player who shuffles
/// (<see cref="ShuffleCardsFromGraveyardIntoLibraryEffect.Player"/>) and the
/// bounded card selection from that player's own graveyard
/// (<see cref="ShuffleCardsFromGraveyardIntoLibraryEffect.Cards"/>, with
/// <see cref="ControllerFilter.Target"/> recording the "their graveyard"
/// back-reference to the targeted player).
/// </summary>
[SpellRule]
public sealed class ShuffleTargetCardsFromGraveyardRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+player\s+shuffles\s+up\s+to\s+(?<n>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+target\s+(?:(?<filter>permanent|creature|artifact|enchantment|land|instant|sorcery)\s+)?cards?\s+from\s+their\s+graveyard\s+into\s+their\s+library$",
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

    var maximum = SpellRuleHelpers.ParseSmallWord(m.Groups["n"].Value);
    var filterText = m.Groups["filter"].Success
      ? m.Groups["filter"].Value.ToLowerInvariant()
      : "card";

    var cardTypes = filterText switch
    {
      "permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      "instant" => new List<string> { "instant" },
      "sorcery" => new List<string> { "sorcery" },
      _ => new List<string> { "card" },
    };

    effect = new ShuffleCardsFromGraveyardIntoLibraryEffect
    {
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = new List<string> { "player" } },
      },
      Cards = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.Target,
        },
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
      },
    };
    return true;
  }
}
