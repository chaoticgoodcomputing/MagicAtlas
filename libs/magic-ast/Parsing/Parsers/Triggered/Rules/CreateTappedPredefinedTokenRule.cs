namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a tapped [Subtype] token" — creates a predefined artifact token that enters the
/// battlefield tapped (Rule 111 — Tokens; CR 302.6 / 110.6b: a permanent enters tapped
/// when an effect instructs it to do so).
///
/// Handles the "create a tapped Treasure token" form produced by cards such as Nuka-Cola
/// Vending Machine (PIP). The "tapped" qualifier is recorded on the
/// <see cref="TokenDefinition.EntersTapped"/> field so consumers know the token enters
/// already tapped rather than untapped. This is a descriptive datum, not an engine tap
/// action (MAST describes what oracle text says, not engine execution).
///
/// Priority 96: sits above <see cref="CreateTokenRule"/> (default priority 50) so the
/// tapped variant is matched before the plain vanilla path, which would fail anyway because
/// it doesn't recognise "tapped" between the article and the subtype word.
///
/// CR 701.21a (Sacrifice), CR 111 (Tokens), CR 302.6 (enters tapped), CR 603.2
/// (triggered ability event matching).
/// </summary>
[TriggeredRule(Priority = 96)]
public sealed class CreateTappedPredefinedTokenRule : ITriggeredRule
{
  // "create a tapped [Subtype] token" — article + "tapped" + named subtype.
  private static readonly Regex _createTappedPattern = new(
    @"^create\s+a\s+tapped\s+(?<subtype>Treasure|Food|Clue|Blood)\s+token\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out MagicAST.AST.Effects.Effect? effect)
  {
    effect = null;

    var m = _createTappedPattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    // Title-case normalize
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    var token = subtype switch
    {
      "Treasure" => TokenDefinition.TappedTreasure(),
      "Food" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Food"],
        AbilityText = ["{2}, {T}, Sacrifice this artifact: You gain 3 life."],
        EntersTapped = true,
      },
      "Clue" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Clue"],
        AbilityText = ["{2}, Sacrifice this artifact: Draw a card."],
        EntersTapped = true,
      },
      "Blood" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Blood"],
        AbilityText = ["{1}, {T}, Discard a card, Sacrifice this artifact: Draw a card."],
        EntersTapped = true,
      },
      _ => null,
    };

    if (token is null)
    {
      return false;
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = token,
    };
    return true;
  }
}
