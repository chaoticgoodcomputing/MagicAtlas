namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Return up to (one|two|three|…) target [filter] cards from your graveyard to your hand."
/// Covers the bounded multi-target graveyard-retrieval pattern where the count is an
/// "up to N" ceiling, e.g. Soul Salvage / March of the Returned.
/// Source zone on <see cref="ObjectFilter.Zone"/>; count on <see cref="ObjectReference.Quantity"/>.
/// </summary>
[SpellRule]
public sealed class ReturnMultipleFromGraveyardRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Return\s+up\s+to\s+(?<n>one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+target\s+(?<filter>permanent|creature|artifact|enchantment|land|card|instant|sorcery|instant\s+or\s+sorcery|nonland\s+permanent)\s+cards?\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
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
    var filterText = m.Groups["filter"].Value.ToLowerInvariant();

    var cardTypes = filterText switch
    {
      "permanent" or "nonland permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      "instant" => new List<string> { "instant" },
      "sorcery" => new List<string> { "sorcery" },
      "instant or sorcery" => new List<string> { "instant", "sorcery" },
      "card" => new List<string> { "card" },
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
          Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
      },
    };
    return true;
  }
}
