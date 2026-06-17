namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create X P/T color subtype creature tokens, where X is the number of [subtype] you control."
///
/// Handles the Krenko, Mob Boss pattern (CR 111.2: token creation) where X is defined inline
/// as a count of permanents the controller has. Distinct from the plain X-count variant in
/// <see cref="CreateTokenEffectRule"/>: here X is not a free variable — it is immediately
/// resolved to a <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/>.
///
/// Rule priority is higher than <see cref="CreateTokenEffectRule"/> (986) so this more-specific
/// pattern is tried first.
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class CreateTokenWhereXCountEffectRule : IActivatedEffectRule
{
  // "Create X 1/1 red Goblin creature tokens, where X is the number of Goblins you control."
  private static readonly Regex _pattern = new(
    @"^Create\s+X\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens?\s*,\s+where\s+X\s+is\s+the\s+number\s+of\s+(?<countSubtype>\w+)s?\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();

    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    if (!_colorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var tokenSubtype = m.Groups["subtype"].Value;
    tokenSubtype = char.ToUpperInvariant(tokenSubtype[0]) + tokenSubtype[1..];

    var countSubtype = m.Groups["countSubtype"].Value;
    // Normalize the count subtype: the oracle text "Goblins" should resolve to "Goblin"
    // Strip a trailing 's' to singularize if needed (Goblins -> Goblin)
    if (countSubtype.EndsWith("s", StringComparison.OrdinalIgnoreCase) && countSubtype.Length > 1)
    {
      countSubtype = countSubtype[..^1];
    }
    countSubtype = char.ToUpperInvariant(countSubtype[0]) + countSubtype[1..];

    return new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          Subtypes = [countSubtype],
          Controller = ControllerFilter.You,
        },
      },
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [tokenSubtype],
        IsCopy = false,
      },
    };
  }
}
