namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [filter] card [with mana value N or less/greater/more/fewer] from
/// your graveyard to the battlefield." Reanimation (CR 701.17-adjacent zone change /
/// CR 608): the target card moves from its owner's graveyard onto the battlefield.
/// Source zone on <see cref="ObjectFilter.Zone"/>; the optional mana-value qualifier
/// (e.g. Yathan Roadwatcher's "with mana value 3 or less") lands on
/// <see cref="ObjectFilter.ManaValueComparison"/>. Fully anchored (^…$) so the
/// optional qualifier cannot let the pattern substring-match a more specific sibling.
/// </summary>
[SpellRule]
public sealed class ReturnGraveyardToBattlefieldRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+(?<filter>permanent|creature|artifact|enchantment|land|card|nonland\s+permanent)\s+card"
    + @"(?:\s+with\s+mana\s+value\s+(?<mv>\d+)\s+or\s+(?<mvdir>less|fewer|greater|more))?"
    + @"\s+from\s+your\s+graveyard\s+to\s+the\s+battlefield$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var filterText = m.Groups["filter"].Value.ToLowerInvariant();
    var cardTypes = filterText switch
    {
      "permanent" or "nonland permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      "card" => new List<string> { "card" },
      _ => new List<string> { "card" },
    };
    var characteristics =
      filterText == "nonland permanent" ? new List<string> { "nonland" } : null;

    Comparison? manaValueComparison = null;
    if (m.Groups["mv"].Success)
    {
      var value = int.Parse(m.Groups["mv"].Value);
      var op = m.Groups["mvdir"].Value.ToLowerInvariant() switch
      {
        "less" or "fewer" => ComparisonOperator.LessThanOrEqual,
        "greater" or "more" => ComparisonOperator.GreaterThanOrEqual,
        _ => ComparisonOperator.LessThanOrEqual,
      };
      manaValueComparison = new Comparison { Operator = op, Value = value };
    }

    effect = new ReturnToBattlefieldEffect
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
          ManaValueComparison = manaValueComparison,
        },
      },
    };
    return true;
  }
}
