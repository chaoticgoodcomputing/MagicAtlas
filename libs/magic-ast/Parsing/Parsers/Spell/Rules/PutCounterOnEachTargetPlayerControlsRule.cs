namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter-type] counter on each [type] target player controls." — the mass-counter
/// shape distributed over every qualifying permanent a TARGETED player controls, e.g.:
///   "Put a +1/+1 counter on each creature target player controls." (Requisition Raid's
///   third Spree mode).
/// Count=1. Sibling of <see cref="PutCounterOnEachRule"/> (the "each … you control" form);
/// the controller axis is <see cref="ControllerFilter.Target"/> rather than
/// <see cref="ControllerFilter.You"/>. Fully anchored (<c>^…$</c>) and required to end in
/// "target player controls", so it is mutually exclusive with the "you control" sibling and
/// cannot substring-capture it.
/// </summary>
[SpellRule]
public sealed class PutCounterOnEachTargetPlayerControlsRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Put\s+a\s+(?<counter>[+\-]\d+/[+\-]\d+|\w+)\s+counter\s+on\s+each\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)\s+target\s+player\s+controls$",
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
          Controller = ControllerFilter.Target,
        },
      },
    };
    return true;
  }
}
