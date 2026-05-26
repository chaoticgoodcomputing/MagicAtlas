namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Niambi shape: "you may return ... to its owner's hand. If you do, you gain
/// life equal to that creature's mana value." Priority 80: must override
/// <see cref="ReturnToHandRule"/> so the IfYouDo continuation isn't dropped.
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ReturnToHandWithIfYouDoGainLifeRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var split = Regex.Match(
      text,
      @"^(?<ret>you\s+may\s+return\s+.+?to\s+(?:its?\s+owner'?s|your)\s+hand)\.\s*If\s+you\s+do,\s*(?<rest>you\s+gain\s+life\s+equal\s+to\s+(?<src>that\s+creature'?s\s+mana\s+value))$",
      RegexOptions.IgnoreCase
    );
    if (!split.Success)
    {
      return false;
    }

    var plain = new ReturnToHandRule();
    if (!plain.TryMatch(split.Groups["ret"].Value.Trim(), out var inner) || inner is not ReturnToHandEffect returnEffect)
    {
      return false;
    }

    var source = split.Groups["src"].Value.Trim();
    var sourceObject = Regex.Replace(source, @"'?s\s+mana\s+value$", "", RegexOptions.IgnoreCase).Trim();

    var gainLife = new GainLifeEffect
    {
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.ManaValue,
        Source = sourceObject,
      },
      Player = ObjectReference.You(),
    };

    effect = returnEffect with { IfYouDo = gainLife, IsOptional = true };
    return true;
  }
}
