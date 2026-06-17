namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create that many [P/T] [color] [Subtype] creature tokens" — token creation where the
/// quantity is derived from the triggering event's counter count (Nest of Scarabs).
///
/// <para>
/// "That many" is an anaphoric reference to the number of counters placed in the
/// triggering event — the antecedent of "that many" is the count from a
/// <see cref="MagicAST.AST.Triggers.TriggerEvent.CounterPlaced"/> trigger (CR 122.1:
/// counters are markers placed on objects; the count placed in a single event is the
/// antecedent). Modelled as <see cref="DerivedQuantity"/> keyed on
/// <see cref="DerivedKind.CountersPlaced"/>, the counter-placement analog of
/// <see cref="DerivedKind.DamageDealt"/> and <see cref="DerivedKind.LifeLost"/>.
/// </para>
///
/// <para>
/// Rule 111.1: "A token is a marker used to represent any permanent that isn't represented
/// by a card." Rule 603.2: the triggering event fires the ability; the effect clause
/// creates tokens equal to the number of counters placed.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class CreateThatManyTokensRule : ITriggeredRule
{
  // "create that many [P/T] [color] [Subtype] creature tokens"
  // e.g. "create that many 1/1 black Insect creature tokens"
  private static readonly Regex _pattern = new(
    @"^create\s+that\s+many\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>[A-Z][a-z]+)\s+creature\s+tokens?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorMap =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    if (!_colorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    // Normalise capitalisation: first letter upper, rest lower.
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.CountersPlaced },
      Token = new TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        IsCopy = false,
      },
    };
    return true;
  }
}
