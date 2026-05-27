namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter-type] counter on target [type]." — Boon of Safety, Battlegrowth. Count=1.
/// Handles both word counters ("shield") and P/T notation counters ("+1/+1", "-1/-1").
/// </summary>
[SpellRule]
public sealed class PutCounterOnTargetRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Put\s+a\s+(?<counter>[+\-]\d+/[+\-]\d+|\w+)\s+counter\s+on\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
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
