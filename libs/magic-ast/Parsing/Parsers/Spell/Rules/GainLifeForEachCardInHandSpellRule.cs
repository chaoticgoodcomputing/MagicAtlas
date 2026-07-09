namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life for each card in your hand." — spell-resolution life-gain
/// scaled by a count of cards in the caster's own hand (Gerrard's Wisdom: "You
/// gain 2 life for each card in your hand.").
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The "for each card in your
/// hand" count is recorded descriptively (reference-not-resolution, ADR 0004)
/// as a <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/>
/// (<c>CardTypes: ["card"], Zone: Hand, Controller: You</c>) — MAST does not
/// resolve the number, mirroring the hand-count shape on
/// <see cref="AddManaForEachCardInHandRule"/> (which counts a targeted
/// opponent's hand rather than the caster's own). Sibling of
/// <see cref="GainLifeForEachPermanentSpellRule"/> (which counts permanents on
/// the battlefield rather than cards in hand); this rule owns the "in your
/// hand" zone specifically.
/// </para>
/// </summary>
[SpellRule]
public sealed class GainLifeForEachCardInHandSpellRule : ISpellRule
{
  private const string CountTokens =
    @"X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten";

  private static readonly Regex Pattern = new(
    $@"^You\s+gain\s+(?<amount>{CountTokens})\s+life\s+for\s+each\s+card\s+in\s+your\s+hand\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var count = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["card"],
        Zone = Zone.Hand,
        Controller = ControllerFilter.You,
      },
    };

    effect = new GainLifeEffect
    {
      Amount = BuildAmount(m.Groups["amount"].Value, count),
      Player = ObjectReference.You(),
    };
    return true;
  }

  private static Quantity BuildAmount(string rawAmount, CountQuantity count)
  {
    var lower = rawAmount.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      // A variable rate ("gain X life for each …") is not expressible as a
      // simple multiply; fall back to the bare count since no target card
      // exercises this shape.
      return count;
    }

    var n = SpellRuleHelpers.ParseSmallWord(rawAmount);
    if (n == 1)
    {
      return count;
    }

    return new CalculatedQuantity
    {
      Operation = "multiply",
      Operand = n,
      BaseQuantity = count,
    };
  }
}
