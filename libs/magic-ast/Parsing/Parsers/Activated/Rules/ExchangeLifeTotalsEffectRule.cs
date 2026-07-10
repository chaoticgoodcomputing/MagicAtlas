namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Exchange life totals with target opponent." — Magus of the Mirror.
///
/// <para>
/// CR 701.12a: "A spell or ability may instruct players to exchange something (for
/// example, life totals or control of two permanents) as part of its resolution."
/// CR 701.12c: "When life totals are exchanged, each player gains or loses the amount
/// of life necessary to equal the other player's previous life total."
/// </para>
///
/// <para>
/// Reuses the shared <see cref="ExchangeCharacteristicEffect"/> with its pre-existing
/// <see cref="ExchangeableCharacteristic.LifeTotals"/> facet — no new discriminator.
/// The exchange is between the controller (the implicit first party, "you") and the
/// named target opponent, so <c>First = You</c> and <c>Second = Opponent</c>. The
/// "target" keyword on the opponent is carried by the <see cref="ObjectReferenceKind.Opponent"/>
/// reference itself, matching how "target opponent" is modeled elsewhere (e.g. Blood
/// Tribute).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 620)]
public sealed class ExchangeLifeTotalsEffectRule : IActivatedEffectRule
{
  // Anchored (^…$): the entire trimmed, period-stripped effect sentence must be exactly
  // "Exchange life totals with target opponent" so this rule can never claim a substring
  // of a larger "exchange …" sentence.
  private static readonly Regex Pattern = new(
    @"^Exchange\s+life\s+totals\s+with\s+target\s+opponent$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new ExchangeCharacteristicEffect
    {
      Characteristic = ExchangeableCharacteristic.LifeTotals,
      First = ObjectReference.You(),
      Second = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
    };
  }
}
