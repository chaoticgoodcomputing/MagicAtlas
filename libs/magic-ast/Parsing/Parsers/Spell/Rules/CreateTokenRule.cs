namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;

/// <summary>
/// "Create (a|X|&lt;num&gt;) &lt;P&gt;/&lt;T&gt; &lt;color&gt; &lt;subtype&gt; creature token(s)."
/// Handles literal counts ("a"), variable counts ("X"), and numeric literals.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CreateTokenRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> ColorMap = new Dictionary<string, string>(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var rawCount = m.Groups["count"].Value;
    Quantity count;
    var rawLower = rawCount.ToLowerInvariant();
    if (rawLower is "x" or "y" or "z")
    {
      count = new VariableQuantity { Name = rawLower.ToUpperInvariant() };
    }
    else
    {
      count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(rawCount));
    }

    var colorCode = ColorMap[m.Groups["color"].Value];
    var power = m.Groups["power"].Value;
    var toughness = m.Groups["toughness"].Value;
    // Capitalize first letter of subtype to match canonical MTG form.
    var subtype = m.Groups["subtype"].Value;
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    effect = new CreateTokenEffect
    {
      Count = count,
      Token = new TokenDefinition
      {
        Power = power,
        Toughness = toughness,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        IsCopy = false,
      },
      IsOptional = false,
    };
    return true;
  }
}
