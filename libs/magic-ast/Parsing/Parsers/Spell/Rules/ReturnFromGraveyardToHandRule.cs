namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [filter] card from your graveyard to your hand."
/// Also handles the untyped form: "Return target card from your graveyard to your hand."
/// Source zone on <see cref="ObjectFilter.Zone"/>.
/// </summary>
[SpellRule]
public sealed class ReturnFromGraveyardToHandRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Return\s+target\s+(?:(?<filter>permanent|creature|artifact|enchantment|land|nonland\s+permanent)\s+)?card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    // filter group is empty for the untyped "target card" form (e.g. Regrowth)
    var filterText = m.Groups["filter"].Value.ToLowerInvariant();
    var cardTypes = filterText switch
    {
      "permanent" or "nonland permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      _ => new List<string> { "card" },
    };
    var characteristics =
      filterText == "nonland permanent" ? new List<string> { "nonland" } : null;

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Characteristics = characteristics,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
