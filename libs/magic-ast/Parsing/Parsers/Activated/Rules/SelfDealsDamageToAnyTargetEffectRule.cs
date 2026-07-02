namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Two shapes — both map to self-as-source dealDamage:
/// <list type="bullet">
///   <item>"[Self] deals N damage to any target." → AnyTarget (CR 119.2: damage is dealt
///   by the source; "any target" selects a creature, player, or planeswalker).</item>
///   <item>"[Self] deals N damage to target creature." → Target with creature filter
///   (CR 119.2: narrower form restricted to creatures only).</item>
/// </list>
/// GUARD: does NOT match "each opponent" (family F17) — that shape has no "target" keyword.
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class SelfDealsDamageToAnyTargetEffectRule : IActivatedEffectRule
{
  // "… deals N damage to any target"
  private static readonly Regex AnyTargetPattern = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+any\s+target$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "… deals N damage to target <type>" — anchored on "target creature" (F16).
  // Accepts creature, artifact, enchantment, land, planeswalker, permanent to cover
  // sibling shapes without granting "each opponent" entry.
  private static readonly Regex TargetTypePattern = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    // --- branch 1: "to any target" ---
    var m = AnyTargetPattern.Match(trimmed);
    if (m.Success)
    {
      var subject = m.Groups["subject"].Value;
      if (subject.Length == 0 || !char.IsUpper(subject[0]))
        return null;

      return new MagicAST.AST.Effects.Damage.DealDamageEffect
      {
        Amount = LiteralQuantity.Of(ParseAmount(m.Groups["amount"].Value)),
        Source = ObjectReference.Self(),
        Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
      };
    }

    // --- branch 2: "to target <type>" ---
    var m2 = TargetTypePattern.Match(trimmed);
    if (m2.Success)
    {
      var subject = m2.Groups["subject"].Value;
      if (subject.Length == 0 || !char.IsUpper(subject[0]))
        return null;

      return new MagicAST.AST.Effects.Damage.DealDamageEffect
      {
        Amount = LiteralQuantity.Of(ParseAmount(m2.Groups["amount"].Value)),
        Source = ObjectReference.Self(),
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = [m2.Groups["type"].Value.ToLowerInvariant()],
          },
        },
      };
    }

    return null;
  }

  private static int ParseAmount(string raw) =>
    raw.ToLowerInvariant() switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(raw),
    };
}
