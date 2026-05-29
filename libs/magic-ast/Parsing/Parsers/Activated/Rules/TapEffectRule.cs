namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Tap [count] target [type]" — "Tap target creature", "Tap X target lands",
/// "Tap two target creatures" (Rule 701.21). For variable-X abilities the X in
/// cost and effect refer to the same chosen value (Rule 107.3b/c).
/// </summary>
[ActivatedEffectRule(Priority = 994)]
public sealed class TapEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.');
    var lower = text.ToLowerInvariant();

    if (!lower.StartsWith("tap "))
    {
      return null;
    }

    // Strip leading "tap " before parsing count + target noun.
    var rest = text[4..].Trim();
    var restLower = rest.ToLowerInvariant();

    Quantity? count = null;
    var quantityMatch = Regex.Match(
      rest,
      @"^(X|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+target\b",
      RegexOptions.IgnoreCase
    );
    if (quantityMatch.Success)
    {
      var qStr = quantityMatch.Groups[1].Value;
      if (string.Equals(qStr, "X", StringComparison.OrdinalIgnoreCase))
      {
        count = VariableQuantity.X;
      }
      else
      {
        var n = ActivatedRuleHelpers.ParseNumberWord(qStr) ?? int.Parse(qStr);
        count = LiteralQuantity.Of(n);
      }
    }
    else if (restLower.StartsWith("enchanted "))
    {
      // "Tap enchanted <type>" — CR 701.26a: tap the permanent this Aura is attached to.
      // The attached object is referenced as EnchantedOrEquipped (no target keyword; not a
      // targeted ability — the Aura is already attached at activation time).
      return new TapEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      };
    }
    else if (!restLower.StartsWith("target"))
    {
      // We can't recognize what's between "tap" and "target" — bail.
      return null;
    }

    // Match "target X or Y" (two card types) or "target X" (single card type).
    var orTargetMatch = Regex.Match(
      rest,
      @"\btarget\s+(\w+)\s+or\s+(\w+)",
      RegexOptions.IgnoreCase
    );
    if (orTargetMatch.Success)
    {
      var noun1 = orTargetMatch.Groups[1].Value.ToLowerInvariant();
      if (noun1.EndsWith("s") && noun1.Length > 1)
      {
        noun1 = noun1[..^1];
      }
      var noun2 = orTargetMatch.Groups[2].Value.ToLowerInvariant();
      if (noun2.EndsWith("s") && noun2.Length > 1)
      {
        noun2 = noun2[..^1];
      }
      var orFilter = new ObjectFilter { CardTypes = [noun1, noun2] };
      return new TapEffect { Target = ObjectReference.Target(orFilter), Count = count };
    }

    var targetMatch = Regex.Match(
      rest,
      @"\btarget\s+(\w+)",
      RegexOptions.IgnoreCase
    );
    if (!targetMatch.Success)
    {
      return null;
    }
    var noun = targetMatch.Groups[1].Value.ToLowerInvariant();
    if (noun.EndsWith("s") && noun.Length > 1)
    {
      noun = noun[..^1];
    }

    var filter = new ObjectFilter { CardTypes = [noun] };
    var target = ObjectReference.Target(filter);

    return new TapEffect { Target = target, Count = count };
  }
}
