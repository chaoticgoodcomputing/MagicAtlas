namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target [color1] or [color2] permanent." — Celestial Purge.
/// Priority 80: must override <see cref="ExileTypeDisjunctionRule"/> whose regex
/// would otherwise greedily capture "black or red permanent" as three card types.
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ExileColorDisjunctionPermanentRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Exile\s+target\s+(?<c1>white|blue|black|red|green)\s+or\s+(?<c2>white|blue|black|red|green)\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var colors = new List<string>();
    foreach (var word in new[] { m.Groups["c1"].Value, m.Groups["c2"].Value })
    {
      var (mappedColors, _, _) = SpellRuleHelpers.MapColorWord(word);
      if (mappedColors is null)
      {
        continue;
      }
      foreach (var c in mappedColors)
      {
        if (!colors.Contains(c))
        {
          colors.Add(c);
        }
      }
    }
    if (colors.Count < 2)
    {
      return false;
    }

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [m.Groups["type"].Value.ToLowerInvariant()],
          Colors = colors,
        },
      },
    };
    return true;
  }
}
