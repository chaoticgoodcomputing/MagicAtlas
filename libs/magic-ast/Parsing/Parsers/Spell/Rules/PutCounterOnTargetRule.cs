namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter-type] counter on target [type]." — Boon of Safety. Count=1.
/// </summary>
[SpellRule]
public sealed class PutCounterOnTargetRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Put\s+a\s+(?<counter>\w+)\s+counter\s+on\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    effect = new PutCountersEffect
    {
      CounterType = m.Groups["counter"].Value.ToLowerInvariant(),
      Count = LiteralQuantity.Of(1),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [m.Groups["type"].Value.ToLowerInvariant()] },
      },
    };
    return true;
  }
}
