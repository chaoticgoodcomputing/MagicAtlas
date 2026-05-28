namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to any target." — self-as-source dealDamage with
/// AnyTarget (captures Denethor's burn tail and any card referencing itself by
/// name in the source position).
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class SelfDealsDamageToAnyTargetEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+any\s+target$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    // The subject is the card's own name (capitalised).
    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return null;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(raw),
    };
    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
  }
}
