namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "put a +1/+1 counter on it and draw a card" — the ETB trigger effect on
/// The Great Henge family. The entering creature (the subject of the enters
/// trigger) receives one +1/+1 counter, and the ability's controller draws
/// one card. Both effects resolve simultaneously as a <see cref="CompositeEffect"/>.
///
/// <para>
/// CR 603.6a: "Enters-the-battlefield abilities trigger when a permanent enters
/// the battlefield." The "it" pronoun back-references the permanent that just
/// entered — the trigger's subject — encoded as
/// <see cref="ObjectReferenceKind.It"/> following the existing PutCountersTriggeredRule
/// convention for pronoun antecedents (Rule 109.2).
/// </para>
///
/// <para>
/// CR 121.1: Drawing a card is "putting the top card of that player's library
/// into their hand." The draw targets <see cref="ObjectReferenceKind.You"/> —
/// the ability's controller — which is the canonical reference for "draw a card"
/// without an explicit subject.
/// </para>
///
/// <para>
/// The rule is fully anchored (^…$) so it cannot match as a substring inside a
/// more-specific sibling's text.
/// </para>
/// </summary>
[TriggeredRule(Priority = 985)]
public sealed class PutCounterOnItAndDrawCardRule : ITriggeredRule
{
  // Anchored to the full effect clause; "it" pronoun refers to the entering creature.
  private static readonly Regex _pattern = new(
    @"^put\s+a\s+\+1/\+1\s+counter\s+on\s+it\s+and\s+draw\s+a\s+card$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.It(),
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(1),
        },
        new DrawCardsEffect
        {
          Count = LiteralQuantity.Of(1),
          Player = ObjectReference.You(),
        },
      ],
    };
    return true;
  }
}
