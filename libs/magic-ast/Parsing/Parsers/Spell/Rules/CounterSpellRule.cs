namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Counter target spell." with optional color/type qualifiers and an optional
/// "unless its controller pays {X}" tail. Rule 701.6.
/// </summary>
[SpellRule]
public sealed class CounterSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Counter\s+target\s+(?<filter>(?<color>colorless|multicolored|white|blue|black|red|green)?(?:\s+or\s+(?<color2>white|blue|black|red|green))?\s*(?<type>instant|sorcery|creature|noncreature)?\s*spell(\s+with\s+converted\s+mana\s+cost.*)?)(?:\s+unless\s+its\s+controller\s+pays\s+\{(?<unlessx>[A-Za-z])\})?$",
      RegexOptions.IgnoreCase
    );
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
    var filter = SpellRuleHelpers.BuildSpellFilter(m.Groups["filter"].Value, colorWords);
    UnlessClause? unless = null;
    if (m.Groups["unlessx"].Success)
    {
      unless = new UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
        Cost = new ManaCost { Symbols = [new ManaSymbol { Kind = ManaSymbolKind.Variable }] },
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
