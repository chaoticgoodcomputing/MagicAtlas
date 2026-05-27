namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Counter target spell." with optional color/type qualifiers and an optional
/// "unless its controller pays {cost}" tail. Rule 701.6.
/// Handles these filter dimensions in one consolidated surface:
///   - color words (white, blue, black, red, green, colorless, multicolored)
///   - non&lt;color&gt; predicates (nonblue, nonred, …) → Characteristics
///   - bare card-type qualifier before "spell" (artifact, land, enchantment, …) → CardTypes
///   - "with mana value N" → ManaValueComparison
///   - "unless its controller pays {cost}" where cost is any mana expression
///     ({1}, {X}, {1}{U}, etc.) — parsed via ManaCostParser
/// </summary>
[SpellRule]
public sealed class CounterSpellRule : ISpellRule
{
  // Pattern breakdown:
  //   optional non<color>  e.g. "nonblue"
  //   optional color word  e.g. "white", "colorless", "multicolored"
  //   optional "or <color2>"
  //   optional card-type qualifier  e.g. "artifact", "instant", "creature"
  //   literal "spell"
  //   optional "with mana value N"
  //   optional "unless its controller pays {cost}"  — captures full mana expression
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+"
    + @"(?<noncolor>non(?:white|blue|black|red|green))?\s*"
    + @"(?<color>colorless|multicolored|white|blue|black|red|green)?"
    + @"(?:\s+or\s+(?<color2>white|blue|black|red|green))?\s*"
    + @"(?<cardtype>artifact|enchantment|land|instant|sorcery|creature|noncreature|permanent)?\s*"
    + @"spell"
    + @"(?:\s+with\s+mana\s+value\s+(?<mv>\d+))?"
    + @"(?:\s+unless\s+its\s+controller\s+pays\s+(?<unless>(?:\{[^}]+\})+))?\.?$",
    RegexOptions.IgnoreCase
  );

  private readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var colorWords = new List<string>();
    if (m.Groups["color"].Success)
    {
      colorWords.Add(m.Groups["color"].Value);
    }
    if (m.Groups["color2"].Success)
    {
      colorWords.Add(m.Groups["color2"].Value);
    }

    var nonColorWord = m.Groups["noncolor"].Success ? m.Groups["noncolor"].Value : null;
    var cardTypeWord = m.Groups["cardtype"].Success ? m.Groups["cardtype"].Value : null;

    Comparison? manaValueComparison = null;
    if (m.Groups["mv"].Success && int.TryParse(m.Groups["mv"].Value, out var mvValue))
    {
      manaValueComparison = new Comparison { Operator = ComparisonOperator.Equal, Value = mvValue };
    }

    var filter = SpellRuleHelpers.BuildSpellFilter(colorWords, nonColorWord, cardTypeWord, manaValueComparison);

    UnlessClause? unless = null;
    if (m.Groups["unless"].Success)
    {
      var parsed = _manaCostParser.Parse(m.Groups["unless"].Value);
      unless = new UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
        Cost = new ManaCost { Symbols = [.. parsed.Symbols] },
      };
    }
    effect = new CounterSpellEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      UnlessClause = unless,
    };
    return true;
  }
}
