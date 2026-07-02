namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;

/// <summary>
/// "Create a [P]/[T] [color] [subtype] creature token." and the predefined
/// artifact-token form "Create [count] [Treasure|Food|Clue|Blood] token(s)."
/// Token creation as an activated-ability effect (Rule 111).
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class CreateTokenEffectRule : IActivatedEffectRule
{
  private static readonly Regex _createTokenPattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _createPredefinedTokenPattern = new(
    @"^Create\s+(?<count>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<subtype>Treasure|Food|Clue|Blood)\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _activatedColorMap = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();

    // --- Predefined artifact token: Treasure, Food, Clue, Blood ---
    var predefined = _createPredefinedTokenPattern.Match(stripped);
    if (predefined.Success)
    {
      var rawPCount = predefined.Groups["count"].Value.ToLowerInvariant();
      int pCount = rawPCount switch
      {
        "a" or "an" or "one" => 1,
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ => int.TryParse(rawPCount, out var pn) ? pn : 1,
      };
      var subtype = predefined.Groups["subtype"].Value;
      // Title-case normalize
      subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();
      var tokenFactory = subtype switch
      {
        "Treasure" => MagicAST.AST.Effects.TokenDefinition.Treasure(),
        "Food" => MagicAST.AST.Effects.TokenDefinition.Food(),
        "Clue" => MagicAST.AST.Effects.TokenDefinition.Clue(),
        "Blood" => MagicAST.AST.Effects.TokenDefinition.Blood(),
        _ => null,
      };
      if (tokenFactory is not null)
      {
        return new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
        {
          Player = MagicAST.AST.References.ObjectReference.You(),
          Count = MagicAST.AST.Quantities.LiteralQuantity.Of(pCount),
          Token = tokenFactory,
        };
      }
    }

    // --- Standard creature token: "Create [count] P/T color subtype creature token(s)" ---
    var m = _createTokenPattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    MagicAST.AST.Quantities.Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new MagicAST.AST.Quantities.VariableQuantity { Name = rawCount.ToUpperInvariant() },
      "a" or "one" => MagicAST.AST.Quantities.LiteralQuantity.Of(1),
      "two" => MagicAST.AST.Quantities.LiteralQuantity.Of(2),
      "three" => MagicAST.AST.Quantities.LiteralQuantity.Of(3),
      _ => MagicAST.AST.Quantities.LiteralQuantity.Of(int.TryParse(rawCount, out var ctn) ? ctn : 1),
    };

    if (!_activatedColorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var ctSubtype = m.Groups["subtype"].Value;
    ctSubtype = char.ToUpperInvariant(ctSubtype[0]) + ctSubtype[1..];

    return new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
    {
      Player = MagicAST.AST.References.ObjectReference.You(),
      Count = count,
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [ctSubtype],
        IsCopy = false,
      },
    };
  }
}
