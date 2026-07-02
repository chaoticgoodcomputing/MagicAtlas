namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals N damage to you" / "this creature deals N damage to you" — reflexive damage.
/// </summary>
[TriggeredRule]
public sealed class SelfDealsDamageToYouRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^(it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+(?<amount>\d+|one|two|three)\s+damage\s+to\s+you$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.Parse(raw),
    };
    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = ObjectReference.You(),
    };
    return true;
  }
}
