namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put N [counter type] counters on target [Subtype1] or [Subtype2] you control."
/// Subtype-disjunction target (Rule 205.3) with an explicit Controller=You.
/// </summary>
[SpellRule]
public sealed class PutCountersTargetSubtypeDisjunctionRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Put\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counter>[a-z]+(?:[+\-]\d+/[+\-]\d+)?)\s+counters?\s+on\s+target\s+(?<s1>[A-Z][A-Za-z]+)\s+or\s+(?<s2>[A-Z][A-Za-z]+)(?:\s+you\s+control)?$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var s1 = m.Groups["s1"].Value;
    var s2 = m.Groups["s2"].Value;
    if (!char.IsUpper(s1[0]) || !char.IsUpper(s2[0]))
    {
      return false;
    }

    var count = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["count"].Value));
    var controller = text.Contains("you control", System.StringComparison.OrdinalIgnoreCase)
      ? ControllerFilter.You
      : (ControllerFilter?)null;

    effect = new PutCountersEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          Subtypes = new List<string> { s1, s2 },
          Controller = controller,
        },
      },
      CounterType = m.Groups["counter"].Value.ToLowerInvariant(),
      Count = count,
    };
    return true;
  }
}
