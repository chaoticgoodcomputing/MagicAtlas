namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create X [P]/[T] [color|colorless] [Subtype] [artifact] creature tokens,
/// where X is the number of [Subtype]s you control." — the triggered-ability
/// sibling of the activated Krenko, Mob Boss pattern
/// (<see cref="Activated.Rules.CreateTokenWhereXCountEffectRule"/>), reached here
/// from an attack trigger (Myrel, Shield of Argive: "Whenever Myrel attacks,
/// create X 1/1 colorless Soldier artifact creature tokens, where X is the
/// number of Soldiers you control.").
///
/// <para>
/// CR 111.2 (token creation): X is not a free variable — it is immediately
/// resolved to a <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/>
/// counting the named subtype the controller controls, mirroring the activated
/// sibling exactly.
/// </para>
///
/// <para>
/// Adds the "colorless ... artifact creature" shape
/// (<c>Colors=["C"], Types=["artifact","creature"]</c>) alongside the colored
/// plain-creature shape the activated sibling already handles, matching the
/// established colorless-token convention (CR 105.1; e.g.
/// <see cref="Activated.Rules.CreateColorlessArtifactCreatureTokenEffectRule"/>).
/// </para>
///
/// Priority above the generic <see cref="CreateTokenRule"/> (default 50) so this
/// more-specific "where X is …" shape is tried first — the generic rule has no
/// "where X is" handling and would otherwise silently emit a fixed literal count
/// of 1 instead of the derived count. Fully anchored (^…$).
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CreateTokenWhereXCountTriggeredRule : ITriggeredRule
{
  // "Create X 1/1 colorless Soldier artifact creature tokens, where X is the number of Soldiers you control."
  private static readonly Regex _pattern = new(
    @"^Create\s+X\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green|colorless)\s+"
      + @"(?<subtype>[A-Za-z]+)\s+(?<artifact>artifact\s+)?creature\s+tokens?\s*,\s+where\s+X\s+is\s+the\s+"
      + @"number\s+of\s+(?<countSubtype>[A-Za-z]+)s?\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var colorWord = m.Groups["color"].Value;
    List<string> colors;
    if (string.Equals(colorWord, "colorless", StringComparison.OrdinalIgnoreCase))
    {
      // CR 105.1: colorless is not a color; the token's Colors list carries the
      // "C" marker, mirroring CreateColorlessArtifactCreatureTokenEffectRule.
      colors = ["C"];
    }
    else if (_colorMap.TryGetValue(colorWord, out var colorCode))
    {
      colors = [colorCode];
    }
    else
    {
      return false;
    }

    var types = m.Groups["artifact"].Success ? new List<string> { "artifact", "creature" } : new List<string> { "creature" };

    var tokenSubtype = m.Groups["subtype"].Value;
    tokenSubtype = char.ToUpperInvariant(tokenSubtype[0]) + tokenSubtype[1..];

    var countSubtype = m.Groups["countSubtype"].Value;
    // Normalize the count subtype: the oracle text "Soldiers" should resolve to "Soldier".
    if (countSubtype.EndsWith("s", StringComparison.OrdinalIgnoreCase) && countSubtype.Length > 1)
    {
      countSubtype = countSubtype[..^1];
    }
    countSubtype = char.ToUpperInvariant(countSubtype[0]) + countSubtype[1..];

    effect = new CreateTokenEffect
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
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = colors,
        Types = types,
        Subtypes = [tokenSubtype],
        IsCopy = false,
      },
    };
    return true;
  }
}
