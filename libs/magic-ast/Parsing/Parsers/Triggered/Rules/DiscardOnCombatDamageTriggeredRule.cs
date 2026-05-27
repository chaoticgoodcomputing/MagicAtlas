namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player discards a card" — discard effect imposed on the player who
/// received combat damage. Fires as the effect clause of a
/// <c>DealsCombatDamageToPlayer</c> trigger (Rule 510 — Combat Damage Step).
/// "That player" refers back to the player mentioned in the trigger condition
/// (Rule 701.9 — Discard).
///
/// <para>
/// Covers the canonical single-card form. The N-card variant
/// ("that player discards N cards") is also handled: count is parsed from
/// a word-or-digit group so both shapes land on the same rule.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class DiscardOnCombatDamageTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var countRaw = m.Groups["count"].Value.ToLowerInvariant();
    var count = countRaw switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ when int.TryParse(countRaw, out var n) => n,
      _ => 1,
    };

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      IsOptional = false,
    };
    return true;
  }

  // "that player discards a card" / "that player discards two cards" / etc.
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+discards?\s+(?<count>a|one|two|three|four|five|\d+)\s+cards?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
