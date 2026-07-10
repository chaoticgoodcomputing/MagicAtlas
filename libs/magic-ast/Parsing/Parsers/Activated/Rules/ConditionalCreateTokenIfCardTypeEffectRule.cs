namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "If it was a [card type] card, create [count] [P/T] [color] [subtype(s)]
/// creature token(s)." — the conditional payoff that trails a graveyard-exile
/// sentence, e.g. General's Enforcer: "{2}{W}{B}: Exile target card from a
/// graveyard. If it was a creature card, create a 1/1 white Human Soldier
/// creature token."
///
/// <para>
/// The preceding "Exile target card from a graveyard." sentence is handled by
/// <see cref="ExileFromGraveyardEffectRule"/>; the activated-ability parser
/// splits the effect half on sentence boundaries (period-space), so this rule
/// structures the SECOND sentence alone into a <see cref="ConditionalEffect"/>
/// gating a <see cref="CreateTokenEffect"/> (mirrors
/// <see cref="ConditionalUntapThatLandEffectRule"/>'s trailing-conditional
/// shape at the sentence level).
/// </para>
///
/// <para>
/// "it" is the card the preceding sentence exiled — a back-reference to the
/// object mentioned earlier in the same ability, modelled as
/// <see cref="ObjectReferenceKind.It"/> (reference-not-resolution, ADR 0004).
/// The card-type check is structured as
/// <see cref="ObjectHasCardTypeCondition"/> (CardType lowercase per the
/// <see cref="ObjectFilter.CardTypes"/> vocabulary, Subject="It") rather than
/// left as free text — CR 109.2/608.2h: after a card leaves the zone it was
/// checked in, its last known information (the type it had while in the
/// graveyard) is what's checked.
/// </para>
///
/// CR 111.1 (verbatim): "A token is a marker used to represent any permanent
/// that isn't represented by a card." CR 205.3m: creature subtypes such as
/// "Human" and "Soldier" may be printed in any combination on a single token.
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class ConditionalCreateTokenIfCardTypeEffectRule : IActivatedEffectRule
{
  // "If it was a <cardtype> card, create <count> <P>/<T> <color> <subtype(s)>
  // creature token(s)." Anchored (^…$) so it only ever matches the whole
  // sentence and can never claim a substring of a longer clause.
  private static readonly Regex _pattern = new(
    @"^If\s+it\s+was\s+an?\s+(?<cardtype>[a-z]+)\s+card,\s+create\s+" +
    @"(?<count>a|an|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+" +
    @"(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+" +
    @"(?<subtypes>[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)\s+creature\s+tokens?$",
    RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorMap = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var cardType = match.Groups["cardtype"].Value.ToLowerInvariant();

    var rawCount = match.Groups["count"].Value.ToLowerInvariant();
    Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new VariableQuantity { Name = rawCount.ToUpperInvariant() },
      "a" or "an" or "one" => LiteralQuantity.Of(1),
      "two" => LiteralQuantity.Of(2),
      "three" => LiteralQuantity.Of(3),
      "four" => LiteralQuantity.Of(4),
      "five" => LiteralQuantity.Of(5),
      "six" => LiteralQuantity.Of(6),
      "seven" => LiteralQuantity.Of(7),
      "eight" => LiteralQuantity.Of(8),
      "nine" => LiteralQuantity.Of(9),
      "ten" => LiteralQuantity.Of(10),
      _ => LiteralQuantity.Of(int.TryParse(rawCount, out var n) ? n : 1),
    };

    if (!_colorMap.TryGetValue(match.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var subtypes = match.Groups["subtypes"].Value
      .Split(' ', StringSplitOptions.RemoveEmptyEntries)
      .ToList();

    var createToken = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = count,
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = match.Groups["power"].Value,
        Toughness = match.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = subtypes,
        IsCopy = false,
      },
    };

    return new ConditionalEffect
    {
      Condition = new ObjectHasCardTypeCondition { CardType = cardType, Subject = "It" },
      Then = createToken,
    };
  }
}
