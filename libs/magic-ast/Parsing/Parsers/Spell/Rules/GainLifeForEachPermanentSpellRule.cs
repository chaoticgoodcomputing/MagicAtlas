namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You gain N life for each [permanent type] you control." — spell-resolution
/// life-gain scaled by a count of permanents the caster controls (e.g. Bountiful
/// Harvest: "You gain 1 life for each land you control.").
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The "for each &lt;type&gt; you
/// control" count is recorded descriptively (reference-not-resolution, ADR 0004)
/// as a <see cref="CountQuantity"/> over an <see cref="ObjectFilter"/> — MAST does
/// not resolve the number, mirroring the amount shape on
/// <see cref="AddManaForEachTappedLandOpponentsControlRule"/>. Since the rate is
/// exactly 1 life per counted object, the amount is the bare count (1 x count =
/// count); a rate other than 1 (e.g. "gain 2 life for each …") is out of scope for
/// this rule's target but the leading count is still captured for family
/// robustness.
/// </para>
/// </summary>
[SpellRule]
public sealed class GainLifeForEachPermanentSpellRule : ISpellRule
{
  private const string CountTokens =
    @"X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten";

  private const string TypeTokens =
    @"land|creature|artifact|enchantment|planeswalker|permanent";

  private static readonly Regex Pattern = new(
    $@"^You\s+gain\s+(?<amount>{CountTokens})\s+life\s+for\s+each\s+(?<type>{TypeTokens})\s+you\s+control\.?$",
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

    var type = m.Groups["type"].Value.ToLowerInvariant();
    var count = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = [type],
        Zone = Zone.Battlefield,
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
