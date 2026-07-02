namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter-type] counter on each [type] you control." — Titania's Boon. Count=1.
/// Matches the mass-counter shape where the controller distributes one counter to
/// every qualifying permanent they control, e.g.:
///   "Put a +1/+1 counter on each creature you control."
/// </summary>
[SpellRule]
public sealed class PutCounterOnEachRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Put\s+a\s+(?<counter>[+\-]\d+/[+\-]\d+|\w+)\s+counter\s+on\s+each\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)\s+you\s+control$",
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
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [m.Groups["type"].Value.ToLowerInvariant()],
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
