namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Tap target [type]." with optional count and type-disjunction target.
/// </summary>
[SpellRule]
public sealed class SpellTapTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = Regex.Match(
      text,
      @"^Tap(?:\s+(?<count>(?<cmin>\w+)\s+or\s+(?<cmax>\w+)|\w+))?\s+target\s+(?<types>\w+(?:\s*,\s*\w+)*(?:\s*,?\s+or\s+\w+)?)$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return false;
    }

    Quantity? count = null;
    if (match.Groups["count"].Success)
    {
      if (match.Groups["cmin"].Success && match.Groups["cmax"].Success)
      {
        if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["cmin"].Value, out var min)
          || !SpellRuleHelpers.TryParseSmallWord(match.Groups["cmax"].Value, out var max))
        {
          return false;
        }
        count = new UpToQuantity { Minimum = min, Maximum = max };
      }
      else
      {
        if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["count"].Value, out var n))
        {
          return false;
        }
        count = LiteralQuantity.Of(n);
      }
    }

    var typesPhrase = match.Groups["types"].Value;
    var types = Regex
      .Split(typesPhrase, @"\s*,\s*|\s+or\s+")
      .Select(t => t.Trim().ToLowerInvariant())
      .Select(t => t.EndsWith("s") && t.Length > 1 ? t[..^1] : t)
      .Where(t => t.Length > 0)
      .ToList();
    if (types.Count == 0)
    {
      return false;
    }
    effect = new TapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = types },
      },
      Count = count,
    };
    return true;
  }
}
