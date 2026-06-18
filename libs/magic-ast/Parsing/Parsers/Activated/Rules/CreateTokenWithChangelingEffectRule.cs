namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Create a [P/T] [color] [Subtype] creature token with changeling." — activated
/// creature-token creation where the token bears the Changeling keyword.
///
/// <para>
/// Maskwood Nexus pattern (CR 111 — tokens; CR 702.73 — changeling):
/// "{3}, {T}: Create a 2/2 blue Shapeshifter creature token with changeling."
/// The token's Changeling ability is modelled as a <see cref="StaticAbility"/>
/// with <see cref="KeywordAbilityEffect"/>{Changeling}, mirroring the
/// <see cref="MagicAST.Keywords.Definitions.ChangelingKeyword"/> combinator shape.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the "with changeling" tail is distinctive but substring-safe
/// only when anchored. The generic <see cref="CreateTokenEffectRule"/> (priority
/// 986) does not match "with changeling" because its regex is anchored and ends at
/// "creature token[s]$"; this rule fires first (priority 987) for the changeling
/// variant, ensuring the generic rule never sees it.
/// </para>
///
/// <para>
/// Priority 987 — one above the generic <see cref="CreateTokenEffectRule"/> (986)
/// so this more-specific shape is tried first.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class CreateTokenWithChangelingEffectRule : IActivatedEffectRule
{
  // "Create a [count] [P]/[T] [color] [Subtype] creature token with changeling."
  // Anchored ^ and $ to prevent substring matches inside broader effect sentences.
  private static readonly Regex _pattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+token\s+with\s+changeling\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  /// <summary>
  /// The Changeling keyword ability modelled as a static ability on the token,
  /// mirroring the ChangelingKeyword combinator shape (CR 702.73a).
  /// </summary>
  private static readonly StaticAbility _changelingTokenAbility = new()
  {
    KeywordSource = KeywordAbility.Changeling,
    Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Changeling }],
  };

  public Effect? TryMatch(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim() + ".";
    var m = _pattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new VariableQuantity { Name = rawCount.ToUpperInvariant() },
      "a" or "one" => LiteralQuantity.Of(1),
      "two" => LiteralQuantity.Of(2),
      "three" => LiteralQuantity.Of(3),
      "four" => LiteralQuantity.Of(4),
      "five" => LiteralQuantity.Of(5),
      _ => LiteralQuantity.Of(int.TryParse(rawCount, out var n) ? n : 1),
    };

    if (!_colorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var subtype = m.Groups["subtype"].Value;
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    return new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        Abilities = [_changelingTokenAbility],
        IsCopy = false,
      },
    };
  }
}
