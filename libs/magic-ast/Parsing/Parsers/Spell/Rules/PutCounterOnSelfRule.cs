namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter-type] counter on this [creature|permanent|...]." — the
/// self-referential counter placement. Count=1.
///
/// <para>
/// This surfaces the put-counters-on-self shape on the SPELL rule chain so it is
/// reachable when a modal option body is dispatched as a <c>SpellAbility</c>. The
/// "When this creature enters, choose one — • Put a +1/+1 counter on this creature.
/// • ..." family (Saurian Symbiote) routes each bullet body back through the
/// classifier+registry, which classifies a bare "Put a … counter on this creature."
/// imperative as a spell. The triggered <c>PutCountersTriggeredRule</c> already
/// handles the same text on the triggered chain; this is its spell-chain sibling so
/// the mode parses to its established <see cref="PutCountersEffect"/> shape rather
/// than falling through to an unparsed ability.
/// </para>
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." Handles both P/T notation
/// counters ("+1/+1", "-1/-1") and named word counters. Distinct from
/// <c>PutCounterOnTargetRule</c> only in subject: "this [permanent]" → a
/// <see cref="ObjectReferenceKind.Self"/> reference rather than a targeted one.
/// </para>
/// </summary>
[SpellRule]
public sealed class PutCounterOnSelfRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Put\s+a\s+(?<counter>[+\-]\d+/[+\-]\d+|\w+)\s+counter\s+on\s+this\s+(?:creature|permanent|enchantment|artifact|land|planeswalker|token)$",
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
      Target = ObjectReference.Self(),
    };
    return true;
  }
}
