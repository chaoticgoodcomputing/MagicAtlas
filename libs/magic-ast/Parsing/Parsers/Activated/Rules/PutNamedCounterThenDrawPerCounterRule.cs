namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Put a [counter] counter on [self], then draw a card for each [counter] counter on [self]" —
/// The One Ring's {T} ability ("Put a burden counter on The One Ring, then draw a card for each
/// burden counter on The One Ring").
///
/// <para>
/// A two-step <see cref="CompositeEffect"/>: (1) <see cref="PutCountersEffect"/> adds one named counter
/// to the source (<see cref="ObjectReferenceKind.Self"/>); (2) <see cref="DrawCardsEffect"/> draws a
/// card for each such counter — <see cref="DrawCardsEffect.Count"/> is a
/// <see cref="CounterCountQuantity"/> over the same named counter on the source, read AFTER the put
/// (reference-not-resolution, ADR 0004; the engine evaluates the live count). CR 122 (counters);
/// CR 121 (draw). The same counter name must appear in both clauses.
/// </para>
///
/// <para>The self target is "it" or the card naming itself (capitalized → self-reference, CR 201.5).
/// ANCHORED on the full clause.</para>
///
/// <para>Priority 999: must run BEFORE the generic <c>DrawCardsEffectRule</c> (Priority 998), which
/// otherwise greedily claims the sentence's "draw a card" clause as a flat <c>drawCards</c> and drops
/// the put-counter clause + the per-counter scaling. This rule is strictly more specific (it owns the
/// full anchored put-then-draw-per-counter shape).</para>
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class PutNamedCounterThenDrawPerCounterRule : IActivatedEffectRule
{
  // Self subject: "it" or a capitalized name (self-by-name). Shared sub-pattern for both clauses.
  private const string Self = @"(?:it|[A-Z][A-Za-z',]*(?:\s+[A-Za-z',]+)*)";

  private static readonly Regex _pattern = new(
    @"^Put\s+a\s+(?<counter>[a-z][a-z\-]*)\s+counter\s+on\s+"
      + Self
      + @",\s*then\s+draw\s+a\s+card\s+for\s+each\s+(?<counter2>[a-z][a-z\-]*)\s+counter\s+on\s+"
      + Self
      + @"$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var m = _pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return null;
    }

    var counter = m.Groups["counter"].Value.ToLowerInvariant();
    // The "draw for each" clause must reference the same counter that was just put.
    if (!string.Equals(counter, m.Groups["counter2"].Value, System.StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return new CompositeEffect
    {
      Effects =
      [
        new PutCountersEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          CounterType = counter,
          Count = new LiteralQuantity { Value = 1 },
        },
        new DrawCardsEffect
        {
          Player = new ObjectReference { Kind = ObjectReferenceKind.You },
          Count = new CounterCountQuantity
          {
            CounterType = counter,
            On = new ObjectReference { Kind = ObjectReferenceKind.Self },
          },
        },
      ],
    };
  }
}
